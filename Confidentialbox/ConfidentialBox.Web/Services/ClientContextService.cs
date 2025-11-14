using System.Text.Json;
using ConfidentialBox.Web.Models;
using Microsoft.JSInterop;

namespace ConfidentialBox.Web.Services;

public class ClientContextService
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private ClientContextPayload? _context;
    private bool _initialized;

    public ClientContextPayload? Current => _context;

    public async Task EnsureInitializedAsync(IJSRuntime jsRuntime)
    {
        if (_initialized)
        {
            return;
        }

        await _initializationLock.WaitAsync();
        try
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                await jsRuntime.InvokeVoidAsync("ConfidentialBox.ensureSecureViewerReady");
                var payload = await jsRuntime.InvokeAsync<ClientContextPayload?>("ConfidentialBox.collectClientContext");
                if (payload != null)
                {
                    _context = payload;
                }
            }
            catch (JSException)
            {
                // Ignore JS errors (offline, blocked scripts, etc.). We'll fall back to server values.
            }
            catch (TaskCanceledException)
            {
                // Ignore cancellations from prerendering scenarios.
            }
            finally
            {
                _initialized = true;
            }
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public void Update(ClientContextPayload payload)
    {
        _context = payload;
        _initialized = true;
    }
}
