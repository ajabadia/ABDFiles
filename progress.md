# 📁 Historial de Progresos — ABDFiles (Gestor Documental)

Este archivo actúa como diario de bitácora y registro cronológico de los avances en el satélite de gestión documental.

---

## [2026-06-21] — Fase 1: Core de Almacenamiento, Modelos e Ingesta

*   **Modelos de Datos MongoDB**: Definidos 7 esquemas Mongoose con tipado estricto `type`:
    *   `Document`: Asset documental con ciclo de vida (`active` → `deleted_pending_retention` → `purged`).
    *   `DocumentVersion`: Versiones inmutables con hash SHA-256 y referencia de almacenamiento.
    *   `DocumentEvent`: Auditoría de eventos por documento.
    *   `AssetSpaceLink`: Enlace polimórfico many-to-many entre documentos y espacios.
    *   `StorageConnector`: Conector de almacenamiento configurable por Tenant.
    *   `DeletionJob`: Trabajos de purga física diferida.
    *   `LegalHold`: Bloqueos legales que detienen purgas.
    *   `IdempotencyKey`: Claves de idempotencia para subidas.

*   **Capa de Servicios**:
    *   `DocumentService`: CRUD de documentos, deduplicación intra-tenant por hash SHA-256, versionado inmutable, borrado lógico con retención configurable y worker de purga (`purgeExpiredDocuments`).
    *   `StorageService`: Abstracción sobre proveedores de almacenamiento con resolución dinámica según el conector activo del Tenant.
    *   `ConnectorService`: CRUD de conectores de almacenamiento por Tenant, validación de credenciales y test de conexión física.
    *   `WebhookService`: Emisión de eventos firmados con HMAC-SHA256 a suscriptores externos (`docs.abdia.es`, `templates.abdia.es`) con reintento exponencial.
    *   `LegalHoldService`: Aplicación y liberación de bloqueos legales con soporte para múltiples holds activos simultáneos.
    *   `SpaceLinkService`: Vinculación y desvinculación de activos a espacios lógicos.
    *   `IntegrationLogsService`: Replicación asíncrona de eventos de auditoría hacia `ABDLogs`.

*   **Storage Providers**:
    *   `CloudinaryProvider`: Implementación real con `cloudinary` SDK (subida, URLs firmadas, borrado). Fallback a mock si no hay credenciales.
    *   `S3CompatibleProvider`: Cliente AWS S3 con soporte para MinIO local y Cloudflare R2. `forcePathStyle` para compatibilidad.
    *   `GoogleDriveProvider`: Service Account mediante `googleapis` SDK. Soporta `uploadFile`, `getSignedUrl`, `deleteFile` reales.
    *   `OneDriveProvider`: Client Credentials con `@azure/msal-node` y Microsoft Graph API v1.0. Soporte para `driveId` compartido.

*   **API REST v1**: Rutas `/api/v1/documents` (CRUD + versiones + eventos + holds), `/api/v1/connectors` (CRUD + test de conexión).

*   **Resultado**: 8 tests unitarios creados y verificados. Certificación `SYSTEM CERTIFIED`.

---

## [2026-06-21] — Fase 2: Borrado Lógico, Ciclos de Retención y Purga

*   **Ciclo de Vida del Documento**: Implementadas transiciones de estado:
    *   `active` → `deleted_pending_retention` (borrado lógico con programación de purga diferida).
    *   `deleted_pending_retention` → `purged` (purga física ejecutada por worker CRON).
*   **Reglas de Retención por Clase**:
    *   `temporary`: 7 días.
    *   `default`/`standard`: 30 días.
    *   `draft`: 1 día.
    *   `legal`: 365 días.
*   **Worker CRON**: Endpoint `/api/cron/data-lifecycle` protegido por token, que ejecuta `purgeExpiredDocuments` con límite de 5 reintentos por trabajo fallido.
*   **Protección Legal**: Los documentos bajo `legalHold` no pueden ser purgados ni borrados lógicamente.

*   **Resultado**: +3 tests de ciclo de vida (11 total). Certificación `SYSTEM CERTIFIED`.

---

## [2026-06-21] — Fase 3: RBAC, Espacios Jerárquicos y UI Industrial

*   **RBAC Documental**: Matriz de 4 roles:
    *   `FILE_VIEWER`: `view`, `list`
    *   `FILE_EDITOR`: `view`, `list`, `upload`, `update_metadata`
    *   `FILE_ADMIN`: todo excepto `audit` (heredado)
    *   `FILE_AUDITOR`: `view`, `list`, `audit`
*   **Middleware ABAC**: Integración con `@ajabadia/satellite-sdk` para evaluación de acceso mediante el motor `GuardianEngine`.
*   **UI Dashboard**: Panel de administración con:
    *   `DashboardClient`: Gestor de 4 pestañas.
    *   `UploadZone`: Zona de arrastre para subida de documentos.
    *   `DocumentDetailClient`: Vista forense detallada de documento con metadatos, versiones y eventos.
    *   `DashboardSkeleton`: Esqueleto de carga monospace.

*   **Resultado**: UI funcional y panel de control con datos mock para demostración. Certificación `SYSTEM CERTIFIED`.

---

## [2026-06-21] — Fase 4: Webhooks, Idempotencia, Concurrencia y Logs

*   **Webhooks Firmados**: Emisión de eventos `DOCUMENT_CREATED`, `DOCUMENT_VERSION_CREATED`, `DOCUMENT_PURGED` con firma HMAC-SHA256 y reintento exponencial (3 intentos con backoff de 2s, 4s, 8s).
*   **Idempotencia**: Control de duplicados en subidas mediante `Idempotency-Key` con caché en MongoDB y tiempo de expiración.
*   **Control de Concurrencia**: Versión optimista (`version` counter) en `createNewVersion` que lanza `VersionConflictError` si el documento fue modificado concurrentemente.
*   **Logs Forenses**: Replicación en tiempo real de eventos transaccionales hacia `ABDLogs` mediante `IntegrationLogsService` con timeout de 3s para no bloquear flujos documentales.

*   **Resultado**: +3 tests de idempotencia y webhooks (14 total). Certificación `SYSTEM CERTIFIED`.

---

## [2026-06-21] — Fase 4.5: Aislamiento de Deduplicación (Hito 9.10)

*   **Verificación de Aislamiento Intra-Tenant**: Implementados 2 tests unitarios en `deduplication.test.ts`:
    *   **Test 1**: Confirma que al subir un archivo con hash existente dentro del mismo tenant, el sistema reutiliza el `storageRef` sin subida física redundante.
    *   **Test 2**: Confirma que el mismo hash en un tenant diferente **no** provoca deduplicación cruzada: se realiza una subida física independiente.

*   **Resultado**: 35 tests pasando. Certificación `SYSTEM CERTIFIED - ERA 11 COMPLIANT`.

---

## [2026-06-23] — Sesión 34: Certificación Global ERA 11

*   **Auditoría Global Monorepo**: Ejecutado pipeline `full-audit` de 6 fases en los 7 satélites del ecosistema.
*   **proxy.ts Restaurado**: Verificado que `src/proxy.ts` (middleware de Next.js 16) existe en ABDFiles con `export default proxy`, coherente con el patrón estandarizado del monorepo.
*   **Resultado**: ABDFiles re-certificado sin regresiones. 7/7 satélites certificados ERA 11.

---

## Estado Actual

| Métrica | Valor |
|---------|-------|
| Tests | 35/35 pasando |
| Certificación | `SYSTEM CERTIFIED - ERA 11 COMPLIANT` |
| Proveedores de Storage | 4 (Cloudinary, S3, Google Drive, OneDrive) |
| Modelos de Datos | 8 (Document, DocumentVersion, DocumentEvent, AssetSpaceLink, StorageConnector, DeletionJob, LegalHold, IdempotencyKey) |
| Servicios | 7 (DocumentService, StorageService, ConnectorService, WebhookService, LegalHoldService, SpaceLinkService, IntegrationLogsService) |
| APIs REST | CRUD documentos, versiones, eventos, holds, conectores + CRON data-lifecycle |
| Roles RBAC | FILE_VIEWER, FILE_EDITOR, FILE_ADMIN, FILE_AUDITOR |
