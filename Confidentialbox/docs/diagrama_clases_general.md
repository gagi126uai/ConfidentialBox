# Diagrama de clases – ConfidentialBox (vista general)

El siguiente diagrama de clases en PlantUML resume los principales objetos de dominio del proyecto, sus atributos más relevantes y las asociaciones tal como están implementadas en las entidades de `ConfidentialBox.Core` (archivos compartidos, sesiones de visor PDF, perfiles de comportamiento, alertas y flujos de seguridad).

```plantuml
@startuml
set namespaceSeparator none
skinparam classAttributeIconSize 0
skinparam classFontSize 12
skinparam arrowColor DarkSlateGray
left to right direction

class ApplicationUser {
  +string Id
  +string UserName
  +string Email
  +string FirstName
  +string LastName
  +DateTime CreatedAt
  +DateTime? LastLoginAt
  +bool IsActive
  +bool RequiresMFA
  +bool IsBlocked
  ..Operaciones..
  +Block(reason)
  +Unblock()
}

class ApplicationRole {
  +string Id
  +string Name
  +string Description
  +DateTime CreatedAt
  +bool IsSystemRole
}

class RolePolicy {
  +int Id
  +string PolicyName
  +string PolicyValue
  +DateTime CreatedAt
}

class SharedFile {
  +int Id
  +string OriginalFileName
  +string EncryptedFileName
  +string FileExtension
  +long FileSizeBytes
  +string ShareLink
  +string? MasterPassword
  +DateTime UploadedAt
  +DateTime? ExpiresAt
  +bool IsDeleted
  +bool IsBlocked
  +bool IsPDF
  +bool AIMonitoringEnabled
  ..Operaciones..
  +Block(reason)
  +MarkDeleted()
}

class FileAccess {
  +int Id
  +string? AccessedByUserId
  +string AccessedByIP
  +DateTime AccessedAt
  +string Action
  +bool WasAuthorized
  +string? DeviceName
  +string? Location
}

class FilePermission {
  +int Id
  +bool CanView
  +bool CanDownload
  +bool CanDelete
  +DateTime CreatedAt
}

class PDFViewerSession {
  +int Id
  +string SessionId
  +DateTime StartedAt
  +DateTime? EndedAt
  +int PageViewCount
  +int CurrentPage
  +TimeSpan TotalViewTime
  +string ViewerIP
  +bool WasBlocked
  +double SuspicionScore
}

class PDFViewerEvent {
  +int Id
  +string EventType
  +DateTime Timestamp
  +string EventData
  +int? PageNumber
  +bool WasBlocked
}

class FileScanResult {
  +int Id
  +DateTime ScannedAt
  +bool IsSuspicious
  +string? SuspiciousReason
  +double ThreatScore
  +double MalwareProbability
  +double DataExfiltrationProbability
}

class UserBehaviorProfile {
  +int Id
  +double AverageFilesPerDay
  +double AverageFileSizeMB
  +TimeSpan TypicalActiveHoursStart
  +TimeSpan TypicalActiveHoursEnd
  +double AverageSessionDuration
  +int UnusualActivityCount
  +double RiskScore
  +DateTime LastUpdated
}

class SecurityAlert {
  +int Id
  +string AlertType
  +string Severity
  +string Status
  +string Description
  +string DetectedPattern
  +double ConfidenceScore
  +DateTime DetectedAt
}

class SecurityAlertAction {
  +int Id
  +string ActionType
  +string? Notes
  +string? Metadata
  +DateTime CreatedAt
  +string? StatusAfterAction
}

class AuditLog {
  +int Id
  +string Entity
  +string Action
  +string PerformedBy
  +DateTime PerformedAt
  +string Details
}

class UserNotification {
  +int Id
  +string UserId
  +string Title
  +string Message
  +bool IsRead
  +DateTime CreatedAt
}

class UserMessage {
  +int Id
  +string SenderUserId
  +string ReceiverUserId
  +string Content
  +DateTime SentAt
  +bool IsRead
}

class SystemSetting {
  +int Id
  +string Key
  +string Value
  +string Category
  +DateTime CreatedAt
}

class AIModel {
  +int Id
  +string Name
  +string Version
  +string Provider
  +string Description
  +bool Enabled
}

class RecycleBinItem {
  +int Id
  +int SharedFileId
  +DateTime DeletedAt
  +string? DeletedByUserId
}

' Relaciones principales
ApplicationUser "1" -- "*" SharedFile : sube
ApplicationUser "1" -- "*" FileAccess : realiza
ApplicationUser "1" -- "*" AuditLog : genera
ApplicationUser "1" -- "*" UserNotification : recibe
ApplicationUser "1" -- "*" UserMessage : envía/recibe
ApplicationUser "1" -- "1" UserBehaviorProfile : perfila
ApplicationUser "1" -- "*" SecurityAlertAction : ejecuta
ApplicationUser "1" -- "*" SecurityAlert : asociado

ApplicationRole "1" -- "*" RolePolicy
ApplicationRole "1" -- "*" FilePermission

SharedFile "1" -- "*" FileAccess
SharedFile "1" -- "*" FilePermission
SharedFile "1" -- "*" PDFViewerSession
SharedFile "1" -- "*" FileScanResult
SharedFile "1" -- "*" SecurityAlert : puede generar
SharedFile "1" -- "1" RecycleBinItem : opciona

PDFViewerSession "1" -- "*" PDFViewerEvent
PDFViewerSession "*" -- "1" ApplicationUser : viewer

FileAccess "*" -- "1" SharedFile
FilePermission "*" -- "1" SharedFile
FilePermission "*" -- "1" ApplicationRole

FileScanResult "*" -- "1" SharedFile
UserBehaviorProfile "*" -- "1" ApplicationUser
SecurityAlertAction "*" -- "1" SecurityAlert
SecurityAlertAction "*" -- "0..1" SharedFile
SecurityAlertAction "*" -- "0..1" ApplicationUser : target
SecurityAlert "*" -- "0..1" SharedFile
SecurityAlert "*" -- "1" ApplicationUser
RecycleBinItem "*" -- "1" SharedFile

SystemSetting ..> AIModel : configura
AIModel ..> SecurityAlert : alimenta scoring
@enduml
```

> El diagrama prioriza las clases y relaciones de dominio más usadas por los servicios de seguridad, visor PDF y dashboard. Se incluyen atributos clave y asociaciones cardinalizadas para reflejar cómo los objetos se vinculan en la solución.
