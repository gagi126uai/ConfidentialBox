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
                var module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/confidentialbox.js");
                try
                {
                    var payload = await module.InvokeAsync<ClientContextPayload?>("collectClientContext");
                    if (payload != null)
                    {
                        _context = payload;
                    }
                }
                finally
                {
                    try
                    {
                        await module.DisposeAsync();
                    }
                    catch
                    {
                        // ignore dispose issues
                    }
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
