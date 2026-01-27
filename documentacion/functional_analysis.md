# Análisis Funcional de Aplicaciones Go

Este documento describe detalladamente las funcionalidades de las aplicaciones existentes en Go encontradas en el directorio `cmd`. El objetivo es documentar el comportamiento actual para su posterior migración a C#.

**NOTA IMPORTANTE**: De la aplicación GeneradorCartas, solo se requiere migrar la funcionalidad de **GAWEB/Comunicados** (generación de PDFs con índices GAWEB para impresión industrial).

## 1. Encriptador (Encriptador ABDFN)

Herramienta de utilidad para la encriptación y desencriptación de archivos masiva utilizando AES-256.

### Funcionalidades
*   **Gestión de Archivos**:
    *   Permite agregar múltiples archivos a una lista de procesamiento.
    *   Evita duplicados en la lista.
    *   Permite eliminar archivos individuales o limpiar la lista completa.
    *   Ordenación de la lista (ascendente/descendente por nombre).
*   **Encriptación**:
    *   Algoritmo: AES-256.
    *   Detecta automáticamente si un archivo ya tiene extensión `.enc` para saltarlo.
    *   Genera archivos de salida con extensión `.enc`.
*   **Desencriptación**:
    *   Detecta si el archivo no tiene extensión `.enc` para saltarlo.
    *   Elimina la extensión `.enc` en el archivo resultante.
    *   Maneja conflictos de nombre agregando el sufijo `_decrypted`.
    *   Gestión de errores específica para "password incorrecta" (mapeo de "message authentication failed").
*   **Configuración de Procesamiento**:
    *   **Contraseña**: Campo con opción de visualizar/ocultar caracteres.
    *   **Directorio de Salida**: Opcional. Si no se especifica, usa el directorio del archivo original.
    *   **Modo Batch**: Checkbox "Mantener clave y lista" para realizar múltiples operaciones sin limpiar la interfaz tras finalizar.
*   **Interfaz y Feedback**:
    *   Área de Log visible con resultados operación por operación ([OK], [X], [SKIP]).
    *   Resumen final con conteo de éxitos y errores.
*   **Extras**:
    *   Acceso a Manual de Usuario (PDF) desde el menú.

---

## 2. Generador GAWEB (Comunicados para Impresión Industrial)

**ALCANCE**: Solo la funcionalidad de generación de lotes GAWEB para proveedor de impresión.

Aplicación para la generación masiva de correspondencia en formato PDF con índices GAWEB para impresión externalizada.

### Pestaña 1: Configurador GAWEB
*   **Gestión de Presets**:
    *   CRUD (Crear, Leer, Actualizar, Borrar) y Clonar configuraciones técnicas.
    *   Persistencia en archivos JSON.
*   **Parámetros Técnicos**:
    *   **Identificación**: Nombre, Descripción, Activo.
    *   **Soporte**: Selección entre Overlay (plantilla física) o PDF (impresión digital).
    *   **Formato de Sobre**: Filtrado dinámico según soporte (ej. C5, Americano).
    *   **Fechas**: Validación estricta de formato AAAAMMDD para Fecha Generación y Fecha Carta.
    *   **HOST**: Configuración de códigos de entorno (metadata de lote) y códigos de documento (metadata de registro).
    *   **Mapeo Dinámico**: Configuración de qué columnas del Excel/CSV se mapean a qué campos del registro GAWEB.
*   **Validación de Formulario**:
    *   Feedback visual (bordes rojos) para campos obligatorios o formatos incorrectos.

### Pestaña 2: Generador de Lotes GAWEB
*   **Fuentes de Datos**:
    *   Importación de datos desde **CSV** y **Excel (.xlsx)**.
    *   Detección automática de cabeceras.
*   **Plantillas**:
    *   Carga de plantillas **Word (.docx)**.
    *   Análisis automático de variables dentro de la plantilla (formato `{Variable}`).
*   **Mapeo de Campos**:
    *   Interfaz para asociar columnas del CSV a variables del DOCX.
    *   Intento de emparejamiento automático por nombre.
*   **Generación de Lotes**:
    *   **Proceso**:
        1. Generar DOCX por cada registro (reemplazo de variables).
        2. Convertir DOCX a PDF (requiere Microsoft Word).
        3. Generar fichero índice GAWEB (251 bytes por línea).
        4. Empaquetar PDFs en ZIP (compatible Windows).
        5. Generar archivo MD5 del ZIP.
    *   **Nomenclatura**:
        *   Paquete: `COMUNICADOS.PDF.{CodigoEntorno}.{Timestamp}.{Lote}.ZIP`
        *   Índice: `COMUNICADOS.PDF.{CodigoEntorno}.{Timestamp}.{Lote}.GAWEB`
        *   MD5: `COMUNICADOS.PDF.{CodigoEntorno}.{Timestamp}.{Lote}.MD5`
        *   PDFs: `{MD5Base32chars}{Secuencial8digits}` (40 chars total, sin extensión en índice).
    *   **Configuración**:
        *   Selección de Preset GAWEB (obligatorio).
        *   Generación parcial (Desde registro X hasta registro Y).
        *   Lote auto-generado (formato HHMM) o manual.
*   **Ejecución**:
    *   Barra de progreso con estimación de tamaño generado.
    *   Opción de Cancelar proceso en curso.
    *   Reporte de auditoría CSV con hash SHA256 de cada archivo.

### Pestaña 3: Verificador GAWEB
*   **Validación de Ficheros**:
    *   Carga de ficheros de índices `.GAWEB` o `.txt`.
    *   Validación lógica línea por línea según especificaciones (longitud fija 251 bytes, tipos de datos).
*   **Visualización**:
    *   Tabla paginada con estado de cada línea (OK/Error).
    *   Detalle de errores específicos (Campo esperado vs obtenido, posición exacta).
*   **Reportes**:
    *   Exportación de informe de validación a CSV.

---

## 3. Procesador ETL (ProcesadorETL/ProcesadorFATCA)

Motor de procesamiento de archivos de texto planos (Legacy/Mainframe) para su transformación y limpieza.

### Pestaña 1: Procesar Archivos
*   **Entrada**:
    *   Archivos de texto planos (sin delimitadores estándar o ancho fijo complejo).
    *   Detección y conversión de codificación (Latin1/Windows-1252 a UTF-8).
*   **Configuración (Presets)**:
    *   Selección de reglas de transformación definidas en JSON (Presets).
    *   Definición de tipos de registro y posiciones de campos.
*   **Lógica de Procesamiento**:
    *   **Detección de Tipos**: Algoritmo para identificar qué tipo de registro es cada línea (por longitud, identificador en posición fija, o fallback).
    *   **Validación Estructural**: Verificación opcional de cabeceras de archivo específicas.
    *   **Filtrado**: Por rango de líneas (Inicio/Fin).
*   **Salida**:
    *   **Formatos**: CSV (delimitado por `;`) o JSON.
    *   **Rotación**: División automática de archivos de salida cada N registros (Chunk Size configurables).
    *   **Segregación**: Genera un archivo de salida distinto por cada tipo de registro detectado en el archivo de entrada.
*   **Rendimiento**:
    *   Lectura mediante Buffer para archivos grandes (> 5GB).
    *   Barra de progreso basada en estimación de líneas.

### Pestaña 2: Editor de Configuración
*   *(Gestión visual de los JSON de reglas ETL: Definir Posición Inicio, Longitud, Nombre de campo, etc.)*
