# 📁 ABDFiles - Gestor Documental Multi-Tenant

[![ERA 11 Certified](https://img.shields.io/badge/ERA%2011-CERTIFIED-brightgreen?style=for-the-badge&logo=shield)](../.github/workflows/audit.yml)

Sistema centralizado de almacenamiento, versionado y ciclo de vida de documentos para todo el ecosistema **ABD**. Proporciona ingesta segura, deduplicación intra-tenant, múltiples proveedores de almacenamiento y gobernanza documental completa (retenciones, bloqueos legales y purgas programadas).

---

## 🚀 Arquitectura y Tecnologías

*   **Next.js 16.2.6 & React 19**: Server Components (RSC) y Server Actions para operaciones Documentales.
*   **Mongoose & Zod**: Modelos de datos con tipado estricto y validación en runtime.
*   **Multi-Provider Storage**: Cloudinary (primario), AWS S3, Google Drive (Service Account) y OneDrive (Microsoft Graph API) — configurables por Tenant.
*   **Deduplicación Intra-Tenant**: Hash SHA-256 para evitar almacenamiento redundante dentro del mismo inquilino.
*   **RBAC Documental**: Roles `FILE_VIEWER`, `FILE_EDITOR`, `FILE_ADMIN`, `FILE_AUDITOR`.
*   **Next-Intl**: Soporte multilingüe completo (Inglés / Español) mediante enrutamiento localizado con prefijos de idioma (`/[locale]`).

---

## 🛠️ Guía de Inicio Rápido

### Requisitos Previos
Configurar las variables de entorno en el archivo `.env.local`:
```env
NEXT_PUBLIC_APP_ID="files"
MONGODB_URI=mongodb+srv://...
DATABASE_URL=mongodb+srv://...
CLOUDINARY_URL=cloudinary://...
```

### Comandos de Desarrollo
Para arrancar el servidor local en el puerto oficial **`5005`**:
```powershell
# Levantar el entorno local
.\start.bat
```

Para validar tipos estáticos, compilación y empaquetado de producción:
```powershell
pnpm build
```

Para ejecutar la suite de tests unitarios:
```powershell
pnpm test
```

---

## 📁 Estructura del Proyecto (`src/`)

*   `src/app/[locale]/`: Enrutador Next.js localizado con páginas de administración y vista de documentos.
*   `src/services/`: Capa de lógica de negocio.
    *   `document-service.ts`: CRUD de documentos, versionado y deduplicación SHA-256.
    *   `storage-service.ts`: Abstracción sobre proveedores de almacenamiento (Cloudinary, S3, Google Drive, OneDrive).
    *   `connector-service.ts`: Gestión de conectores de almacenamiento por Tenant.
    *   `legal-hold-service.ts`: Bloqueos legales que detienen purgas.
    *   `webhook-service.ts`: Emisión de eventos firmados con HMAC.
    *   `space-link-service.ts`: Vinculación polimórfica de activos a espacios.
*   `src/models/`: Esquemas Mongoose (`Document`, `DocumentVersion`, `DocumentEvent`, `AssetSpaceLink`, `StorageConnector`, `DeletionJob`, `LegalHold`).
*   `src/lib/`: Utilidades compartidas (`rbac.ts`, `abac.ts`, `idempotency.ts`).
*   `src/actions/`: Server Actions de Next.js para operaciones del dashboard.

---

## 📜 Manifestos del Proyecto
*   **[progress.md](./progress.md)**: Registro cronológico de avances y sesiones de trabajo.
*   **[handoff.md](./handoff.md)**: Documentación de traspaso de contexto técnico entre sesiones.
*   **Roadmap Técnico**: Ver `ABD-Suite-DOCS/01_active_specs/ROADMAP.md` (Fase 9 de la Suite).

---

## ☁️ Despliegue en Producción (Vercel)

| Variable de Entorno | Valor en Local | Valor en Producción |
| :--- | :--- | :--- |
| **`NEXT_PUBLIC_APP_URL`** | `http://localhost:5005` | `https://files.abdia.es` |
| **`AUTH_URL`** | `http://localhost:5005` | `https://files.abdia.es` |
| **`NEXT_PUBLIC_APP_ID`** | `files` | `files` |

El proveedor de almacenamiento activo se configura por Tenant desde el panel de `ABDtenantGobernance`.
