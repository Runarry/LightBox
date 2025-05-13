<script>
    import { getApplicationSettings } from './services/lightboxApi';
    import { onMount } from 'svelte';

    let settings = null;
    let errorMessage = '';

    onMount(async () => {
        try {
            settings = await getApplicationSettings();
        } catch (error) {
            errorMessage = `Failed to load settings: ${error.message}`;
            console.error(errorMessage, error);
        }
    });
</script>

<main>
    <h1>LightBox WebView UI (Svelte)</h1>

    {#if errorMessage}
        <p style="color: red;">{errorMessage}</p>
    {:else if settings}<!-- Check if settings is not null -->
        <h2>Application Settings:</h2>
        <pre>{JSON.stringify(settings, null, 2)}</pre>
    {:else}
        <p>Loading application settings...</p>
    {/if}
</main>

<style>
    main {
        font-family: sans-serif;
        padding: 20px;
    }
    h1 {
        color: #333;
    }
    pre {
        background-color: #f4f4f4;
        padding: 10px;
        border-radius: 5px;
        overflow-x: auto;
    }
</style>