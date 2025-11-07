namespace ConfidentialBox.Core.Configuration;

public static class RolePolicyCatalog
{
    public const string PolicyCanUpload = "CanUploadFiles";
    public const string PolicyMaxFileSizeMb = "MaxFileSizeMb";
    public const string PolicyMaxStorageGb = "MaxStorageGb";
    public const string PolicyRequiresMfa = "RequiresMfa";
    public const string PolicyCanShareExternal = "CanShareExternal";
    public const string PolicyCanAccessAi = "CanAccessAISuite";

    public static readonly IReadOnlyList<RolePolicyDefinition> Definitions = new List<RolePolicyDefinition>
    {
        new(
            PolicyCanUpload,
            "Subida de archivos",
            "Permite que el rol cargue archivos en la plataforma.",
            PolicyValueType.Boolean,
            DefaultValue: "true"),
        new(
            PolicyMaxFileSizeMb,
            "Tamaño máximo por archivo (MB)",
            "Límite individual para cada archivo que suben los usuarios del rol.",
            PolicyValueType.Number,
            DefaultValue: "500"),
        new(
            PolicyMaxStorageGb,
            "Cupo de almacenamiento del rol (GB)",
            "Capacidad total que puede consumir el rol antes de impedir nuevas subidas.",
            PolicyValueType.Number,
            DefaultValue: "5"),
        new(
            PolicyRequiresMfa,
            "Requiere MFA",
            "Obliga a los miembros del rol a configurar un segundo factor.",
            PolicyValueType.Boolean,
            DefaultValue: "false"),
        new(
            PolicyCanShareExternal,
            "Compartir fuera de la organización",
            "Habilita enlaces públicos y compartidos para usuarios externos.",
            PolicyValueType.Boolean,
            DefaultValue: "false"),
        new(
            PolicyCanAccessAi,
            "Acceso al panel de IA",
            "Permite visualizar el AI Security Dashboard y ejecutar escaneos.",
            PolicyValueType.Boolean,
            DefaultValue: "false")
    };

    public static IReadOnlyDictionary<string, string> GetDefaultValuesForRole(string roleName)
    {
        roleName = roleName.ToLowerInvariant();
        return roleName switch
        {
            "admin" => new Dictionary<string, string>
            {
                [PolicyCanUpload] = "true",
                [PolicyMaxFileSizeMb] = "1024",
                [PolicyMaxStorageGb] = "100",
                [PolicyRequiresMfa] = "true",
                [PolicyCanShareExternal] = "true",
                [PolicyCanAccessAi] = "true"
            },
            "user" => new Dictionary<string, string>
            {
                [PolicyCanUpload] = "true",
                [PolicyMaxFileSizeMb] = "500",
                [PolicyMaxStorageGb] = "10",
                [PolicyRequiresMfa] = "false",
                [PolicyCanShareExternal] = "false",
                [PolicyCanAccessAi] = "false"
            },
            "guest" => new Dictionary<string, string>
            {
                [PolicyCanUpload] = "false",
                [PolicyMaxFileSizeMb] = "50",
                [PolicyMaxStorageGb] = "1",
                [PolicyRequiresMfa] = "false",
                [PolicyCanShareExternal] = "false",
                [PolicyCanAccessAi] = "false"
            },
            _ => Definitions.ToDictionary(d => d.Key, d => d.DefaultValue ?? string.Empty)
        };
    }
}

public enum PolicyValueType
{
    Boolean,
    Number,
    Text
}

public record RolePolicyDefinition(
    string Key,
    string DisplayName,
    string Description,
    PolicyValueType ValueType,
    string? DefaultValue = null,
    IReadOnlyList<string>? Options = null)
{
    public IReadOnlyList<string> Options { get; init; } = Options ?? Array.Empty<string>();
}
