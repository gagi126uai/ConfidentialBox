# Casos de uso y diagramas solicitados (IA Security)

Este documento lista únicamente los casos de uso core solicitados. Cada CU incluye un diagrama de secuencia propio y, para cada núcleo, se mantienen los diagramas consolidados de casos de uso y robustez.

## CORE 1 — IA Security Actions PDF

### Casos de uso

#### CU-001-001: Evaluación de riesgo por sesión de visualización
Calcula el `SuspicionScore` 0–1 combinando capturas, impresiones, copias, cambios rápidos de página, eventos de portapapeles, blur/visibilidad, salidas de fullscreen, lectura anómala y tiempo por página.

**Diagrama de caso de uso (CU-001-001)**
```plantuml
@startuml
left to right direction
actor Usuario
rectangle "IA Security Actions PDF" {
  usecase "Evaluar riesgo\npor sesión" as UC1001
}
Usuario --> UC1001
UC1001 : Captura/copiar/imprimir
UC1001 : Blur/visibility/fullscreen
UC1001 : Lectura anómala + tiempo por página
@enduml
```

**Diagrama de secuencia (CU-001-001)**
```plantuml
@startuml
actor Usuario
participant "PDF Viewer UI" as UI
participant "PDFViewerController" as Ctrl
participant "PDFViewerAIService" as AI
database "Alerts/Logs" as Logs

Usuario -> UI : Interacción de lectura
UI -> Ctrl : Eventos de sesión (captura/copiar/impresión)
Ctrl -> AI : Solicita SuspicionScore
AI -> AI : Evalúa pesos por evento y ratio por minuto
AI -> Logs : Persiste score + razones
AI --> Ctrl : SuspicionScore + recomendaciones
Ctrl --> UI : Respuesta al usuario
@enduml
```

#### CU-002-002: Integración con riesgo histórico
Inyecta el `RiskScore` del usuario y aplica un bono por anomalías previas para reforzar perfiles riesgosos.

**Diagrama de caso de uso (CU-002-002)**
```plantuml
@startuml
left to right direction
actor "Analista de seguridad" as Analyst
actor "Motor de scoring" as Engine
rectangle "IA Security Actions PDF" {
  usecase "Integrar RiskScore\nen visor" as UC1002
}
Analyst --> UC1002
Engine --> UC1002
UC1002 : Leer RiskScore histórico
UC1002 : Aplicar bono por anomalías
UC1002 : Enriquecer SuspicionScore
@enduml
```

**Diagrama de secuencia (CU-002-002)**
```plantuml
@startuml
actor "Analista de seguridad" as Analyst
participant "PDFViewerAIService" as AI
database "UserBehaviorProfile" as Profile
database "SecurityAlerts" as Alerts

AI -> Profile : Obtiene RiskScore acumulado
AI -> Profile : Consulta anomalías previas
AI -> AI : Ajusta SuspicionScore base con RiskScore
AI -> Alerts : Registra score final y contexto histórico
Analyst --> Alerts : Revisa alertas enriquecidas
@enduml
```

#### CU-003-003: Reputación de IP y tasa de acciones sospechosas
Sube el score si la IP cambia respecto de accesos previos o si la tasa de screenshot/copiar/imprimir supera el umbral por minuto.

**Diagrama de caso de uso (CU-003-003)**
```plantuml
@startuml
left to right direction
actor Usuario
actor "Sistema de reputación" as Reputation
rectangle "IA Security Actions PDF" {
  usecase "Ajustar score por IP\ny tasa sospechosa" as UC1003
}
Usuario --> UC1003
Reputation --> UC1003
UC1003 : Comparar IP vs. histórico
UC1003 : Calcular tasa acciones/min
UC1003 : Ajustar SuspicionScore
@enduml
```

**Diagrama de secuencia (CU-003-003)**
```plantuml
@startuml
actor Usuario
participant "PDF Viewer UI" as UI
participant "PDFViewerController" as Ctrl
participant "PDFViewerAIService" as AI
database "Historial de IPs" as IPs

Usuario -> UI : Abre PDF
UI -> Ctrl : Envía IP y eventos
Ctrl -> AI : Solicita scoring contextual
AI -> IPs : Compara IP actual vs. previas
AI -> AI : Calcula ratio de acciones por minuto
AI --> Ctrl : Score ajustado por IP/reputación
Ctrl --> UI : Score + alertas locales
@enduml
```

#### CU-004-004: Secuencia extremo a extremo de scoring
Flujo Usuario → UI → API → servicio de scoring → análisis de comportamiento → respuesta con score y recomendaciones.

**Diagrama de caso de uso (CU-004-004)**
```plantuml
@startuml
left to right direction
actor Usuario
actor "Analista de seguridad" as Analyst
rectangle "IA Security Actions PDF" {
  usecase "Orquestar scoring\nend-to-end" as UC1004
}
Usuario --> UC1004
Analyst --> UC1004
UC1004 : UI → API → Servicio AI
UC1004 : Integrar RiskScore + IP
UC1004 : Devolver score + recomendaciones
@enduml
```

**Diagrama de secuencia (CU-004-004)**
```plantuml
@startuml
actor Usuario
participant "PDF Viewer UI" as UI
participant "PDFViewerController" as Ctrl
participant "PDFViewerAIService" as AI
participant "AISecurityService" as Behavior
database "UserBehaviorProfile" as Profile
database "SecurityAlerts" as Alerts

Usuario -> UI : Interactúa con el visor
UI -> Ctrl : Envía sesión completa
Ctrl -> AI : Llama scoring del visor (CU-001)
AI -> Behavior : Enriquecer con RiskScore (CU-002)
Behavior -> Profile : Lee historial / accesos
AI -> AI : Ajusta por IP y tasa (CU-003)
AI -> Alerts : Guarda alerta y score final
AI --> Ctrl : Respuesta con SuspicionScore
Ctrl --> UI : Notifica score + recomendaciones
@enduml
```

### Diagramas de contexto (CORE 1)
**Pertenece a:** IA Security Actions PDF

Diagrama de casos de uso consolidado:
```plantuml
@startuml
actor "Usuario" as User
actor "Analista de seguridad" as Analyst

rectangle "IA Security Actions PDF" as System {
  usecase "CU-001-001\nEvaluación de riesgo por sesión" as UC1
  usecase "CU-002-002\nIntegración con riesgo histórico" as UC2
  usecase "CU-003-003\nIP + tasa sospechosa" as UC3
  usecase "CU-004-004\nSecuencia extremo a extremo" as UC4
}

User --> UC1
User --> UC3
User --> UC4
Analyst --> UC2
UC2 .> UC1 : «extiende»
UC3 .> UC1 : «refuerza»
UC4 .> UC1 : «orquesta»
@enduml
```

Diagrama de robustez consolidado:
```plantuml
@startuml
actor "Usuario" as User
actor "Analista de seguridad" as Analyst
boundary "PDF Viewer UI" as BoundaryUI
control "PDFViewerController" as Ctrl
control "PDFViewerAIService" as Scoring
control "AISecurityService\n(AnalyzeUserBehaviorAsync)" as Behavior
entity "UserBehaviorProfile" as Profile
entity "FileAccess" as Access
entity "Alertas/Recomendaciones" as Alerts

User --> BoundaryUI : Genera eventos (captura, copiar, imprimir)
BoundaryUI --> Ctrl : Envía sesión y metadatos
Ctrl --> Scoring : CU-001-001
Scoring --> Behavior : CU-002-002
Behavior --> Access : Historial de accesos
Behavior --> Profile : RiskScore + anomalías
Scoring --> Alerts : Guarda SuspicionScore + razones
Scoring --> Ctrl : Devuelve score y recomendaciones
Ctrl --> BoundaryUI : Respuesta al usuario

Analyst --> Behavior : Ajusta pesos/umbrales históricos
Analyst --> Alerts : Revisa alertas de visor PDF
@enduml
```

## CORE 2 — Gestión AI-Agent Security Dashboard

### Casos de uso

#### CU-012-012: Cálculo de threat score por archivo
Combina señales directas del archivo y contexto de subida antes de almacenarlo.

**Diagrama de caso de uso (CU-012-012)**
```plantuml
@startuml
left to right direction
actor Usuario
rectangle "AI-Agent Security Dashboard" {
  usecase "Calcular threat score\npor archivo" as UC12012
}
Usuario --> UC12012
UC12012 : Extensión / tamaño / horario
UC12012 : Probabilidad malware / exfiltración
UC12012 : Decisión permitir/alertar
@enduml
```

**Diagrama de secuencia (CU-012-012)**
```plantuml
@startuml
actor Usuario
participant "File Upload UI" as UI
participant "UploadController" as Ctrl
participant "AISecurityService" as AI
database "FileScanResult" as Scan

Usuario -> UI : Selecciona archivo y envía
UI -> Ctrl : Metadatos + archivo
Ctrl -> AI : Solicita threatScore
AI -> AI : Evalúa señales (extensión, hora, tamaño)
AI -> Scan : Guarda threatScore + flags
AI --> Ctrl : Decisión (permitir/alertar)
Ctrl --> UI : Resultado de la subida
@enduml
```

#### CU-013-013: Riesgo de comportamiento del usuario
Deriva `RiskScore` continuo con anomalías de ubicación, dispositivo, horario y accesos fallidos.

**Diagrama de caso de uso (CU-013-013)**
```plantuml
@startuml
left to right direction
actor Usuario
actor "Analista de seguridad" as Analyst
rectangle "AI-Agent Security Dashboard" {
  usecase "Calcular RiskScore\nde usuario" as UC13013
}
Usuario --> UC13013
Analyst --> UC13013
UC13013 : Ubicación/dispositivo dominante
UC13013 : Spikes de accesos fallidos
UC13013 : Horarios atípicos + rareza
@enduml
```

**Diagrama de secuencia (CU-013-013)**
```plantuml
@startuml
actor Usuario
participant "AISecurityService" as AI
database "FileAccess" as Access
database "UserBehaviorProfile" as Profile

Usuario -> AI : Genera eventos de acceso
AI -> Access : Lee historial (ubicación/dispositivo)
AI -> AI : Detecta spikes y anomalías
AI -> Profile : Actualiza RiskScore y evidencias
AI --> Usuario : Recomendaciones (si aplica)
@enduml
```

#### CU-014-014: Proyección en AI Security Dashboard
Usa `RiskScore`, alertas y `FileScanResult` para métricas, tendencias y recomendaciones.

**Diagrama de caso de uso (CU-014-014)**
```plantuml
@startuml
left to right direction
actor "Analista de seguridad" as Analyst
rectangle "AI-Agent Security Dashboard" {
  usecase "Proyectar métricas\ny tendencias" as UC14014
}
Analyst --> UC14014
UC14014 : KPIs diarios y severidad
UC14014 : Top usuarios/archivos riesgosos
UC14014 : Recomendaciones accionables
@enduml
```

**Diagrama de secuencia (CU-014-014)**
```plantuml
@startuml
actor "Analista de seguridad" as Analyst
participant "AI-Agent Security Dashboard" as Dashboard
database "SecurityAlerts" as Alerts
database "UserBehaviorProfile" as Profile
database "FileScanResult" as Scan

Analyst -> Dashboard : Consulta vista diaria
Dashboard -> Alerts : Top alertas y severidad
Dashboard -> Profile : Usuarios con RiskScore alto
Dashboard -> Scan : Tendencias de threatScore
Dashboard --> Analyst : KPIs y recomendaciones
@enduml
```

#### CU-015-015: Umbrales y decisiones operativas
Aplica umbrales para marcar alertas y guiar monitoreo, revisión o bloqueo.

**Diagrama de caso de uso (CU-015-015)**
```plantuml
@startuml
left to right direction
actor "Analista de seguridad" as Analyst
rectangle "AI-Agent Security Dashboard" {
  usecase "Aplicar umbrales\ny acciones" as UC15015
}
Analyst --> UC15015
UC15015 : Thresholds Suspicious/HighRisk
UC15015 : Generar/severidad de alertas
UC15015 : Recomendar acción operativa
@enduml
```

**Diagrama de secuencia (CU-015-015)**
```plantuml
@startuml
actor "Analista de seguridad" as Analyst
participant "AISecurityService" as AI
database "SecurityAlerts" as Alerts

AI -> AI : Compara threatScore / RiskScore con umbrales
AI -> Alerts : Crea/actualiza alerta con severidad
Alerts --> Analyst : Notificación y prioridad
Analyst -> Alerts : Selecciona acción recomendada
@enduml
```

#### CU-016-016: Flujo de revisión y bloqueo manual
Permite al revisor confirmar `blockuser`, auditando la decisión.

**Diagrama de caso de uso (CU-016-016)**
```plantuml
@startuml
left to right direction
actor "Analista de seguridad" as Analyst
rectangle "AI-Agent Security Dashboard" {
  usecase "Revisar y bloquear\nusuario" as UC16016
}
Analyst --> UC16016
UC16016 : Consultar alerta crítica
UC16016 : Evaluar evidencias y RiskScore
UC16016 : Confirmar blockuser + auditoría
@enduml
```

**Diagrama de secuencia (CU-016-016)**
```plantuml
@startuml
actor "Analista de seguridad" as Analyst
participant "AISecurityController (review)" as Review
database "SecurityAlerts" as Alerts
database "UserBehaviorProfile" as Profile

Analyst -> Review : Solicita revisar alerta crítica
Review -> Alerts : Obtiene detalle y evidencias
Review -> Profile : Consulta RiskScore + historial
Review -> Alerts : Registra veredicto (ej. blockuser)
Review --> Analyst : Confirmación de acción y auditoría
@enduml
```

### Diagramas de contexto (CORE 2)
**Pertenece a:** Gestión AI-Agent Security Dashboard

Diagrama de casos de uso consolidado:
```plantuml
@startuml
actor "Usuario" as User
actor "Analista de seguridad" as Analyst

rectangle "AI-Agent Security Dashboard" as System {
  usecase "CU-012-012\nThreat score por archivo" as UC10
  usecase "CU-013-013\nRiesgo de comportamiento" as UC11
  usecase "CU-014-014\nProyección en dashboard" as UC12
  usecase "CU-015-015\nUmbrales y decisiones" as UC13
  usecase "CU-016-016\nRevisión y bloqueo manual" as UC14
}

User --> UC10
User --> UC11
User --> UC13
Analyst --> UC12
Analyst --> UC14
UC11 .> UC10 : «alimenta RiskScore»
UC10 .> UC12 : «puebla dashboard»
UC13 .> UC12 : «define severidad»
UC14 .> UC13 : «ejecuta decisión»
@enduml
```

Diagrama de robustez consolidado:
```plantuml
@startuml
actor "Usuario" as User
actor "Analista de seguridad" as Analyst
boundary "File Upload UI" as UploadUI
control "UploadController" as UploadCtrl
control "AISecurityService" as AISec
control "AISecurityController\n(review)" as ReviewCtrl
entity "FileScanResult" as Scan
entity "SecurityAlert" as Alert
entity "UserBehaviorProfile" as Profile
entity "FileAccess" as Access

User --> UploadUI : Sube archivo
UploadUI --> UploadCtrl : CU-012-012
UploadCtrl --> AISec : Calcula threat score
AISec --> Access : Historial y dispositivos
AISec --> Profile : CU-013-013
AISec --> Scan : Guarda threatScore + flags
AISec --> Alert : Crea alerta (CU-015-015)
UploadCtrl --> UploadUI : Decisión y recomendación

Analyst --> ReviewCtrl : CU-016-016
ReviewCtrl --> Alert : Lee alerta pendiente
ReviewCtrl --> Profile : Revisa RiskScore + anomalías
ReviewCtrl --> Alert : Actualiza estado/veredicto
ReviewCtrl --> Scan : Audita evidencia usada
@enduml
```
