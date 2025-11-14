using System;
using System.Collections.Generic;
using System.Linq;

namespace ConfidentialBox.Core.Configuration;

public static class RolePolicyCatalog
{
    public const string PolicyCanUpload = "CanUploadFiles";
    public const string PolicyMaxFileSizeMb = "MaxFileSizeMb";
    public const string PolicyMaxStorageGb = "MaxStorageGb";
    public const string PolicyRequiresMfa = "RequiresMfa";
    public const string PolicyCanShareExternal = "CanShareExternal";
    public const string PolicyCanAccessAi = "CanAccessAISuite";
    public const string PolicySessionTimeoutMinutes = "SessionTimeoutMinutes";
    public const string PolicyRequireExternalJustification = "RequireExternalJustification";
    public const string PolicyExternalApproval = "ExternalShareNeedsApproval";
    public const string PolicyForceWatermark = "ForceWatermark";
    public const string PolicyMaxDailyDownloads = "MaxDailyDownloads";
    public const string PolicyAllowOfflineAccess = "AllowOfflineAccess";
    public const string PolicyAuditRetentionDays = "AuditRetentionDays";
    public const string PolicyCanViewAuditTrail = "CanViewAuditTrail";
    public const string PolicyEnableAiAutoResponse = "EnableAiAutoResponse";
    public const string PolicyAiEscalationLevel = "AiEscalationLevel";

    private const string CategoryOperations = "Operaciones de archivo";
    private const string CategoryAccess = "Acceso y sesiones";
    private const string CategorySharing = "Colaboración externa";
    private const string CategoryProtection = "Protección de documentos";
    private const string CategoryAutomation = "IA y automatización";
    private const string CategoryAudit = "Auditoría y cumplimiento";

    public static readonly IReadOnlyList<RolePolicyDefinition> Definitions = new List<RolePolicyDefinition>
    {
        new RolePolicyDefinition(
            PolicyCanUpload,
            "Subida de archivos",
            "Permite que el rol cargue archivos en la plataforma.",
            PolicyValueType.Boolean,
            CategoryOperations,
            DefaultValue: "true"),
        new RolePolicyDefinition(
            PolicyMaxFileSizeMb,
            "Tamaño máximo por archivo (MB)",
            "Límite individual para cada archivo que suben los usuarios del rol.",
            PolicyValueType.Number,
            CategoryOperations,
            DefaultValue: "500"),
        new RolePolicyDefinition(
            PolicyMaxStorageGb,
            "Cupo de almacenamiento del rol (GB)",
            "Capacidad total que puede consumir el rol antes de impedir nuevas subidas.",
            PolicyValueType.Number,
            CategoryOperations,
            DefaultValue: "5"),
        new RolePolicyDefinition(
            PolicyRequiresMfa,
            "Requiere MFA",
            "Obliga a los miembros del rol a configurar un segundo factor.",
            PolicyValueType.Boolean,
            CategoryAccess,
            DefaultValue: "false"),
        new RolePolicyDefinition(
            PolicySessionTimeoutMinutes,
            "Caducidad de sesión (minutos)",
            "Tiempo máximo de inactividad antes de cerrar la sesión automáticamente.",
            PolicyValueType.Number,
            CategoryAccess,
            DefaultValue: "30"),
        new RolePolicyDefinition(
            PolicyCanShareExternal,
            "Compartir fuera de la organización",
            "Habilita enlaces públicos y compartidos para usuarios externos.",
            PolicyValueType.Boolean,
            CategorySharing,
            DefaultValue: "false"),
        new RolePolicyDefinition(
            PolicyRequireExternalJustification,
            "Solicitar justificación",
            "Pide al usuario justificar el motivo al compartir fuera de la organización.",
            PolicyValueType.Boolean,
            CategorySharing,
            DefaultValue: "true"),
        new RolePolicyDefinition(
            PolicyExternalApproval,
            "Requiere aprobación previa",
            "Impide publicar enlaces externos sin revisión de un responsable.",
            PolicyValueType.Boolean,
            CategorySharing,
            DefaultValue: "false"),
        new RolePolicyDefinition(
            PolicyForceWatermark,
            "Marca de agua obligatoria",
            "Inyecta marcas de agua dinámicas en los documentos sensibles.",
            PolicyValueType.Boolean,
            CategoryProtection,
            DefaultValue: "true"),
        new RolePolicyDefinition(
            PolicyMaxDailyDownloads,
            "Descargas máximas por día",
            "Cantidad máxima de archivos que un usuario del rol puede descargar en 24 horas.",
            PolicyValueType.Number,
            CategoryProtection,
            DefaultValue: "25"),
        new RolePolicyDefinition(
            PolicyAllowOfflineAccess,
            "Permitir acceso sin conexión",
            "Autoriza la sincronización local en dispositivos autorizados.",
            PolicyValueType.Boolean,
            CategoryProtection,
            DefaultValue: "false"),
        new RolePolicyDefinition(
            PolicyCanAccessAi,
            "Acceso al panel de IA",
            "Permite visualizar el AI Security Dashboard y ejecutar escaneos.",
            PolicyValueType.Boolean,
            CategoryAutomation,
            DefaultValue: "false"),
        new RolePolicyDefinition(
            PolicyEnableAiAutoResponse,
            "Respuesta automática IA",
            "Autoriza a la IA a ejecutar acciones automáticas ante amenazas críticas.",
            PolicyValueType.Boolean,
            CategoryAutomation,
            DefaultValue: "false"),
        new RolePolicyDefinition(
            PolicyAiEscalationLevel,
            "Escalamiento automático",
            "Nivel máximo de escalamiento automático permitido por la IA (1 = notifica, 3 = bloquea).",
            PolicyValueType.Number,
            CategoryAutomation,
            DefaultValue: "2"),
        new RolePolicyDefinition(
            PolicyCanViewAuditTrail,
            "Acceso a auditoría",
            "Permite revisar los registros detallados de auditoría y exportarlos.",
            PolicyValueType.Boolean,
            CategoryAudit,
            DefaultValue: "false"),
        new RolePolicyDefinition(
            PolicyAuditRetentionDays,
            "Retención de auditoría (días)",
            "Tiempo que se conservan los eventos detallados antes de anonimizarse.",
            PolicyValueType.Number,
            CategoryAudit,
            DefaultValue: "180")
    };

    public static IReadOnlyDictionary<string, string> GetDefaultValuesForRole(string roleName)
    {
        return roleName.ToLowerInvariant() switch
        {
            "admin" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [PolicyCanUpload] = "true",
                [PolicyMaxFileSizeMb] = "1024",
                [PolicyMaxStorageGb] = "100",
                [PolicyRequiresMfa] = "true",
                [PolicySessionTimeoutMinutes] = "15",
                [PolicyCanShareExternal] = "true",
                [PolicyRequireExternalJustification] = "true",
                [PolicyExternalApproval] = "false",
                [PolicyForceWatermark] = "true",
                [PolicyMaxDailyDownloads] = "100",
                [PolicyAllowOfflineAccess] = "true",
                [PolicyCanAccessAi] = "true",
                [PolicyEnableAiAutoResponse] = "true",
                [PolicyAiEscalationLevel] = "3",
                [PolicyCanViewAuditTrail] = "true",
                [PolicyAuditRetentionDays] = "365"
            },
            "user" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [PolicyCanUpload] = "true",
                [PolicyMaxFileSizeMb] = "500",
                [PolicyMaxStorageGb] = "10",
                [PolicyRequiresMfa] = "false",
                [PolicySessionTimeoutMinutes] = "30",
                [PolicyCanShareExternal] = "false",
                [PolicyRequireExternalJustification] = "true",
                [PolicyExternalApproval] = "true",
                [PolicyForceWatermark] = "true",
                [PolicyMaxDailyDownloads] = "20",
                [PolicyAllowOfflineAccess] = "false",
                [PolicyCanAccessAi] = "false",
                [PolicyEnableAiAutoResponse] = "false",
                [PolicyAiEscalationLevel] = "2",
                [PolicyCanViewAuditTrail] = "false",
                [PolicyAuditRetentionDays] = "180"
            },
            "guest" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [PolicyCanUpload] = "false",
                [PolicyMaxFileSizeMb] = "50",
                [PolicyMaxStorageGb] = "1",
                [PolicyRequiresMfa] = "false",
                [PolicySessionTimeoutMinutes] = "10",
                [PolicyCanShareExternal] = "false",
                [PolicyRequireExternalJustification] = "true",
                [PolicyExternalApproval] = "true",
                [PolicyForceWatermark] = "true",
                [PolicyMaxDailyDownloads] = "5",
                [PolicyAllowOfflineAccess] = "false",
                [PolicyCanAccessAi] = "false",
                [PolicyEnableAiAutoResponse] = "false",
                [PolicyAiEscalationLevel] = "1",
                [PolicyCanViewAuditTrail] = "false",
                [PolicyAuditRetentionDays] = "90"
            },
            _ => Definitions.ToDictionary(d => d.Key, d => d.DefaultValue ?? string.Empty, StringComparer.OrdinalIgnoreCase)
        };
    }
}

public enum PolicyValueType
{
    Boolean,
    Number,
    Text
}

public sealed class RolePolicyDefinition
{
    public RolePolicyDefinition(
        string key,
        string displayName,
        string description,
        PolicyValueType valueType,
        string category,
        string? defaultValue = null,
        IReadOnlyList<string>? options = null)
    {
        Key = key;
        DisplayName = displayName;
        Description = description;
        ValueType = valueType;
        Category = category;
        DefaultValue = defaultValue;
        Options = options ?? Array.Empty<string>();
    }

    public string Key { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public PolicyValueType ValueType { get; }
    public string Category { get; }
    public string? DefaultValue { get; }
    public IReadOnlyList<string> Options { get; }
}
