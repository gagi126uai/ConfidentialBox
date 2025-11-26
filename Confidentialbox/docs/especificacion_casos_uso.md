# Especificación de casos de uso core

A continuación se listan las especificaciones en formato de tabla para cada caso de uso solicitado, siguiendo el contexto actual de ConfidentialBox.

## CORE 1 — IA Security Actions PDF

### CU-001-001: Evaluación de riesgo por sesión de visualización
| ID | Título | Prioridad | Pre-Condiciones | Pasos a reproducir | Resultado Esperado |
| --- | --- | --- | --- | --- | --- |
| CU-001-001 | Evaluación de riesgo por sesión de visualización | Alta | 1. El usuario inició sesión.<br>2. Tiene permisos para abrir el PDF protegido.<br>3. El visor PDF reporta telemetría (acciones y tiempos). | 1. Abrir un documento PDF protegido en el visor.<br>2. Registrar acciones de sesión (capturas, impresiones, copias, cambios de página, eventos de portapapeles, blur/visibilidad, salida de fullscreen).<br>3. Calcular el suspicion score usando las señales y el tiempo por página.<br>4. Normalizar el score a 0–1 y determinar si supera el umbral de alerta.<br>5. Enviar la respuesta con score y recomendaciones al cliente. | Se genera un suspicion score normalizado; si supera el umbral se marca la sesión como sospechosa y se devuelven recomendaciones/alerta al cliente. |

### CU-002-002: Integración con riesgo histórico
| ID | Título | Prioridad | Pre-Condiciones | Pasos a reproducir | Resultado Esperado |
| --- | --- | --- | --- | --- | --- |
| CU-002-002 | Integración con riesgo histórico | Media | 1. Existe un `RiskScore` histórico para el usuario.<br>2. El usuario tiene acceso al documento solicitado.<br>3. Los ajustes de scoring están configurados. | 1. Iniciar la sesión de visualización y solicitar el `RiskScore` histórico del usuario.<br>2. Combinar el `RiskScore` histórico con el suspicion score de la sesión usando los pesos configurados.<br>3. Recalcular el score total y compararlo con umbrales de alerta/monitoreo.<br>4. Registrar en auditoría el score compuesto y la justificación. | El score de la sesión se ajusta con el riesgo histórico; si el score combinado supera umbrales, se dispara alerta o monitoreo priorizado y se registra la decisión. |

### CU-003-003: Reputación de IP y tasa de acciones sospechosas
| ID | Título | Prioridad | Pre-Condiciones | Pasos a reproducir | Resultado Esperado |
| --- | --- | --- | --- | --- | --- |
| CU-003-003 | Reputación de IP y tasa de acciones sospechosas | Media | 1. Telemetría de acciones por minuto habilitada.<br>2. Servicio de reputación IP disponible.<br>3. Usuario autenticado en el visor. | 1. Capturar la IP de la sesión y consultar su reputación.<br>2. Calcular la tasa por minuto de acciones sensibles (screenshot, copiar, imprimir, portapapeles).<br>3. Incrementar el score si la IP es riesgosa o la tasa supera el umbral configurado.<br>4. Determinar si se genera alerta inmediata o se eleva a monitoreo. | El score de la sesión refleja el riesgo de IP y la tasa de acciones; si supera umbrales, se crea alerta o recomendación de revisión. |

### CU-004-004: Secuencia extremo a extremo de scoring
| ID | Título | Prioridad | Pre-Condiciones | Pasos a reproducir | Resultado Esperado |
| --- | --- | --- | --- | --- | --- |
| CU-004-004 | Secuencia extremo a extremo de scoring | Alta | 1. Usuario autenticado con acceso a archivos protegidos.<br>2. Telemetría del visor y servicios de scoring disponibles.<br>3. Umbrales y pesos configurados en AI Settings. | 1. El usuario abre el PDF en el visor y se envían eventos al API.<br>2. El API enruta los eventos al servicio de scoring del visor y al historial del usuario.<br>3. Se calculan suspicion score, riesgo histórico y riesgo por IP/tasa, combinando resultados.<br>4. Se decide si crear alerta, monitorear o aprobar la sesión.<br>5. Se retorna el resultado al cliente y se registra auditoría/alerta. | Flujo completo de scoring ejecutado de punta a punta; se produce la decisión (alerta/monitoreo/aprobado) y se registran logs y auditoría. |

## CORE 2 — Gestión AI-Agent Security Dashboard

### CU-005-005: Cálculo de threat score por archivo
| ID | Título | Prioridad | Pre-Condiciones | Pasos a reproducir | Resultado Esperado |
| --- | --- | --- | --- | --- | --- |
| CU-005-005 | Cálculo de threat score por archivo | Alta | 1. Usuario autenticado con permiso para subir/compartir archivos.<br>2. Servicio de análisis de malware y reputación disponible.<br>3. Umbrales de threat score configurados. | 1. Subir o compartir un archivo a través del UI.<br>2. El API envía el archivo al servicio de análisis (malware/reputación/extensión/horario/tamaño).<br>3. Se calcula el threat score combinando las señales y se compara con umbrales.<br>4. Si el score supera el límite, se genera alerta y se marca el archivo; si no, se aprueba y registra el resultado. | Se obtiene un threat score normalizado; los archivos sobre umbral quedan marcados con alerta y recomendación, los demás se registran como aprobados. |

### CU-006-006: Riesgo de comportamiento del usuario
| ID | Título | Prioridad | Pre-Condiciones | Pasos a reproducir | Resultado Esperado |
| --- | --- | --- | --- | --- | --- |
| CU-006-006 | Riesgo de comportamiento del usuario | Media | 1. Historial de accesos/descargas del usuario disponible.<br>2. Configuración de anomalías (ubicación/dispositivo/horarios/picos de fallos) definida.<br>3. Usuario autenticado. | 1. Analizar los accesos recientes del usuario (ubicación, dispositivo, horario, tamaño promedio, fallos).<br>2. Detectar anomalías y calcular el `RiskScore` comportamental.<br>3. Comparar el score con umbrales de alerta y monitoreo.<br>4. Registrar resultados y, si aplica, generar alerta o recomendación de seguimiento. | Se produce un `RiskScore` de comportamiento; si supera umbrales se genera alerta o se registra para monitoreo con auditoría del análisis. |

### CU-007-007: Proyección en AI Security Dashboard
| ID | Título | Prioridad | Pre-Condiciones | Pasos a reproducir | Resultado Esperado |
| --- | --- | --- | --- | --- | --- |
| CU-007-007 | Proyección en AI Security Dashboard | Media | 1. Existen threat scores y risk scores recientes.<br>2. AI Security Dashboard operativo con acceso a métricas.<br>3. Permisos de analista habilitados. | 1. Consultar threat scores de archivos y `RiskScore` de usuarios.<br>2. Agregar métricas (top usuarios de riesgo, archivos marcados, severidad temporal, recomendaciones).<br>3. Renderizar los indicadores en el dashboard con filtros y severidades.<br>4. Permitir al analista abrir detalles y tomar acciones sugeridas. | El dashboard muestra métricas actualizadas y navegables; cada tarjeta refleja severidad y recomendaciones basadas en los scores cargados. |

### CU-008-008: Umbrales y decisiones operativas
| ID | Título | Prioridad | Pre-Condiciones | Pasos a reproducir | Resultado Esperado |
| --- | --- | --- | --- | --- | --- |
| CU-008-008 | Umbrales y decisiones operativas | Alta | 1. Umbrales de alerta/alto riesgo definidos en AI Settings.<br>2. Servicios de scoring y alertas activos.<br>3. Analista con rol para ajustar thresholds. | 1. Evaluar los scores provenientes de archivos y comportamiento contra los umbrales vigentes.<br>2. Clasificar cada caso en aprobado, monitoreo o alerta crítica.<br>3. Generar recomendaciones operativas (monitorear, revisión manual, bloqueo) según la clasificación.<br>4. Registrar las decisiones y el threshold aplicado en auditoría. | Cada score queda categorizado según los umbrales; se generan recomendaciones/alertas coherentes y se auditan las decisiones y parámetros usados. |

### CU-009-009: Flujo de revisión y bloqueo manual
| ID | Título | Prioridad | Pre-Condiciones | Pasos a reproducir | Resultado Esperado |
| --- | --- | --- | --- | --- | --- |
| CU-009-009 | Flujo de revisión y bloqueo manual | Alta | 1. Existe una alerta generada por scoring (archivo o comportamiento).<br>2. Reviewer autenticado con permisos para bloquear/accionar.<br>3. Bitácora de auditoría habilitada. | 1. El reviewer abre la alerta en el dashboard y analiza la evidencia (scores, eventos, historial).<br>2. Selecciona una acción (bloquear usuario, monitorear, cerrar) y confirma la decisión.<br>3. El sistema aplica la acción (por ejemplo, `IsActive = false` en el usuario) y registra `SecurityAlertAction` y auditoría.<br>4. Notificar el resultado a los interesados (seguridad/soporte) y actualizar el estado de la alerta. | La decisión queda aplicada y auditada; la alerta cambia de estado, y si el usuario es bloqueado, su acceso queda deshabilitado inmediatamente. |
