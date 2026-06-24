# Handoff: ABDFiles (Gestor Documental) — Fases 1-4 Completas

## 🎯 Objetivo del Proyecto

Implementar un gestor documental multi-tenant completo para el ecosistema ABD con versionado inmutable, deduplicación intra-tenant, múltiples proveedores de almacenamiento, ciclo de vida con retención y purga, webhooks firmados y control de concurrencia.

## 📊 Estado Actual

*   **Puerto de Desarrollo**: `5005`
*   **Tests**: 35/35 pasando con Vitest
*   **Certificación**: `SYSTEM CERTIFIED - ERA 11 COMPLIANT`
*   **Dashboard**: Funcional con datos mock para demostración (4 pestañas: Suite, LMS, Seguridad, Gobernanza)

## 🛫 Archivos en Vuelo (Pendientes / Futuros)

*   **Fase 5.2**: Migración de webhooks síncronos a Event Bus (Kafka/RabbitMQ).
*   **Fase 5.3**: Monitorización de salud de sockets y observabilidad SOC2.
*   **Fase 5.4**: Indexador Elasticsearch/OpenSearch para búsqueda en metadatos.
*   **Fase 5.5**: Integración ABAC completa con `GuardianEngine`.
*   **Fase 6.1**: Despliegue Blue-Green y configuraciones avanzadas de Vercel.
*   **Fase 6.2**: Cifrado criptográfico a nivel de campo en MongoDB.
*   **Fase 6.3**: Event Sourcing para historial de transacciones de storage.

## 🛠️ Archivos Modificados/Creados

### Modelos (Mongoose)
*   `src/models/Document.ts`: Asset documental con ciclo de vida completo.
*   `src/models/DocumentVersion.ts`: Versiones inmutables con hash SHA-256.
*   `src/models/DocumentEvent.ts`: Auditoría de eventos.
*   `src/models/AssetSpaceLink.ts`: Enlace polimórfico many-to-many documentos ↔ espacios.
*   `src/models/StorageConnector.ts`: Conector de almacenamiento configurable.
*   `src/models/DeletionJob.ts`: Trabajos de purga física.
*   `src/models/LegalHold.ts`: Bloqueos legales.
*   `src/models/IdempotencyKey.ts`: Claves de idempotencia.

### Servicios
*   `src/services/document-service.ts`: CRUD, deduplicación SHA-256, versionado, borrado lógico, worker de purga.
*   `src/services/storage-service.ts`: Abstracción multi-provider con resolución dinámica.
*   `src/services/storage/storage-providers.ts`: 4 implementaciones (Cloudinary, S3, Google Drive, OneDrive).
*   `src/services/connector-service.ts`: CRUD de conectores, validación y test de conexión.
*   `src/services/webhook-service.ts`: Eventos firmados HMAC-SHA256 con reintento exponencial.
*   `src/services/legal-hold-service.ts`: Gestión de bloqueos legales con múltiples holds activos.
*   `src/services/space-link-service.ts`: Vinculación documentos ↔ espacios.
*   `src/services/integration-logs-service.ts`: Replicación a ABDLogs.

### API Routes
*   `src/app/api/v1/documents/route.ts`: CRUD de documentos.
*   `src/app/api/v1/documents/[assetId]/versions/route.ts`: Gestión de versiones.
*   `src/app/api/v1/documents/[assetId]/events/route.ts`: Consulta de eventos de auditoría.
*   `src/app/api/v1/documents/[assetId]/holds/route.ts`: Gestión de bloqueos legales.
*   `src/app/api/v1/documents/[assetId]/metadata/route.ts`: Actualización de metadatos.
*   `src/app/api/v1/connectors/route.ts`: CRUD de conectores de almacenamiento.
*   `src/app/api/v1/connectors/[connectorId]/test/route.ts`: Test de conexión física.
*   `src/app/api/cron/data-lifecycle/route.ts`: Worker CRON de purga programada.

### Librerías y Utilidades
*   `src/lib/rbac.ts`: Matriz de permisos por rol (FILE_VIEWER, FILE_EDITOR, FILE_ADMIN, FILE_AUDITOR).
*   `src/lib/abac.ts`: Integración con GuardianEngine para ABAC.
*   `src/lib/idempotency.ts`: Helper de idempotencia con caché en MongoDB.
*   `src/lib/mock-dashboard-data.ts`: Datos mock para el dashboard de demostración.
*   `src/lib/utils.ts`: Utilidades generales.

### Componentes UI
*   `src/components/admin/DashboardClient.tsx`: Panel de 4 pestañas con métricas del ecosistema.
*   `src/components/admin/DashboardSkeleton.tsx`: Esqueleto de carga monospace.
*   `src/components/admin/UploadZone.tsx`: Zona de arrastre para subida de documentos.
*   `src/components/admin/DocumentDetailClient.tsx`: Vista forense de documento.
*   `src/components/admin/tabs/SuiteTab.tsx`: KPIs generales de la suite.
*   `src/components/admin/tabs/LmsTab.tsx`: Gráficos de distribución de calificaciones.
*   `src/components/admin/tabs/SecurityTab.tsx`: Adopción MFA y accesos fallidos.
*   `src/components/admin/tabs/GovernanceTab.tsx`: Utilización de almacenamiento por espacio.

### Tests
*   `src/models/__tests__/abdfiles.test.ts`: Tests de modelos, upload, borrado lógico, purga y legal holds (~8 tests).
*   `src/models/__tests__/idempotency.test.ts`: Tests de idempotencia, concurrencia y webhooks (~3 tests).
*   `src/services/__tests__/deduplication.test.ts`: Tests de aislamiento de deduplicación intra-tenant (~2 tests).
*   `src/services/__tests__/connector-service.test.ts`: Tests de validación y creación de conectores (~4 tests).
*   `src/services/__tests__/storage-providers.test.ts`: Tests de enrutamiento a proveedores de almacenamiento (~8 tests).

## ⚠️ Lecciones Aprendidas

1. **Deduplicación intra-tenant con aislamiento estricto**:
   - La consulta de hash debe incluir SIEMPRE el `tenantId` en el filtro.
   - El mismo archivo subido por dos tenants diferentes genera almacenamiento físico independiente.

2. **Conectores de almacenamiento dinámicos**:
   - Al activar un nuevo conector, se desactivan automáticamente los demás (un único proveedor activo por tenant).
   - `forcePathStyle: true` es obligatorio para S3 compatible con MinIO local.

3. **Control de concurrencia optimista**:
   - El contador `version` en el documento previene conflictos de edición concurrente.
   - `VersionConflictError` obliga al cliente a refrescar y reintentar.

4. **Webhooks con reintento exponencial**:
   - Backoff de 2s, 4s, 8s para evitar tormentas de reintentos.
   - Los fallos después de 3 intentos se registran pero no se reintentan automáticamente.

5. **Worker de purga CRON**:
   - Protegido por token secreto para evitar invocaciones externas.
   - Límite de 5 reintentos por trabajo; después se marca como `failed` para revisión manual.

6. **Integración con ABDLogs**:
   - Timeout de 3s para no bloquear flujos documentales.
   - Las fallas de replicación se registran pero no interrumpen la operación principal.
