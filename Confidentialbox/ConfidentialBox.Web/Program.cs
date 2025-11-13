using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using ConfidentialBox.Web;
using ConfidentialBox.Web.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Configurar HttpClient con la URL base del API
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7233/";
builder.Services.AddScoped<ClientContextService>();
builder.Services.AddScoped<ClientContextMessageHandler>();
builder.Services.AddHttpClient("ConfidentialBox.Api", client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(100);
    })
    .AddHttpMessageHandler<ClientContextMessageHandler>();

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("ConfidentialBox.Api"));

// Agregar Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Registrar servicios personalizados
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPdfViewerService, PdfViewerService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Configurar autenticación
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthStateProvider>());

// Agregar autorización
builder.Services.AddAuthorizationCore();

var app = builder.Build();

// Configurar el pipeline de solicitudes HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
