const pendingPromises = {};

function generateGuid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

let isWebMessageListenerAttached = false;

function initializeWebMessageListenerInternal() {
    if (window.chrome && window.chrome.webview) {
        if (isWebMessageListenerAttached) {
            console.debug("WebMessage listener already attached.");
            return true;
        }
        console.info("Attaching WebMessage listener.");
        window.chrome.webview.addEventListener('message', (event) => {
            console.debug("Received message from C#:", event.data);
            try {
                // C# uses PostWebMessageAsJson(responseJson). It seems WebView2 parses this JSON string
                // back into an object before delivering it to JS as event.data.
                const response = event.data; // event.data is the object directly

                if (response.CallbackId && pendingPromises[response.CallbackId]) {
                    if (response.Error) { // response.Error is still a JSON string from C#
                        let errorDetails = { message: "Unknown error from C#" };
                        try {
                            errorDetails = JSON.parse(response.Error);
                        } catch (e) {
                            console.error("Failed to parse error details string from C#:", response.Error, e);
                        }
                        console.error(`Error from C# for ${response.CallbackId}:`, errorDetails.message);
                        pendingPromises[response.CallbackId].reject(new Error(errorDetails.message || "Unknown error from C#"));
                    } else if (response.Result !== undefined && response.Result !== null) { // response.Result is still a JSON string from C#
                        try {
                            pendingPromises[response.CallbackId].resolve(JSON.parse(response.Result));
                        } catch (parseError) {
                            console.error(`Failed to parse Result JSON from C# for ${response.CallbackId}:`, response.Result, parseError);
                            const errorToReject = parseError instanceof Error ? parseError : new Error(String(parseError));
                            pendingPromises[response.CallbackId].reject(new Error(`Failed to parse result from C#: ${errorToReject.message}`));
                        }
                    } else {
                        pendingPromises[response.CallbackId].resolve(undefined); // For calls that don't return data, like save.
                    }
                    delete pendingPromises[response.CallbackId];
                } else {
                    console.warn("Received message with unknown CallbackId or no pending promise:", response);
                }
            } catch (e) {
                console.error("Error parsing message from C# or handling promise:", e, event.data);
            }
        });
        isWebMessageListenerAttached = true;
        console.info("WebMessage listener successfully attached.");
        return true;
    } else {
        console.warn("window.chrome.webview not available for WebMessage listener attachment.");
        return false;
    }
}

export function initLightboxApiService() {
    return new Promise((resolve) => {
        if (isWebMessageListenerAttached) {
            resolve(true);
            return;
        }
        // Try a few times in case webview is slow to initialize
        let attempts = 0;
        const maxAttempts = 10;
        const interval = 200;

        function tryInit() {
            attempts++;
            if (initializeWebMessageListenerInternal()) {
                resolve(true);
            } else if (attempts < maxAttempts) {
                console.debug(`WebMessage listener init attempt ${attempts} failed, retrying in ${interval}ms...`);
                setTimeout(tryInit, interval);
            } else {
                console.error("Failed to initialize WebMessage listener after multiple attempts.");
                resolve(false); // Or reject(new Error(...))
            }
        }
        tryInit();
    });
}

// Do NOT call initializeWebMessageListenerInternal() directly here at module load time.
// It should be called via initLightboxApiService from the application.

async function sendMessageToCSharp(command, payload) {
    if (!isWebMessageListenerAttached) {
        // Attempt to initialize if not done yet, though ideally initLightboxApiService should be called first.
        // This is a fallback.
        console.warn("WebMessage listener not attached. Attempting to initialize now before sending message.");
        const initialized = await initLightboxApiService();
        if (!initialized) {
             throw new Error("WebMessage listener could not be initialized. Cannot send message to C#.");
        }
    }
    if (!window.chrome?.webview) {
        console.error("window.chrome.webview is not available. Cannot send message to C#.");
        throw new Error("WebView communication channel is not available.");
    }

    const callbackId = generateGuid();
    const request = {
        Command: command,
        Payload: payload ? JSON.stringify(payload) : null, // Payload is stringified here
        CallbackId: callbackId
    };

    console.debug(`Sending message to C#: Command=${command}, CallbackId=${callbackId}`);
    // The request object itself is sent. C# will deserialize this.
    window.chrome.webview.postMessage(request);

    return new Promise((resolve, reject) => {
        pendingPromises[callbackId] = { resolve, reject };
        setTimeout(() => {
            if (pendingPromises[callbackId]) {
                console.warn(`Timeout waiting for C# response for command: ${command}, CallbackId: ${callbackId}`);
                pendingPromises[callbackId].reject(new Error(`Timeout waiting for C# response for command: ${command}`));
                delete pendingPromises[callbackId];
            }
        }, 30000); // 30 second timeout
    });
}

export async function getApplicationSettings() {
    console.info("Requesting application settings via WebMessage...");
    try {
        // The sendMessageToCSharp function already handles parsing the 'Result' field.
        const settings = await sendMessageToCSharp("getApplicationSettings");
        console.log("lightboxApi: getApplicationSettings received from sendMessageToCSharp:", settings); // Added log
        if (settings && typeof settings.pluginScanDirectories === 'undefined') {
            console.warn("lightboxApi: 'pluginScanDirectories' is undefined in settings object received from C#. Defaulting to empty array.", settings);
            // Potentially correct it here, though store level correction is also in place
            // return { ...settings, pluginScanDirectories: [] };
        }
        console.info("Application settings loaded successfully via WebMessage:", settings);
        return settings;
    } catch (error) {
        console.error("Error requesting getApplicationSettings via WebMessage:", error);
        throw error;
    }
}

export async function saveApplicationSettings(settings) {
    console.info("Saving application settings via WebMessage...");
    try {
        // sendMessageToCSharp will resolve with 'undefined' if C# sends back a result that is null/undefined.
        await sendMessageToCSharp("saveApplicationSettings", settings);
        console.info("Application settings saved successfully via WebMessage.");
    } catch (error) {
        console.error("Error requesting saveApplicationSettings via WebMessage:", error);
        throw error;
    }
}

export async function getAllPluginDefinitions() {
    console.info("Requesting plugin definitions via WebMessage...");
    try {
        const plugins = await sendMessageToCSharp("getAllPluginDefinitions");
        console.info("Plugin definitions received from sendMessageToCSharp:", plugins); // Added log
        if (typeof plugins === 'undefined') {
            console.error("getAllPluginDefinitions in lightboxApi received 'undefined' from sendMessageToCSharp. Returning empty array instead.");
            return []; // Prevent setting store to undefined
        }
        console.info("Plugin definitions loaded successfully via WebMessage:", plugins);
        return plugins;
    } catch (error) {
        console.error("Error requesting getAllPluginDefinitions via WebMessage:", error);
        throw error;
    }
}

// --- Workspace Management API Calls ---

/**
 * 获取所有工作区信息列表
 */
export async function getWorkspaces() {
    console.info("Requesting workspaces via WebMessage...");
    try {
        const workspaces = await sendMessageToCSharp("getWorkspaces");
        console.info("Workspaces loaded successfully via WebMessage:", workspaces);
        return workspaces || []; // Ensure an array is returned
    } catch (error) {
        console.error("Error requesting getWorkspaces via WebMessage:", error);
        throw error;
    }
}

/**
 * 创建新的工作区
 * @param name 工作区名称
 * @param icon 工作区图标
 */
export async function createWorkspace(name, icon) {
    console.info(`Creating workspace "${name}" via WebMessage...`);
    try {
        const newWorkspaceInfo = await sendMessageToCSharp("createWorkspace", { name, icon });
        console.info("Workspace created successfully via WebMessage:", newWorkspaceInfo);
        return newWorkspaceInfo;
    } catch (error) {
        console.error(`Error creating workspace "${name}" via WebMessage:`, error);
        throw error;
    }
}

/**
 * 设置当前活动工作区
 * @param workspaceId 要激活的工作区ID
 */
export async function setActiveWorkspace(workspaceId) {
    console.info(`Setting active workspace to "${workspaceId}" via WebMessage...`);
    try {
        await sendMessageToCSharp("setActiveWorkspace", { workspaceId });
        console.info(`Active workspace set to "${workspaceId}" successfully via WebMessage.`);
    } catch (error) {
        console.error(`Error setting active workspace to "${workspaceId}" via WebMessage:`, error);
        throw error;
    }
}

/**
 * 获取当前活动工作区的详细信息
 */
export async function getActiveWorkspace() {
    console.info("Requesting active workspace via WebMessage...");
    try {
        const activeWorkspace = await sendMessageToCSharp("getActiveWorkspace");
        console.info("Active workspace loaded successfully via WebMessage:", activeWorkspace);
        return activeWorkspace;
    } catch (error) {
        console.warn("Error or special condition encountered in getActiveWorkspace:", error);

        if (error instanceof Error) {
            const errorMessage = error.message.toLowerCase();
            // Check for parsing errors or explicit "no active workspace" messages
            if (errorMessage.includes("failed to parse result from c#") ||
                errorMessage.includes("unexpected token") || // Catches JSON.parse('') and other direct parse failures
                errorMessage.includes("json.parse") || // More general parsing error messages
                errorMessage.includes("no active workspace")) {  // Explicit message from C#
                console.info("getActiveWorkspace: Interpreting error as 'no active workspace'. Returning null. Original error:", error.message);
                return null;
            }
        }

        // For other types of errors, re-throw.
        console.error("getActiveWorkspace: Unhandled error, re-throwing:", error);
        throw error;
    }
}

/**
 * 更新工作区信息
 * @param workspace 要更新的工作区对象 (完整 Workspace 对象)
 */
export async function updateWorkspace(workspace) {
    console.info(`Updating workspace "${workspace.id}" via WebMessage...`);
    try {
        // JSBridge侧接收的是JSON字符串，sendMessageToCSharp 内部会处理 payload 的序列化
        await sendMessageToCSharp("updateWorkspace", workspace);
        console.info(`Workspace "${workspace.id}" updated successfully via WebMessage.`);
    } catch (error) {
        console.error(`Error updating workspace "${workspace.id}" via WebMessage:`, error);
        throw error;
    }
}

/**
 * 删除工作区
 * @param workspaceId 要删除的工作区ID
 */
export async function deleteWorkspace(workspaceId) {
    console.info(`Deleting workspace "${workspaceId}" via WebMessage...`);
    try {
        await sendMessageToCSharp("deleteWorkspace", { workspaceId });
        console.info(`Workspace "${workspaceId}" deleted successfully via WebMessage.`);
    } catch (error) {
        console.error(`Error deleting workspace "${workspaceId}" via WebMessage:`, error);
        throw error;
    }
}