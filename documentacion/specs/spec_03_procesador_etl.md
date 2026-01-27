# Especificación Técnica - Procesador ETL

## 1. Identidad de la Aplicación
*   **ID de Aplicación**: N/A (Consola/Fyne)
*   **Título de Ventana**: "Procesador ETL v{Ver}"
*   **Objetivo**: Procesamiento y normalización de archivos de texto mainframe/legacy a formatos estructurados (CSV/JSON).

## 2. Motor de Procesamiento (Core)
El núcleo de la aplicación usa la configuración definida en `pkg/common/types.go` y la lógica base en `pkg/common/utils.go`.

### Modelo de Configuración (Preset)
Estructura `Config`:
*   `DisplayName`: String.
*   `Version`: String.
*   `IsActive`: Bool.
*   `Encoding`: String ("utf-8", "latin1").
*   `ChunkSize`: Int (Defecto usuario o preset).
*   `RecordTypeStart`: Int (Posición).
*   `RecordTypeLen`: Int (Longitud).
*   `DefaultRecordType`: String.
*   `HeaderTypeID`: String (Validación de primera línea).
*   `TiposRegistro`: Map[String][]Campo.
    *   `Campo`: `{Nombre, Inicio, Longitud}`.

### Lógica de Detección de Encoding
Implementado en `DetectarCodificacion`:
1.  Lee primeros 4KB.
2.  Si es válido UTF-8 (`utf8.Valid(buf)`), retorna "utf-8".
3.  Si no, retorna "latin1".
4.  Si se fuerza "latin1" o "windows-1252" en config, se usa `ToUTF8` (conversión byte a rune directa) para procesar cada línea.

### Lógica de Parseo (`ProcesarLinea`)
Extracción de campos de ancho fijo optimizada:
1.  Recibe línea raw y slice de `Campo`.
2.  Itera campos:
    *   Calcula `fin = Inicio + Longitud`.
    *   Si `Inicio > len(line)`, valor vacío.
    *   Si `fin > len(line)`, trunca hasta fin de línea.
    *   Extrae substring `line[Inicio:fin]`.
    *   Aplica `strings.TrimSpace`.

### Lógica de Salida
*   **Rotación**: Por `ChunkSize`.
*   **CSV**: `utils.go/ReadDataFile` muestra que se espera `;` como separador y soporte de BOM. La salida debe ser consistente.
*   **Zip**: Utilidad `ZipDirectory` para empaquetado final (si aplica).

## 3. Utilidades Comunes (`pkg/common`)
Funcionalidades compartidas críticas para otros módulos también.

*   **Manipulación DOCX (`AnalizarPlantillaDocx`)**:
    *   Abre el ZIP del `.docx`.
    *   Lee `word/document.xml`.
    *   Limpia etiquetas XML (`<[^>]*>`) mediante Regex.
    *   Busca patrón `\{([^{}]+)\}` para extraer variables.
    *   **Nota**: Método robusto ante formateo sucio de Word (XML tags dentro de las llaves).

*   **Conversión PDF (`ConvertirDocxAPdf`)**:
    *   **Dependencia Externa**: PowerShell + Word Interop.
    *   Script incrustado:
        ```powershell
        $word = New-Object -ComObject Word.Application
        $doc = $word.Documents.Open(...)
        $doc.SaveAs(..., 17) // wdFormatPDF = 17
        ```
    *   Requiere entorno Windows con Office instalado.

*   **Hashing**:
    *   CRC/Hash: MD5 (`CalculateStringHash`, `CreateMD5File`) y SHA256 (`CalculateFileHash`).

