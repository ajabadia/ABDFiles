# Guía de Configuración de Presets

Esta carpeta almacena los perfiles de configuración (Presets) que utiliza el Procesador ABDFN/FND para interpretar los archivos de texto del Host.

## 🛠️ Cómo crear un nuevo Preset

Tienes dos opciones para crear una nueva configuración:

### Opción A: Usar el Editor Visual (Recomendado)
1. Abre la aplicación `GestorFinal.exe`.
2. Ve a la pestaña **"Editor de Configuración"**.
3. Define los campos visualmente.
4. Pulsa **"Guardar Preset"**. El archivo JSON se creará automáticamente en esta carpeta.

### Opción B: Edición Manual (Avanzado)
Puedes crear o editar archivos `.json` directamente en esta carpeta siguiendo esta estructura:

```json
{
  "nombre_mostrar": "ABDFN Mensual",
  "version": "1.0",
  "activo": true,
  "max_filas_por_csv": 900000,
  "codificacion": "utf-8",
  "tipos_registro": {
    "0": [
      {"nombre": "TIPO_REGISTRO", "inicio": 0, "longitud": 1},
      {"nombre": "CODIGO_CLIENTE", "inicio": 1, "longitud": 10}
    ],
    "1": [
      {"nombre": "TIPO_REGISTRO", "inicio": 0, "longitud": 1},
      {"nombre": "NOMBRE", "inicio": 1, "longitud": 50}
    ]
  }
}
```

## 📖 Diccionario de Campos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `nombre_mostrar` | String | Nombre que aparecerá en el desplegable de la aplicación. |
| `version` | String | Versión del formato (útil para control de cambios). |
| `activo` | Bool | `true` para mostrarlo, `false` para ocultarlo temporalmente. |
| `max_filas_por_csv` | Int | Número máximo de filas por archivo de salida (Excel soporta hasta 1M). |
| `codificacion` | String | Codificación del archivo de entrada (`utf-8`, `latin1`, `windows-1252`). |
| `tipos_registro` | Map | Define los diferentes tipos de línea que puede contener el archivo. |

### Definición de Posiciones
Para cada campo dentro de `tipos_registro`:
* **inicio:** Posición inicial del carácter (empezando por 0).
* **longitud:** Cantidad de caracteres a leer.

> **Importante:** Asegúrate de que no haya solapamientos entre campos. El Editor Visual detecta estos errores automáticamente.