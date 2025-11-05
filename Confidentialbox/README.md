# ConfidentialBox - Guía de ejecución local

## Requisitos previos
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server local (SQL Server Express, LocalDB o equivalente) con acceso de escritura
- Herramienta `dotnet-ef` (instalar con `dotnet tool install --global dotnet-ef` si aún no la tienes)
- Certificados HTTPS de desarrollo confiables (`dotnet dev-certs https --trust`)

## Configuración inicial
1. **Restaurar dependencias**
   ```bash
   dotnet restore ConfidentialBox.sln
   ```
2. **Configurar la cadena de conexión**
   - Edita `ConfidentialBox.API/appsettings.json` (o crea un `appsettings.Development.json`) y apunta `ConnectionStrings:DefaultConnection` a tu instancia local de SQL Server. Ejemplo con LocalDB:
     ```json
     {
       "ConnectionStrings": {
         "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ConfidentialBoxDB;Trusted_Connection=True;TrustServerCertificate=True;"
       }
     }
     ```
3. **Sincronizar la base de datos**
   ```bash
   dotnet ef database update \
     --project ConfidentialBox.Infrastructure \
     --startup-project ConfidentialBox.API
   ```
   Esto aplica todas las migraciones y deja la base de datos lista con los datos iniciales.

## Ejecución de los servicios
Para levantar backend (API + Swagger) y frontend (Blazor Server) en paralelo puedes usar dos terminales:

1. **API + Swagger**
   ```bash
   dotnet run --project ConfidentialBox.API --launch-profile https
   ```
   - Swagger UI quedará disponible en `https://localhost:7233/swagger`.
   - El backend se autopopula con un usuario administrador inicial (`admin`/`admin`).

2. **UI (Blazor Server)**
   ```bash
   dotnet run --project ConfidentialBox.Web
   ```
   - La interfaz se expone en `https://localhost:5001`.
   - La UI utiliza el valor `ApiBaseUrl` definido en `ConfidentialBox.Web/appsettings.json`. Asegúrate de que apunte al mismo puerto HTTPS que usa la API (`https://localhost:7233/` por defecto).

> 💡 Visual Studio y Rider pueden lanzar ambos proyectos simultáneamente usando el perfil de solución incluido (`ConfidentialBox.slnLaunch.user`).

## Credenciales y seguridad
- **Cuenta administrador por defecto:** usuario `admin`, contraseña `admin`. Se crea automáticamente si no existe.
- **Protección de rutas:** todas las páginas internas requieren autenticación con JWT; si la sesión no es válida la aplicación redirige al login.
- **Registro de usuarios:**
  - Público sólo cuando está habilitado desde *Configuración → Seguridad de acceso* (toggle “Permitir registro público de usuarios”).
  - El endpoint `api/auth/register` respeta esa preferencia; la UI también bloquea la vista de registro si está deshabilitado.
  - El registro de usuarios nunca exige token, el resto de endpoints sí.

## Flujo recomendado tras el primer inicio
1. Inicia ambos proyectos (API y Web) y abre `https://localhost:5001`.
2. Accede con `admin/admin`.
3. Visita la página **Configuración** para ajustar:
   - Estrategia de almacenamiento de archivos.
   - Servidor de correo y destinatarios.
   - Disponibilidad del registro público de usuarios.
4. Si necesitas invitar usuarios manualmente mientras el registro público está desactivado, utiliza el módulo **Usuarios** dentro del panel de administración.

## Notas adicionales
- El token JWT se almacena en `localStorage` y se adjunta automáticamente en cada llamada HTTP desde la UI.
- Swagger requiere HTTPS; si ves errores de certificado ejecuta `dotnet dev-certs https --trust` y reinicia los proyectos.
- Ajusta los puertos si tu entorno ya los ocupa; recuerda mantener sincronizados `ApiBaseUrl` (UI) y `ClientApp:BaseUrl` (API).
