# CORE 1 — Secuencia integrada (CU-001-001 a CU-004-004)

Esta secuencia consolida los cuatro casos de uso del núcleo **IA Security Actions PDF**:
- **CU-001-001:** Evaluación de riesgo por sesión de visualización (telemetría + score base 0–1).
- **CU-002-002:** Integración con riesgo histórico del usuario para reforzar o atenuar el score de la sesión.
- **CU-003-003:** Reputación de IP y tasa de acciones sospechosas por minuto como factores adicionales.
- **CU-004-004:** Orquestación extremo a extremo desde el visor hasta la respuesta con recomendaciones.

La intención es mostrar un flujo coherente, no meramente concatenado: la misma sesión alimenta el cálculo base, luego se ajusta con riesgo histórico y reputación de red antes de decidir alertas o monitoreo.

## Diagrama de secuencia integrado
```plantuml
@startuml
title CORE 1 - Secuencia integrada de scoring (CU-001-001 a CU-004-004)
actor Usuario
participant "Visor PDF" as Visor
participant "PDFViewerController" as Controller
participant "PDFViewerAIService" as Scoring
participant "UserRiskProfileRepo" as Perfil
participant "Reputación IP / ThreatIntel" as Reputation
participant "AlertLog" as Alertas

Usuario -> Visor : Interactúa con el PDF
Visor -> Controller : Telemetría de sesión\n(captura, imprimir, copiar, blur, fullscreen, tiempos)
Controller -> Scoring : Solicitar SuspicionScore (CU-001-001)
Scoring -> Scoring : Ponderar eventos + tasa sospechosa/minuto
Scoring -> Perfil : Consultar RiskScore histórico (CU-002-002)
Scoring -> Reputation : Consultar reputación IP + drift de IP (CU-003-003)
Scoring -> Scoring : Ajustar score con riesgo histórico y reputación
Scoring -> Scoring : Normalizar 0–1 y armar razones
Scoring -> Alertas : Registrar score, razones y severidad
alt Score supera umbral de alerta
  Scoring -> Controller : Score + recomendación de alerta (CU-004-004)
  Controller -> Visor : Mostrar alerta inmediata y acciones
else Score bajo umbral
  Scoring -> Controller : Score + recomendación de monitoreo
  Controller -> Visor : Mostrar estado normal con score
end
@enduml
```

### Cómo leer el diagrama
- **Telemetría y cálculo base (CU-001-001):** El visor envía eventos de la sesión; el servicio calcula pesos y la tasa sospechosa/minuto.
- **Riesgo histórico (CU-002-002):** Se consulta el perfil del usuario para reforzar o suavizar el score de la sesión.
- **Reputación IP (CU-003-003):** Se agrega el factor de reputación y drift de IP para complementar el contexto de red.
- **Decisión extremo a extremo (CU-004-004):** El controlador responde al visor con recomendaciones diferenciadas según umbrales.

