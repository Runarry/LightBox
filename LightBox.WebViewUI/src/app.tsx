import { useState, useEffect } from 'preact/hooks'
import preactLogo from './assets/preact.svg'
import viteLogo from '/vite.svg'
import './app.css'

// Define the bridge interface for TypeScript
interface LightboxBridge {
  Echo: (message: string) => Promise<string>; // Assuming Echo might be async or return a promise
}

// Access the bridge object from the window
// Use 'any' type for window initially, then cast to a more specific type if needed.
const lightboxBridge = (window as any).lightboxBridge as LightboxBridge | undefined;

export function App() {
  const [count, setCount] = useState(0)
  const [bridgeMessage, setBridgeMessage] = useState('');
  const [bridgeResponse, setBridgeResponse] = useState('');
  const [bridgeAvailable, setBridgeAvailable] = useState(false);

  useEffect(() => {
    if (lightboxBridge) {
      setBridgeAvailable(true);
    } else {
      setBridgeAvailable(false);
      console.warn("lightboxBridge is not available on window object.");
    }
  }, []);

  const handleCallBridge = async () => {
    if (lightboxBridge) {
      try {
        const response = await lightboxBridge.Echo(bridgeMessage);
        setBridgeResponse(response);
      } catch (error) {
        console.error("Error calling bridge Echo:", error);
        setBridgeResponse(`Error: ${error}`);
      }
    } else {
      setBridgeResponse("Bridge not available.");
    }
  };

  return (
    <>
      <div>
        <a href="https://vite.dev" target="_blank">
          <img src={viteLogo} class="logo" alt="Vite logo" />
        </a>
        <a href="https://preactjs.com" target="_blank">
          <img src={preactLogo} class="logo preact" alt="Preact logo" />
        </a>
      </div>
      <h1>Vite + Preact</h1>
      <div class="card">
        <button onClick={() => setCount((count) => count + 1)}>
          count is {count}
        </button>
        <p>
          Edit <code>src/app.tsx</code> and save to test HMR
        </p>
      </div>
      <p class="read-the-docs">
        Click on the Vite and Preact logos to learn more
      </p>

      <hr />
      <h2>JS Bridge Test</h2>
      <p>Bridge Available: {bridgeAvailable ? 'Yes' : 'No (Run in LightBox WPF app)'}</p>
      <div>
        <input 
          type="text" 
          value={bridgeMessage} 
          onInput={(e) => setBridgeMessage((e.target as HTMLInputElement).value)}
          placeholder="Message to C#"
        />
        <button onClick={handleCallBridge} disabled={!bridgeAvailable}>
          Call Echo
        </button>
      </div>
      <div>
        <p>Response from C#:</p>
        <pre><code>{bridgeResponse}</code></pre>
      </div>
    </>
  )
}
