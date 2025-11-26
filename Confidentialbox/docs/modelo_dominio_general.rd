# Modelo de dominio general (ConfidentialBox)

El siguiente diagrama de dominio muestra las entidades centrales del sistema y sus cardinalidades, alineadas con los flujos de scoring, archivos y alertas de IA.

```plantuml
@startuml
hide circle
skinparam linetype ortho
skinparam class {
  BackgroundColor<<Entidad>> #eef5ff
  BorderColor<<Entidad>> #5b7dbd
}

class Usuario <<Entidad>> {
  +Id
  +Email
  +IsActive
  +RiskScore
}

class SesionVisorPDF <<Entidad>> {
  +Id
  +StartedAt
  +EndedAt
  +SuspicionScore
}

class EventoPDF <<Entidad>> {
  +Id
  +Tipo
  +Timestamp
  +Metadata
}

class Archivo <<Entidad>> {
  +Id
  +Nombre
  +Tamano
  +ThreatScore
}

class VersionArchivo <<Entidad>> {
  +Id
  +Numero
  +Checksum
  +CreadoEn
}

class AccesoArchivo <<Entidad>> {
  +Id
  +TipoAcceso
  +Fecha
  +Resultado
}

class AlertasSeguridad <<Entidad>> {
  +Id
  +Severidad
  +Motivo
  +Estado
}

class AccionAlerta <<Entidad>> {
  +Id
  +TipoAccion
  +CreadoEn
  +Detalles
}

class ConfiguracionAI <<Entidad>> {
  +Id
  +SuspiciousThreshold
  +HighRiskThreshold
}

class MetricaDashboard <<Entidad>> {
  +Id
  +Fecha
  +Categoria
  +Valor
}

Usuario "1" -- "*" SesionVisorPDF : inicia >
SesionVisorPDF "1" -- "*" EventoPDF : registra >
Usuario "1" -- "*" Archivo : posee >
Archivo "1" -- "*" VersionArchivo : versiona >
Usuario "1" -- "*" AccesoArchivo : genera >
Archivo "1" -- "*" AccesoArchivo : queda en
AccesoArchivo "1" -- "0..1" AlertasSeguridad : puede disparar
SesionVisorPDF "1" -- "0..1" AlertasSeguridad : puede escalar
AlertasSeguridad "1" -- "*" AccionAlerta : resoluciones >
Usuario "1" -- "*" AccionAlerta : autoriza >
ConfiguracionAI "1" -- "*" MetricaDashboard : agrega >
AlertasSeguridad "*" -- "*" MetricaDashboard : resume >
Archivo "*" -- "*" MetricaDashboard : agrega >
Usuario "*" -- "*" MetricaDashboard : agrega >
@enduml
```
