# ABD Files (Suite de Herramientas)

Este repositorio contiene una suite de herramientas desarrolladas en .NET para el procesamiento de archivos, generación de comunicados y utilidades criptográficas.

## Proyectos Principales

### 1. Generador de Cartas (`src/GeneradorCartas`)
Herramienta para la generación masiva de documentos DOCX y PDF a partir de archivos de datos (CSV/Excel) y plantillas de Word.
- Soporta mapeo dinámico de variables.
- Generación de paquetes GAWEB para distribución.
- Conversión a PDF de alta fidelidad.

### 2. ABDTools.Core (`src/ABDTools.Core`)
Librería de lógica compartida que incluye:
- Modelos de datos para GAWEB.
- Utilidades de Logging.
- Funciones Criptográficas.
- Gestión de Configuración.

### 3. CryptoTool (`src/CryptoTool`)
Utilidad para el cifrado y descifrado de archivos y cadenas de texto.

### 4. ETL Tools (`src/EtlConfig`, `src/EtlConverter`)
Herramientas para la configuración y ejecución de procesos de extracción, transformación y carga de datos.

## Requisitos de Desarrollo
- .NET 10.0 SDK
- Windows Forms (requiere entorno Windows)
- (Opcional) Microsoft Word para versiones antiguas del generador (en proceso de sustitución por Syncfusion).

---
© 2025 ABD Tools
