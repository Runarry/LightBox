# LightBox 项目详细设计与开发计划 (MVP 版本)

## 1. 引言

### 1.1. 项目目标回顾
LightBox 旨在为游戏策划人员提供一个集成化的、可自定义的工具集合，以提高工作效率。它通过主程序+插件的架构，允许用户根据自身需求组合和配置各种实用工具。

### 1.2. MVP 范围概述
MVP 版本将专注于实现 LightBox 的核心框架和基础功能，包括：
*   WPF 主程序作为 WebView2 的宿主。
*   基于 Preact 的前端 UI，用于工作区管理、插件列表展示、插件配置和实例控制。
*   通过 JSBridge 实现前后端通信。
*   核心逻辑层负责工作区管理、插件发现与定义加载、插件实例生命周期控制（外部进程和 C# 库类型）、配置存储。
*   支持 `ILightBoxHostContext` 的 `GetWorkspacePath` 方法和 `ILightBoxPlugin` 的 `ExecuteCommand` 方法。
*   JSBridge 增加获取所有已发现插件列表的接口。
*   IPC 通信初期不实现复杂认证。
*   C# 库插件的 UI 作为独立窗口处理（由插件自身负责）。
*   前端状态管理采用 Zustand。
*   日志系统支持 Info 级别以上，日志内容对用户可见（通过打开日志文件）。
*   插件配置表单支持中等复杂度的 JSON Schema。
*   错误通过 Toast 通知和插件状态区域显示。
*   不包含 C# 插件的动态卸载 (`AssemblyLoadContext` 的复杂应用)、高级 IPC 机制、插件市场、主题定制等非 MVP 功能。

### 1.3. 文档目的
本文档旨在详细描述 LightBox MVP 版本的各模块设计、接口定义、数据模型、关键流程以及分阶段的开发步骤，为后续的开发工作提供清晰的指引。

## 2. 整体架构回顾

### 2.1. 架构图
```
+---------------------------------+      +---------------------------------+      +---------------------------------+
|     LightBox.WPF (EXE)          |----->|    LightBox.Core (DLL)          |----->| LightBox.PluginContracts (DLL)|
|  (UI Shell, WebView2 Host,     |      | (Workspace, Plugin Mgmt,        |      | (ILightBoxPlugin Interface,   |
|   JSBridge Implementation)      |      |  Config Handling, Core Logic)   |      |  HostContext Interface)       |
+---------------------------------+      +---------------------------------+      +---------------------------------+
         |         ^                                                                     |         ^
         |         | (JSBridge Calls, WebView2 Messages)                                 |         | (Plugin Loading, IPC)
         v         |                                                                     v         |
+---------------------------------+                                          +---------------------------------+
|  LightBox.WebViewUI             |                                          |      External Plugins (EXE)     |
|  (Preact/Vite, Static Assets)   |                                          | (Any Language, Own UI, IPC)     |
|  (Main UI, Plugin Config UI)    |                                          +---------------------------------+
+---------------------------------+                                                      AND/OR
                                                                           +---------------------------------+
                                                                           |   C# Library Plugins (DLL)      |
                                                                           | (Implements ILightBoxPlugin,    |
                                                                           |  May have own C# UI or be UI-less)|
                                                                           +---------------------------------+
```

### 2.2. 核心组件职责 (MVP 重点)
*   **LightBox.WPF (UI宿主层):** 宿主 WebView2，实现 JSBridge。
*   **LightBox.Core (核心逻辑层):** 管理工作区、插件定义、插件实例生命周期 (启动/停止)，处理配置，提供 `ILightBoxHostContext` (含 `Log`, `GetWorkspacePath`)，实现基础日志记录。
*   **LightBox.PluginContracts (插件契约):** 定义 `ILightBoxPlugin` (含 `ExecuteCommand`) 和 `ILightBoxHostContext` 接口。
*   **LightBox.WebViewUI (前端界面):** 提供工作区管理、插件列表、插件配置 (中等复杂度 Schema)、实例控制 UI；使用 Zustand 进行状态管理；通过 JSBridge 与核心逻辑交互；错误展示。
*   **Plugins (插件系统):** 支持 `external_process` 和 `csharp_library` 类型，通过 `manifest.json` 声明。

## 3. 模块细化设计 (MVP)

### 3.1. `LightBox.WPF` (UI 宿主层)
*   **项目类型：** WPF 应用程序 (.NET 6/7/8+)。
*   **主要类：**
    *   `MainWindow.xaml`/`.cs`:
        *   **职责：** 主窗口，初始化并承载 `Microsoft.Web.WebView2.Wpf.WebView2` 控件。
        *   **WebView2 初始化：**
            *   调用 `EnsureCoreWebView2Async()`。
            *   设置 `Source` 指向 `LightBox.WebViewUI` 构建后的 `index.html` 文件路径 (例如，通过相对路径或配置读取)。
            *   处理 `CoreWebView2InitializationCompleted` 事件：
                *   注入 `LightBoxJsBridge` 对象。
                *   (可选) 注册从 JavaScript 发送至 C# 的消息处理 (`WebMessageReceived`)，虽然主要通信通过 JSBridge。
        *   **窗口管理：** 处理窗口大小调整、关闭等基本窗口事件。
    *   `LightBoxJsBridge.cs`:
        *   **职责：** 作为 JavaScript (WebViewUI) 与 C# (`LightBox.Core`) 之间的桥梁。此类实例将通过 `CoreWebView2.AddHostObjectToScript("lightboxBridge", bridgeInstance)` 暴露给前端。
        *   **依赖注入：** 构造函数接收 `LightBox.Core` 提供的服务接口实例 (例如 `IWorkspaceService`, `IPluginService`, `IConfigurationService`, `IApplicationSettingsService`, `ILoggingService`)。
        *   **方法签名约定：**
            *   异步方法返回 `Task<string>` (JSON 序列化的结果) 或 `Task` (无返回值)。
            *   参数尽量使用简单类型 (string, int, bool) 或 JSON 字符串。
    *   `App.xaml.cs`:
        *   **职责：** 应用程序启动入口。
        *   初始化 `LightBox.Core` 的服务依赖注入容器 (如果使用)。
        *   创建并显示 `MainWindow`。
        *   全局异常处理。

### 3.2. `LightBox.Core` (核心逻辑层)
*   **项目类型：** .NET 类库。
*   **主要命名空间/服务接口与实现：**
    *   `LightBox.Core.Services.Interfaces`:
        *   `IWorkspaceService`: 管理工作区。
        *   `IPluginService`: 管理插件定义和实例。
        *   `IConfigurationService`: 管理插件配置。
        *   `IApplicationSettingsService`: 管理应用级设置。
        *   `ILoggingService`: 提供日志记录功能。
    *   `LightBox.Core.Services.Implementations`:
        *   `WorkspaceManager.cs` (`IWorkspaceService`):
            *   **数据模型:** `WorkspaceInfo` (ID, Name, FilePath), `Workspace` (ID, Name, Description, List<PluginInstanceEntry>)。
            *   **存储:**
                *   `workspaces.json` (在用户文档 `LightBox/Workspaces/` 目录下): 存储 `List<WorkspaceInfo>`。
                *   每个工作区一个 JSON 文件 (如 `MyWorkspace.json`): 存储单个 `Workspace` 对象。
            *   **方法:** `LoadWorkspacesAsync`, `CreateWorkspaceAsync`, `GetWorkspaceByIdAsync`, `SaveWorkspaceAsync`, `DeleteWorkspaceAsync`, `SetActiveWorkspaceIdAsync`, `GetActiveWorkspaceIdAsync`。
        *   `PluginManager.cs` (`IPluginService`の一部):
            *   **数据模型:** `PluginDefinition` (从 `manifest.json` 解析)，`PluginInstance` (运行时表示，包含 `PluginDefinition` 和 `InstanceId`, `CurrentConfigJson`, `Status`)。
            *   **插件发现:**
                *   从 `ApplicationSettings` 获取插件扫描目录列表。
                *   递归扫描目录，查找 `manifest.json` 文件。
                *   解析 `manifest.json` (使用 `System.Text.Json`) 并缓存 `PluginDefinition` 列表。
            *   **方法:** `DiscoverPluginsAsync`, `GetAllPluginDefinitionsAsync`, `GetPluginDefinitionByIdAsync`。
        *   `PluginInstanceController.cs` (`IPluginService`の一部):
            *   **职责:** 管理 `PluginInstance` 的生命周期。
            *   **方法:**
                *   `CreatePluginInstanceAsync(string workspaceId, string pluginId)`: 创建实例并添加到工作区。
                *   `RemovePluginInstanceAsync(string workspaceId, string instanceId)`。
                *   `StartPluginAsync(string instanceId)`:
                    *   获取 `PluginInstance` 和其 `PluginDefinition`。
                    *   **External Process:**
                        *   准备启动参数 (替换 `args_template` 中的占位符如 `{config_file}`, `{instance_id}`, `{host_api_endpoint}` - 如果有)。
                        *   如果需要，将当前配置写入临时 `{config_file}`。
                        *   设置环境变量 (如 `LIGHTBOX_API_ENDPOINT` - 如果有)。
                        *   使用 `System.Diagnostics.Process.Start()` 启动。
                        *   监控进程状态，更新 `PluginInstance.Status`。
                    *   **C# Library:**
                        *   加载程序集 (`Assembly.LoadFrom`)。
                        *   创建 `main_class` 实例。
                        *   调用 `ILightBoxPlugin.Initialize(hostContext, instanceId, configJson)`。
                        *   调用 `ILightBoxPlugin.Start()`。
                        *   更新 `PluginInstance.Status`。
                *   `StopPluginAsync(string instanceId)`:
                    *   **External Process:** 尝试正常关闭 (e.g., `Process.CloseMainWindowAsync`)，超时后 `Process.Kill()`。
                    *   **C# Library:** 调用 `ILightBoxPlugin.Stop()`。
                    *   更新 `PluginInstance.Status`。
                *   `ExecutePluginCommandAsync(string instanceId, string commandName, object payload)`:
                    *   仅适用于 C# 库插件，调用 `ILightBoxPlugin.ExecuteCommand(commandName, payload)`。
        *   `ConfigurationService.cs` (`IConfigurationService`):
            *   **职责:** 获取和保存特定插件实例的配置。
            *   配置数据存储在对应工作区的 JSON 文件中，作为 `PluginInstanceEntry` 的一部分。
            *   **方法:** `GetPluginConfigSchemaAsync(string pluginId)`, `GetPluginConfigurationAsync(string instanceId)`, `SavePluginConfigurationAsync(string instanceId, string configJson)`。
        *   `ApplicationSettingsService.cs` (`IApplicationSettingsService`):
            *   **数据模型:** `ApplicationSettings` (PluginScanDirectories, DefaultWorkspaceId, LogFilePath, etc.)。
            *   **存储:** `LightBox/settings.json` (用户文档目录)。
            *   **方法:** `LoadSettingsAsync`, `SaveSettingsAsync`。
        *   `LoggingService.cs` (`ILoggingService`):
            *   **技术选型:** Serilog (或 NLog)。
            *   **配置:**
                *   日志级别: Info 及以上。
                *   输出到文件 (路径来自 `ApplicationSettings.LogFilePath`)。
                *   文件滚动策略 (例如按天或按大小)。
            *   **方法:** `LogInfo(string message)`, `LogWarning(string message)`, `LogError(string message, Exception ex = null)`, `LogDebug(string message)`。
    *   `LightBox.Core.IPC`:
        *   `HttpIPCListener.cs` (可选，MVP 阶段外部插件主要通过启动参数和 `ILightBoxHostContext` 获取信息，此部分可简化或推迟)。如果实现，监听一个端口，提供非常基础的 API (例如插件上报日志)。
    *   `LightBox.Core.Host`:
        *   `LightBoxHostContext.cs` (实现 `ILightBoxPluginContracts.ILightBoxHostContext`):
            *   构造函数接收 `ILoggingService`, `IWorkspaceService` 等。
            *   `Log(LogLevel level, string message)`: 调用 `ILoggingService`。
            *   `GetWorkspacePath()`: 获取当前活动工作区的根目录路径 (如果工作区与特定文件系统路径关联) 或主程序定义的工作区数据存储路径。

### 3.3. `LightBox.WebViewUI` (前端界面 - Preact + Vite + Zustand)
*   **项目类型：** Vite + Preact + TypeScript。
*   **目录结构 (示例 `src/`):**
    *   `main.tsx`: 应用入口，初始化 Preact, Zustand store, 路由。
    *   `App.tsx`: 根组件，包含布局 (侧边栏、主内容区)、路由定义。
    *   `components/`:
        *   `common/`: 按钮, 输入框, 模态框, Toast通知, 加载指示器等。
        *   `workspace/`: `WorkspaceList.tsx`, `CreateWorkspaceModal.tsx`。
        *   `plugin/`: `PluginList.tsx`, `PluginCard.tsx`, `PluginConfigForm.tsx` (使用 `@rjsf/core`)。
        *   `settings/`: `AppSettingsForm.tsx`。
    *   `views/`: 页面级组件 (如 `DashboardView.tsx`, `WorkspaceView.tsx`, `SettingsView.tsx`)。
    *   `services/lightboxApi.ts`:
        *   封装对 `window.lightboxBridge` 的所有调用。
        *   提供类型化的接口，处理 JSON 解析/序列化。
    *   `store/`: Zustand store 定义。
        *   `workspaceStore.ts`: 管理工作区列表, 当前活动工作区, 工作区内的插件实例。
        *   `pluginStore.ts`: 管理所有已发现的插件定义, 插件实例状态。
        *   `settingsStore.ts`: 管理应用设置。
        *   `uiStore.ts`: 管理全局 UI 状态 (如加载状态, Toast 消息)。
    *   `hooks/`: 自定义 React Hooks (如 `useWorkspaces`, `usePlugins`)。
    *   `router/`: 路由配置 (如使用 `preact-iso` 或 `wouter`)。
    *   `assets/`: 静态资源 (图片, CSS)。
*   **插件配置表单:**
    *   **技术选型:** `@rjsf/core` 配合 `@rjsf/preact` (或一个适配 Preact 的主题)。
    *   **功能:**
        *   根据从 `lightboxApi.getPluginConfigSchema(pluginId)` 获取的 JSON Schema 动态生成表单。
        *   加载并填充从 `lightboxApi.getPluginConfiguration(...)` 获取的当前配置。
        *   支持 JSON Schema 的基本类型 (string, number, integer, boolean), 对象 (嵌套), 数组 (固定项和可变项), 枚举 (`enum`)。
        *   支持 `required` 字段验证。
        *   提供良好的用户体验，对于中等复杂度的 Schema (例如两三层嵌套，包含数组) 能够清晰展示和编辑。
*   **错误展示:**
    *   **Toast 通知:** 使用一个简单的 Toast 组件库或自定义实现，用于显示操作成功/失败的简短消息。由 `uiStore` 管理。
    *   **插件状态区域:** 在插件卡片或列表中，明确显示插件实例的当前状态 (如 "Stopped", "Running", "Error: Failed to start", "Configuring") 和相关的错误信息摘要。

### 3.4. `LightBox.PluginContracts` (插件契约)
*   **项目类型：** .NET Standard 2.0 类库 (为了最大兼容性)。
*   **主要接口/类：**
    *   `ILightBoxPlugin`:
        ```csharp
        public interface ILightBoxPlugin
        {
            string Id { get; } // 由插件实现者确保与 manifest.json 中的 id 一致
            void Initialize(ILightBoxHostContext hostContext, string instanceId, string configurationJson);
            void Start();
            void Stop();
            Task<object> ExecuteCommand(string commandName, object payload); // payload 和返回值可以是简单类型或可序列化对象
        }
        ```
    *   `ILightBoxHostContext`:
        ```csharp
        public interface ILightBoxHostContext
        {
            void Log(LogLevel level, string message);
            string GetWorkspacePath(); // 返回当前活动工作区的相关路径
        }
        ```
    *   `LogLevel`:
        ```csharp
        public enum LogLevel { Debug, Info, Warning, Error }
        ```

## 4. 接口定义 (MVP)

### 4.1. `LightBoxJsBridge` (C# to JS, exposed as `window.lightboxBridge`)
*   **工作区管理:**
    *   `Task<string> GetWorkspaces()`: 返回 `List<WorkspaceInfo>` 的 JSON 字符串。
    *   `Task<string> CreateWorkspace(string name)`: 创建新工作区，返回 `WorkspaceInfo` 的 JSON 字符串。
    *   `Task SetActiveWorkspace(string workspaceId)`: 设置当前活动工作区。
    *   `Task<string> GetActiveWorkspace()`: 返回当前活动 `Workspace` 对象的 JSON 字符串。
*   **插件管理:**
    *   `Task<string> GetPluginsInWorkspace(string workspaceId)`: 返回指定工作区内 `List<PluginInstance>` 的 JSON 字符串。
    *   `Task<string> GetAllPluginDefinitions()`: 返回所有已发现的 `List<PluginDefinition>` 的 JSON 字符串。
    *   `Task AddPluginToWorkspace(string workspaceId, string pluginId)`: 将插件添加到工作区。
    *   `Task RemovePluginFromWorkspace(string workspaceId, string instanceId)`: 从工作区移除插件实例。
*   **配置管理:**
    *   `Task<string> GetPluginConfigSchema(string pluginId)`: 返回指定插件 `config_schema` (JSON Schema) 的 JSON 字符串。
    *   `Task<string> GetPluginConfiguration(string instanceId)`: 返回指定插件实例当前配置的 JSON 字符串。
    *   `Task SavePluginConfiguration(string instanceId, string configJson)`: 保存插件实例的配置。
*   **实例控制:**
    *   `Task StartPlugin(string instanceId)`: 启动插件实例。
    *   `Task StopPlugin(string instanceId)`: 停止插件实例。
    *   `Task<string> ExecutePluginCommand(string instanceId, string commandName, string payloadJson)`: 执行 C# 库插件的命令，返回结果的 JSON 字符串。
*   **应用设置:**
    *   `Task<string> GetApplicationSettings()`: 返回 `ApplicationSettings` 的 JSON 字符串。
    *   `Task SaveApplicationSettings(string settingsJson)`: 保存应用设置。
*   **工具类:**
    *   `Task OpenLogFile()`: 打开当前日志文件。
    *   `Task<string> ShowSelectDirectoryDialog(string title, string initialDirectory)`: 显示目录选择对话框，返回选择的目录路径或空字符串。
    *   `Task ShowMessageBox(string title, string message, string type)`: type 可以是 "Info", "Warning", "Error"。

### 4.2. `ILightBoxPlugin` (C# Plugin Interface)
*   `string Id { get; }`
*   `void Initialize(ILightBoxHostContext hostContext, string instanceId, string configurationJson)`
*   `void Start()`
*   `void Stop()`
*   `Task<object> ExecuteCommand(string commandName, object payload)`

### 4.3. `ILightBoxHostContext` (C# Host Context for Plugins)
*   `void Log(LogLevel level, string message)`
*   `string GetWorkspacePath()`

### 4.4. IPC API (External Plugin to Core - `HttpIPCListener` - 简化版 MVP)
*   如果 MVP 阶段需要外部插件主动与 Core 通信（除了通过启动参数和配置文件），可以提供一个非常基础的 HTTP API。
*   例如: `POST /api/v1/instances/{instance_id}/log` (Body: `{ "level": "Info", "message": "Plugin started" }`)
*   `LIGHTBOX_API_ENDPOINT` 环境变量将指向 `http://localhost:<port>/api/v1`。
*   **注意:** MVP 初期，外部插件主要依赖启动参数和配置文件，此 API 可作为可选增强。

## 5. 数据模型 (MVP)

### 5.1. `manifest.json` (插件清单 - 示例)
```json
{
  "id": "com.example.mytool",
  "name": "My Awesome Tool",
  "version": "1.0.0",
  "description": "Does something awesome.",
  "author": "Dev Team",
  "plugin_type": "external_process",
  "executable": "bin/mytool.exe",
  "args_template": "--config \"{config_file}\" --instance \"{instance_id}\"",
  "config_schema": {
    "type": "object",
    "title": "My Tool Settings",
    "properties": {
      "apiKey": { "type": "string", "title": "API Key" },
      "isEnabled": { "type": "boolean", "title": "Enable Feature", "default": true },
      "retryAttempts": { "type": "integer", "title": "Retry Attempts", "default": 3 }
    },
    "required": ["apiKey"]
  },
  "communication": {
    "type": "stdio"
  },
  "icon": "icon.png"
}
```

### 5.2. `workspaces.json` (用户文档 `LightBox/Workspaces/`)
```json
[
  {
    "id": "uuid-workspace-1",
    "name": "Project Alpha",
    "filePath": "ProjectAlpha.json",
    "lastOpened": "2024-05-06T10:00:00Z"
  }
]
```

### 5.3. 单个工作区文件 (例如 `ProjectAlpha.json`)
```json
{
  "id": "uuid-workspace-1",
  "name": "Project Alpha",
  "description": "Workspace for Project Alpha tasks.",
  "pluginInstances": [
    {
      "instanceId": "uuid-instance-1-plugin-A",
      "pluginId": "com.example.mytool",
      "configuration": {
        "apiKey": "secret-key",
        "isEnabled": true,
        "retryAttempts": 5
      },
      "status": "Stopped",
      "lastErrorMessage": null
    }
  ]
}
```

### 5.4. `settings.json` (用户文档 `LightBox/`)
```json
{
  "pluginScanDirectories": [
    "C:/Users/MyUser/LightBoxPlugins",
    "./plugins"
  ],
  "defaultWorkspaceId": "uuid-workspace-1",
  "logFilePath": "Logs/lightbox-.log",
  "logLevel": "Information",
  "ipcApiPort": 5005
}
```

## 6. 关键交互流程细化 (MVP)

### 6.1. 应用启动与加载默认/上次工作区
1.  `LightBox.WPF.App` 启动。
2.  `ApplicationSettingsService` 加载 `settings.json`。
3.  `LoggingService` 根据设置初始化。
4.  `WorkspaceManager` 加载 `workspaces.json`。
5.  确定活动工作区 (基于 `defaultWorkspaceId` 或上次打开的)。
6.  `WorkspaceManager` 加载活动工作区文件 (如 `ProjectAlpha.json`)。
7.  `MainWindow` 初始化，WebView2 加载 `LightBox.WebViewUI`。
8.  `LightBoxJsBridge` 注入。
9.  **WebViewUI:**
    *   调用 `lightboxApi.getApplicationSettings()` 更新设置状态 (Zustand)。
    *   调用 `lightboxApi.getWorkspaces()` 获取工作区列表。
    *   调用 `lightboxApi.getActiveWorkspace()` 获取当前工作区数据。
    *   调用 `lightboxApi.getAllPluginDefinitions()` 获取所有可用插件定义。
    *   渲染主界面，显示工作区列表、当前工作区插件实例、可用插件列表。

### 6.2. 创建新工作区
1.  **WebViewUI:** 用户点击“创建工作区”按钮，输入名称。
2.  调用 `lightboxApi.createWorkspace(name)`。
3.  **Core:** `WorkspaceManager` 创建新的工作区 JSON 文件和 `workspaces.json` 条目。
4.  **WebViewUI:** 刷新工作区列表，新工作区设为活动工作区。

### 6.3. 添加插件到工作区
1.  **WebViewUI:** 用户从可用插件列表中拖拽或点击添加插件到当前工作区。
2.  调用 `lightboxApi.addPluginToWorkspace(activeWorkspaceId, pluginId)`。
3.  **Core:** `WorkspaceManager` 在当前工作区数据中创建 `PluginInstanceEntry` (使用 `config_schema` 的默认值或空配置)，生成 `instanceId`，保存工作区文件。
4.  **WebViewUI:** 刷新当前工作区的插件实例列表，新插件实例显示为“Configuring”或“Stopped”。

### 6.4. 配置插件实例
1.  **WebViewUI:** 用户点击插件实例的“配置”按钮。
2.  调用 `lightboxApi.getPluginConfigSchema(pluginId)`。
3.  调用 `lightboxApi.getPluginConfiguration(instanceId)`。
4.  使用 `@rjsf/core` 渲染配置表单。
5.  用户修改配置，点击“保存”。
6.  调用 `lightboxApi.savePluginConfiguration(instanceId, newConfigJson)`。
7.  **Core:** `ConfigurationService` (通过 `WorkspaceManager`) 更新工作区文件中的配置，保存。
8.  **WebViewUI:** 显示 Toast 成功消息，关闭配置模态框。

### 6.5. 启动/停止插件实例
1.  **WebViewUI:** 用户点击插件实例的“启动”/“停止”按钮。
2.  调用 `lightboxApi.startPlugin(instanceId)` 或 `lightboxApi.stopPlugin(instanceId)`。
3.  **Core:** `PluginInstanceController` 执行启动/停止逻辑 (详见 3.2)。
    *   **错误处理:** 如果启动/停止失败，`PluginInstanceController` 更新实例状态为 "Error" 并记录错误信息。`ILoggingService` 记录详细错误。
4.  **WebViewUI:**
    *   轮询或通过未来可能的事件推送机制更新插件实例状态。
    *   如果操作失败，显示 Toast 错误消息，并在插件状态区域显示错误摘要。

### 6.6. 执行 C# 插件命令
1.  **WebViewUI:** (假设某个插件的自定义 UI 或主程序界面提供了触发点)
2.  调用 `lightboxApi.executePluginCommand(instanceId, commandName, payloadJson)`。
3.  **Core:** `PluginInstanceController` 调用对应 C# 插件的 `ExecuteCommand` 方法。
4.  **WebViewUI:** 处理返回结果或错误。

### 6.7. 错误处理与反馈 (通用)
*   **JSBridge 调用失败:** `lightboxApi.ts` 中的调用应包含 `try-catch`，捕获异常后更新 `uiStore` (Zustand) 以显示 Toast 错误。
*   **Core 内部错误:** Core 服务方法应抛出特定异常或返回包含错误信息的结果。JSBridge 将这些错误传递给前端。
*   **插件执行错误:**
    *   `external_process`: 监控进程退出码，读取 `stderr`。
    *   `csharp_library`: `Start/Stop/ExecuteCommand` 方法中的异常被 `PluginInstanceController` 捕获。
*   **日志:** 所有重要操作、错误都应通过 `ILoggingService` 记录。用户可通过 `lightboxApi.openLogFile()` 查看。

## 7. 开发步骤与优先级 (MVP)

### 阶段 1: 核心框架与契约 (优先级最高)
1.  **任务 1.1:** 创建解决方案和项目结构 (`LightBox.WPF`, `LightBox.Core`, `LightBox.PluginContracts`, `LightBox.WebViewUI`)。
2.  **任务 1.2:** 定义 `LightBox.PluginContracts`: `ILightBoxPlugin`, `ILightBoxHostContext`, `LogLevel`。
3.  **任务 1.3:** `LightBox.Core`: 实现基础 `ILoggingService` (例如，简单的控制台和文件输出，暂不引入 Serilog)。
4.  **任务 1.4:** `LightBox.Core`: 实现 `LightBoxHostContext` (提供 `Log` 和 `GetWorkspacePath` - 后者可先返回固定值)。
5.  **任务 1.5:** `LightBox.WPF`:
    *   集成 WebView2 控件。
    *   实现一个非常基础的 `LightBoxJsBridge` (例如，只有一个 `echo(string)` 方法用于测试通信)。
    *   确保 WebView2 能加载一个简单的 `index.html` (来自 `LightBox.WebViewUI` 的 `public` 目录)。
6.  **任务 1.6:** `LightBox.WebViewUI`:
    *   设置 Vite + Preact + TypeScript 项目。
    *   创建一个简单的页面，能调用 `window.lightboxBridge.echo()` 并显示结果。

### 阶段 2: 应用设置与插件发现
1.  **任务 2.1:** `LightBox.Core`: 实现 `ApplicationSettingsService` (加载/保存 `settings.json`)。数据模型包含 `pluginScanDirectories`, `logFilePath`。
2.  **任务 2.2:** `LightBox.Core`: 完善 `ILoggingService` (集成 Serilog，根据 `settings.json` 配置)。
3.  **任务 2.3:** `LightBox.Core`: `PluginManager` - 实现插件发现逻辑 (扫描 `pluginScanDirectories`，解析 `manifest.json`，缓存 `PluginDefinition`)。
4.  **任务 2.4:** `LightBoxJsBridge`: 添加 `getApplicationSettings`, `saveApplicationSettings`, `getAllPluginDefinitions` 接口。
5.  **任务 2.5:** `LightBox.WebViewUI`:
    *   实现设置页面，允许用户配置插件扫描目录、查看日志路径。
    *   实现插件列表页面，展示所有已发现的插件定义。
    *   集成 Zustand 用于状态管理 (`settingsStore`, `pluginStore`)。

### 阶段 3: 工作区管理与插件实例
1.  **任务 3.1:** `LightBox.Core`: `WorkspaceManager` - 实现工作区 CRUD (加载/保存 `workspaces.json` 和单个工作区文件)。数据模型包含 `pluginInstances` 列表。
2.  **任务 3.2:** `LightBoxJsBridge`: 添加工作区管理接口 (`getWorkspaces`, `createWorkspace`, `setActiveWorkspace`, `getActiveWorkspace`) 和插件实例管理接口 (`getPluginsInWorkspace`, `addPluginToWorkspace`, `removePluginFromWorkspace`)。
3.  **任务 3.3:** `LightBox.WebViewUI`:
    *   实现工作区管理 UI (列表、创建、切换)。
    *   在活动工作区视图中显示插件实例列表。
    *   允许从“所有插件”列表添加插件到当前工作区。
    *   更新 Zustand store (`workspaceStore`)。

### 阶段 4: 插件配置与生命周期控制
1.  **任务 4.1:** `LightBox.Core`: `ConfigurationService` - 实现获取/保存插件实例配置 (集成到 `WorkspaceManager` 的工作区数据中)。
2.  **任务 4.2:** `LightBox.Core`: `PluginInstanceController` - 实现 `StartPlugin` 和 `StopPlugin` 逻辑 (针对 `external_process` 和 `csharp_library` 类型)。
    *   实现 `ILightBoxPlugin.Initialize` 的调用。
3.  **任务 4.3:** `LightBoxJsBridge`: 添加配置接口 (`getPluginConfigSchema`, `getPluginConfiguration`, `savePluginConfiguration`) 和实例控制接口 (`startPlugin`, `stopPlugin`)。
4.  **任务 4.4:** `LightBox.WebViewUI`:
    *   实现插件配置模态框，集成 `@rjsf/core` (或类似库) 动态生成表单。
    *   实现启动/停止插件实例的按钮和状态显示。
    *   实现 Toast 通知和插件状态区域的错误反馈。
5.  **任务 4.5:** 创建1-2个简单的示例插件 (一个 `external_process`，一个 `csharp_library`) 用于测试。

### 阶段 5: 进阶功能与完善
1.  **任务 5.1:** `LightBox.Core`: `PluginInstanceController` - 实现 `ExecutePluginCommand` 逻辑。
2.  **任务 5.2:** `LightBoxJsBridge`: 添加 `executePluginCommand` 接口。
3.  **任务 5.3:** `LightBoxJsBridge`: 添加工具类接口 (`openLogFile`, `showSelectDirectoryDialog`, `showMessageBox`)。
4.  **任务 5.4:** `LightBox.WebViewUI`: 根据需要集成这些工具类接口。
5.  **任务 5.5:** 完善错误处理和日志记录。
6.  **任务 5.6:** UI/UX 打磨和测试。

## 8. 技术栈确认 (MVP)

*   **后端 (WPF & Core):** .NET 6/7/8+, C#
*   **前端 UI 容器:** WebView2 (Microsoft.Web.WebView2.Wpf)
*   **前端框架:** Preact (with TypeScript)
*   **前端构建工具:** Vite
*   **前端状态管理:** Zustand
*   **前端 JSON Schema 表单:** `@rjsf/core` (或其他兼容 Preact 的库)
*   **JSON 处理 (C#):** `System.Text.Json`
*   **日志 (C#):** Serilog
*   **插件契约 (C#):** .NET Standard 2.0

## 9. 后续步骤

1.  **评审与确认:** 请您评审此详细设计与开发计划。
2.  **环境搭建:** 准备开发环境，创建 Git 仓库。
3.  **迭代开发:** 按照上述阶段和任务逐步进行开发。
4.  **持续集成/测试:** (MVP 后考虑) 引入单元测试、集成测试。