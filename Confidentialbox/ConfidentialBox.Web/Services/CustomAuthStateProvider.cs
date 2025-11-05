using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ConfidentialBox.Web.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;

    public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient httpClient)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
    }

    //public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    //{
    //    var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

    //    string? token = null;
    //    try
    //    {
    //        token = await _localStorage.GetItemAsync<string>("authToken");
    //    }
    //    catch (InvalidOperationException)
    //    {
    //        // Estamos en prerender; JS aún no está disponible
    //        return new AuthenticationState(anonymous);
    //    }

    //    if (string.IsNullOrWhiteSpace(token))
    //        return new AuthenticationState(anonymous);

    //    // construir ClaimsIdentity desde el token...
    //    var identity = /* ... */
    //    return new AuthenticationState(new ClaimsPrincipal(identity));
    //}


    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        string? token = null;
        try
        {
            token = await _localStorage.GetItemAsync<string>("authToken");
        }
        catch (InvalidOperationException)
        {
            // Durante el prerender todavía no está disponible el almacenamiento local
            return new AuthenticationState(anonymous);
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return new AuthenticationState(anonymous);
        }

        var principal = BuildPrincipalFromToken(token);
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return new AuthenticationState(principal);
    }

    public void NotifyUserAuthentication(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var principal = BuildPrincipalFromToken(token);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    public void NotifyUserLogout()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
    }

    private ClaimsPrincipal BuildPrincipalFromToken(string token)
    {
        try
        {
            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);
        return token.Claims;
    }
}
