# Especificaciones Técnicas - General

**Fecha**: 2025-12-16
**Origen de Datos**: Análisis estático de código fuente Go completo (`cmd/` y `pkg/`).
**Cobertura**: Especificaciones construidas utilizando la totalidad del código fuente disponible, incluyendo librerías compartidas de criptografía, lógica de validación GAWEB y utilidades comunes.

## Estructura de la Documentación
Esta documentación técnica se divide en los siguientes módulos:

1.  **[spec_01_encriptador.md](./spec_01_encriptador.md)**: Especificaciones de la herramienta de encriptación AES. Incluye detalles del algoritmo (AES-256-GCM), derivación de clave (PBKDF2) y estructura binaria del archivo.
2.  **[spec_02_generador_cartas.md](./spec_02_generador_cartas.md)**: Especificaciones del generador de correspondencia y gestor GAWEB. Incluye mapas de registro byte a byte (251 bytes), validaciones exactas y flujo de generación de documentos.
3.  **[spec_03_procesador_etl.md](./spec_03_procesador_etl.md)**: Especificaciones del motor ETL de archivos planos. Incluye lógica de detección de encoding, parsing de ancho fijo y rotación de archivos.

## Dependencias de Librerías (Analizadas)
*   `pkg/crypto`: Implementación de seguridad (AES-GCM, PBKDF2).
*   `pkg/gaweb`: Lógica de negocio GAWEB (Structures, Validation, Loading).
*   `pkg/common`: Utilidades de archivo, Encoding, Hashing, PDF Conversion (PowerShell).
*   `pkg/gui`: Componentes UI reutilizables.
