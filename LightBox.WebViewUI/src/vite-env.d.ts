/// <reference types="vite/client" />

// Extend the Window interface for WebView2
interface Window {
    chrome?: {
        webview?: {
            postMessage: (message: any) => void;
            addEventListener: (type: string, listener: (event: MessageEvent) => void) => void;
            removeEventListener: (type: string, listener: (event: MessageEvent) => void) => void;
            // If you use hostObjects, you might declare them here too, though we are moving away from it.
            // hostObjects?: {
            //     [key: string]: any; // Or more specific types if known
            // };
        };
    };
}
