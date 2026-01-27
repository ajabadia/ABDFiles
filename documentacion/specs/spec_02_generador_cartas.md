# Especificación Técnica - Generador de Cartas y GAWEB

## 1. Identidad de la Aplicación
*   **ID de Aplicación**: `com.ABDFN.generador`
*   **Título de Ventana**: "Generador de Cartas v{Ver} - Correspondencia"
*   **Icono**: `assets/images/ICON02.ico`
*   **Tema**: Corporativo (`corporateTheme`).

## 2. Módulo Configuración GAWEB (Host)
Este módulo permite definir "presets" (archivos JSON) que contienen la configuración técnica necesaria para la generación de correspondencia industrial.

### Modelo de Datos (Preset)
Persistido en disco como JSON (`presets/gaweb/*.json`).

| Campo | Tipo | Validación UI | Descripción |
| :--- | :--- | :--- | :--- |
| `ID` | String | Auto (Timestamp) | Unique ID. |
| `Name` | String | - | Display Name. |
| `Description`| String | - | Optional Description. |
| `Active` | Bool | - | Visibility flag. |
| `TipoSoporte` | String | Enum("OV", "PDF") | Filtra la lista de formatos. |
| `FormatoCarta`| String | Requerido | Código interno. Depende de `lst_tamañoFormatoCarta.csv`. |
| `ForzarMetodo`| String | - | Método de envío. Depende de `lst_exportarMetodoEnvio.csv`. |
| `IndicadorDestino`| String | - | Depende de `lst_indicadorDestinoCom.csv`. |
| `TipoDestino` | String | - | Depende de `lst_destino.csv`. |
| `FechaGeneracion`| String (8) | AAAAMMDD | `validateDate()`. |
| `FechaCarta` | String (8) | AAAAMMDD | `validateDate()`. |
| `CodigoEntorno` | String | Max 8 chars | Metadata de Lote. |
| `CodigoDocumento`| String | 6 chars Exacto | Posición 27-32. Ej: `X00054`. |
| `Oficina` | String | 5 chars Númerico | Ej: `00152`. |
| `PaginasDefecto`| Int | 4 digits | Número de páginas. |
| `Idioma` | String | 2 chars (ISO) | Posición 86-87. Ej: `ES`. |
| `ViaReparto` | String | 2 chars | Opcional. |
| `CopiaPapel` | String | 1 char | S/N/X. |
| `Mapping` | Map[String]String | - | Mapeo dinámico: Campo GAWEB → Columna Excel. |

### Carga de Datos Maestros (Legacy)
**IMPORTANTE**: Los archivos CSV en `cartas/tablasFilemaker` son **referencias de valores válidos**, NO son la fuente de datos para la generación.
*   `lst_tamañoFormatoCarta.csv` -> Formatos
*   `lst_soporte.csv` -> Soportes
*   `lst_destino.csv` -> Destinos
*   `lst_indicadorDestinoCom.csv` -> Indicadores
*   `lst_exportarMetodoEnvio.csv` -> Métodos Envío
*   **Nota**: Implementa fallback hardcoded si los archivos no existen.

## 3. Estructura de Registro GAWEB (251 Bytes)
Definida en `pkg/gaweb/types.go`. El registro es de longitud fija, sin separadores.

| Campo | Posición | Longitud | Notas |
| :--- | :--- | :--- | :--- |
| Tipo Carta | 1-1 | 1 | " " o "O" |
| Formato | 2-3 | 2 | Requerido |
| Fecha Generación | 4-11 | 8 | YYYYMMDD |
| Lote | 12-15 | 4 | |
| Secuencial | 16-22 | 7 | Numérico |
| Página | 23-26 | 4 | Numérico |
| Cod Documento | 27-32 | 6 | Requerido |
| Versión | 33-36 | 4 | Default 0000 |
| Clase Contrato | 37-38 | 2 | Parte de Destino |
| Cod Contrato | 39-63 | 25 | Parte de Destino |
| TIREL | 64-64 | 1 | Parte de Destino |
| NUREL | 65-67 | 3 | Parte de Destino |
| CLALF | 68-82 | 15 | Parte de Destino |
| INDOM | 83-84 | 2 | Parte de Destino |
| Forzar Envío | 85-85 | 1 | " ", "S", "N" |
| Idioma | 86-87 | 2 | ISO 639-1 |
| Op Ahorro | 88-135 | 48 | Estructura compleja de subcampos (Code, Account, Sign, Amount, Cur, ISO, Conc), o relleno de espacios/ceros. |
| Fecha Carta | 136-143 | 8 | YYYYMMDD |
| Ind Destino | 144-144 | 1 | "0", "O", "7" |
| Detalle Carga | 145-148 | 4 | |
| Vía Reparto | 149-150 | 2 | |
| Copia Papel | 151-151 | 1 | |
| Oficina | 152-156 | 5 | |
| Mail/Fax | 157-206 | 50 | |
| Longitud Contenido | 207-211 | 5 | Numérico |
| Nombre PDF | 212-251 | 40 | Requerido |

### Reglas de Validación (`validator.go`)
*   **Longitud Total**: Estrictamente 251 bytes.
*   **Tipado**: Campos numéricos verificados con `strconv.Atoi` (permiten ceros a la izquierda).
*   **Fechas**: `time.Parse("20060102")`.
*   **Campos Obligatorios**: Formato, Lote, CodDocumento, NombrePDF, Destino completo.
*   **Enums**:
    *   `Tipo Carta`: solo " " o "O".
    *   `Indicador Destino`: solo "0", "O", "7".

## 4. Módulo Generador de Cartas - Flujo Completo

### 4.1 Entrada de Datos
*   **Fuente**: CSV (delimitador `;`) o Excel (.xlsx).
*   **Procesamiento**: `common.ReadDataFile` con limpieza de BOM UTF-8.
*   **Cabeceras**: Primera fila.

### 4.2 Plantilla DOCX
*   **Análisis**: `common.AnalizarPlantillaDocx`
    *   Abre el ZIP del .docx.
    *   Lee `word/document.xml`.
    *   Limpia XML tags con regex.
    *   Extrae variables con patrón `\{([^{}]+)\}`.
*   **Reemplazo**: Librería `github.com/lukasjarosch/go-docx`.

### 4.3 Modos de Generación

#### Modo Estándar (DOCX o PDF simple)
*   **Organización**: Bloques de N documentos (configurable, default 2000).
*   **Carpetas**: `Bloque_1`, `Bloque_2`, etc.
*   **Nomenclatura**: `Carta_{NumRegistro}_{ID}.docx` o `.pdf`.
*   **Conversión PDF**: `common.ConvertirDocxAPdf` (PowerShell + Word Interop).

#### Modo GAWEB (PDF + Índices)
Activado cuando:
*   Se selecciona formato "PDF + Índices GAWEB", O
*   Hay un `CodigoEntorno` definido en el preset.

**Flujo**:
1.  **Validación Preset**: `gaweb.ValidatePreset` antes de iniciar.
2.  **Generación MD5 Base**:
    *   Timestamp: `YYYYMMDDHHMMSS`.
    *   Hash MD5 del timestamp (32 chars).
    *   Se usa como prefijo para todos los PDFs del lote.
3.  **Nomenclatura PDF**:
    *   Formato: `{BaseMD5}{Secuencial8Digitos}` (40 chars total).
    *   Ejemplo: `a1b2c3d4e5f6...01234567890123456789000000001`.
4.  **Estructura de Carpetas**:
    ```
    Lote_{NumLote}/
      ├── TEMP_PDF_{timestamp}/  (temporal)
      │   ├── {MD5}{Seq}.pdf
      │   └── ...
      ├── COMUNICADOS.PDF.{CodigoEntorno}.{timestamp}.{Lote}.GAWEB
      ├── COMUNICADOS.PDF.{CodigoEntorno}.{timestamp}.{Lote}.ZIP
      └── COMUNICADOS.PDF.{CodigoEntorno}.{timestamp}.{Lote}.MD5
    ```
5.  **Generación DOCX → PDF**:
    *   Crear DOCX con nombre GAWEB en `TEMP_PDF_`.
    *   Convertir a PDF con `ConvertirDocxAPdf`.
    *   Eliminar DOCX tras conversión exitosa.
6.  **Fichero GAWEB**:
    *   Por cada PDF generado, escribir una línea de 251 bytes.
    *   Usar `gaweb.GawebRecord.Serialize()`.
    *   Campo `NombrePDF`: Los 40 caracteres del nombre (SIN extensión .pdf).
7.  **Empaquetado ZIP**:
    *   Función: `common.ZipDirectory(tempPdfDir, zipPath)`.
    *   Método: `zip.Deflate` (compatible Windows).
    *   Estructura: Archivos planos (sin carpetas internas).
8.  **Generación MD5**:
    *   Función: `common.CreateMD5File(zipPath, md5Path)`.
    *   Formato: Archivo de texto con hash MD5 hexadecimal del ZIP.
9.  **Limpieza**:
    *   Eliminar carpeta `TEMP_PDF_` tras ZIP exitoso.

### 4.4 Auditoría
*   **Archivo**: `Reporte_Auditoria_{timestamp}.csv`.
*   **Contenido**: Por cada archivo generado:
    *   Nombre archivo.
    *   Hash SHA256.
    *   Timestamp generación.
*   **Formato**: CSV con BOM, separador `;`.

### 4.5 Generación Parcial
*   **Parámetros**: `inputFromRecord`, `inputToRecord`.
*   **Comportamiento**: Procesa solo el rango especificado de registros.

### 4.6 Cancelación
*   **Mecanismo**: Canal `lettersCancelChan`.
*   **Post-cancelación**: Diálogo para eliminar archivos generados parcialmente.

## 5. Módulo Verificador GAWEB
*   **Raw Line**: Validaciones ejecutadas sobre la línea cruda leída del fichero.
*   **Feedback**: Devuelve lista de errores con posición exacta (Ej: "27-32") y mensaje descriptivo.
*   **Exportación**: Reporte CSV con detalle de errores.

## 6. Dependencias Críticas

### PowerShell + Word Interop (Conversión PDF)
```powershell
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$doc = $word.Documents.Open('{input}')
$doc.SaveAs([ref] '{output}', [ref] 17)  # wdFormatPDF = 17
$doc.Close()
$word.Quit()
```
**Requisito**: Microsoft Word instalado en el sistema.

### ZIP (Windows-Compatible)
*   Método: `zip.Deflate`.
*   Estructura: Flat (sin subdirectorios).
*   Nombres: Solo basenames de archivos.

### MD5
*   Algoritmo: `crypto/md5`.
*   Salida: Hexadecimal lowercase en archivo `.MD5`.
