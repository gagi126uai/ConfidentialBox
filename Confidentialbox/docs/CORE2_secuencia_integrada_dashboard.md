# CORE 2 — Secuencia integrada del AI Security Dashboard (CU-005-005 a CU-009-009)

Secuencia combinada para el núcleo **Gestión AI-Agent Security Dashboard**, mostrando cómo los casos de uso se encadenan:
- **CU-005-005:** Cálculo de threat score por archivo al momento de subir/compartir.
- **CU-006-006:** Riesgo de comportamiento del usuario (`RiskScore`) que alimenta la evaluación.
- **CU-007-007:** Proyección en el AI Security Dashboard de scores, alertas y métricas.
- **CU-008-008:** Aplicación de umbrales y decisiones operativas (monitoreo, revisión, cuarentena/bloqueo).
- **CU-009-009:** Flujo de revisión y bloqueo manual con auditoría.

El flujo refleja dependencias reales: el `ThreatScore` se calcula primero, se ajusta con riesgo de comportamiento, se compara con umbrales para decidir acciones y, finalmente, se proyecta en el dashboard y en la revisión humana.

## Diagrama de secuencia integrado (dashboard, umbrales y scoring)
```plantuml
@startuml
title CORE 2 - Secuencia integrada del AI Security Dashboard
actor Usuario
actor Revisor
participant "UI de subida/compartir" as UI
participant "Security API" as API
participant "AISecurityService" as FileScoring
participant "BehaviorRiskService" as Behavior
participant "AlertWorkflow" as Workflow
participant "AI Security Dashboard" as Dashboard
database "ThreatScores / Alerts" as Repo

autonumber
Usuario -> UI : Subir/compartir archivo
UI -> API : Enviar archivo + metadatos
API -> FileScoring : Solicitar ThreatScore (CU-005-005)
FileScoring -> Behavior : Consultar RiskScore usuario (CU-006-006)
Behavior --> FileScoring : RiskScore + anomalías
FileScoring -> FileScoring : Ponderar señales + riesgo usuario
FileScoring -> Repo : Registrar score y razones
FileScoring -> API : Score + severidad preliminar
API -> Workflow : Evaluar umbrales (CU-008-008)
Workflow -> Repo : Crear alerta si supera umbral
Workflow -> Dashboard : Actualizar métricas y paneles (CU-007-007)
alt Requiere revisión humana
  Workflow -> Revisor : Notificar caso para revisión (CU-009-009)
  Revisor -> Workflow : Decisión (monitoreo/mitigar/bloquear)
  Workflow -> Repo : Registrar acción y auditoría
else Solo monitoreo automatizado
  Workflow -> Dashboard : Marcar como monitoreo
end
Workflow -> UI : Devolver recomendaciones/acciones
@enduml
```

### Cómo leer el diagrama
- **ThreatScore (CU-005-005):** El archivo se evalúa inmediatamente y el score se registra con razones.
- **Riesgo de comportamiento (CU-006-006):** El score del archivo se ajusta con el `RiskScore` del usuario para reflejar contexto.
- **Dashboard (CU-007-007):** Los resultados alimentan métricas y rankings del panel de seguridad.
- **Umbrales (CU-008-008):** Un flujo de decisiones aplica thresholds para definir si se genera alerta o solo monitoreo.
- **Revisión manual (CU-009-009):** Cuando se necesita intervención humana, el revisor decide y el sistema audita la resolución.

