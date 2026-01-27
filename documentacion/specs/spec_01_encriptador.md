# Especificación Técnica - Encriptador

## 1. Identidad de la Aplicación
*   **ID de Aplicación**: `com.ABDFN.encriptador`
*   **Título de Ventana**: "Encriptador ABDFN (AES-256) - Multi Archivo"
*   **Dimensiones Iniciales**: 850x700 (Centrado en pantalla)
*   **Icono**: `assets/images/ICON03.ico`

## 2. Interfaz de Usuario (UI)
La interfaz se divide en 3 zonas verticales (Split principal 45/55):

### Zona 1: Lista de Archivos (Superior)
*   **Componentes**:
    *   Botón "Agregar": Abre diálogo de selección de archivo.
        *   Validación: Comprueba si el path ya existe en `selectedFiles`. Si existe, muestra alerta "Duplicado".
    *   Botón "Limpiar": Vacía la lista `selectedFiles`.
    *   Botones Ordenar: Ascendente y Descendente (alfabético por path completo).
    *   Lista Visual: Scroll con filas enumeradas.
        *   Calcula nombre a mostrar: Si termina en `.enc`, añade prefijo "🔒 ".
        *   Botón "Eliminar" (icono papelera) por cada fila.
*   **Lógica de Estado**:
    *   `selectedFiles`: Slice de strings (paths absolutos).

### Zona 2: Opciones de Configuración (Centro)
*   **Directorio de Salida**: `Entry` + Botón `FolderOpen`.
    *   Placeholder: "Carpeta Salida (Opcional)".
    *   Comportamiento: Si está vacío, la salida es relativa al archivo origen.
*   **Contraseña**: `PasswordEntry` + Checkbox "Ver".
    *   Validación: No permite ejecución si está vacía.
*   **Modo Batch**: Checkbox "Mantener clave y lista (Batch)".
    *   Default: Desmarcado.
    *   Efecto: Si está desmarcado, al finalizar con éxito (0 errores) se limpia la contraseña y la lista de archivos.

### Zona 3: Acciones y Log (Inferior)
*   **Botones**:
    *   "ENCRIPTAR" (Icono Confirm).
    *   "DESENCRIPTAR" (Icono Login).
*   **Log**: `MultiLineEntry` (min rows 4).
    *   Función `appendLog`: Añade texto al final + salto de línea.

## 3. Especificación Criptográfica Detallada
Implementado actualmente en `pkg/crypto/crypto.go`.

### Constantes
*   **Salt Size**: 16 bytes (128-bit)
*   **Nonce Size**: 12 bytes (96-bit, estándar AES-GCM)
*   **Key Size**: 32 bytes (AES-256)
*   **Iterations PBKDF2**: 100,000

### Algoritmo
*   **Cifrado**: AES-256-GCM (Galois/Counter Mode).
*   **Derivación de Clave (KDF)**: PBKDF2
    *   Hash: HMAC-SHA256
    *   Iteraciones: 100,000
    *   Salt: Aleatorio 16 bytes
    *   Longitud Clave: 32 bytes

### Formato de Archivo (.enc)
El archivo binario resultante tiene la siguiente estructura secuencial:
1.  **Salt** (16 bytes): Usado para derivar la clave.
2.  **Nonce** (12 bytes): Vector de inicialización único para GCM.
3.  **Ciphertext + Tag**: El resto del archivo es el contenido cifrado incluyendo el Tag de autenticación de GCM (el tag va implícito al final en la implementación Go `Seal`).

### Lógica de Encriptación
1.  Leer archivo origen (`Create`).
2.  Generar **Salt** aleatorio (16 bytes).
3.  Derivar **Key** con `PBKDF2(pass, salt, 100000, 32, sha256)`.
4.  Generar **Nonce** aleatorio (12 bytes).
5.  Inicializar AES-GCM con la Key.
6.  Cifrar (`Seal`) el contenido.
7.  Escribir en destino: `[Salt] [Nonce] [Ciphertext]`.

### Lógica de Desencriptación
1.  Leer el archivo completo.
2.  Validar longitud mínima: Si `< 28 bytes` (16+12), error "archivo dañado".
3.  Extraer componentes:
    *   `Salt`: Bytes 0-15
    *   `Nonce`: Bytes 16-27
    *   `Ciphertext`: Bytes 28-Final
4.  Derivar **Key** con `PBKDF2(pass, salt, 100000, 32, sha256)`.
5.  Inicializar AES-GCM con la Key.
6.  Descifrar (`Open`) usando el Nonce.
    *   **Autenticación**: GCM valida automáticamente la integridad. Si falla, retorna error (interpretado como "Wrong Password").
7.  Escribir contenido descifrado a disco.

## 4. Lógica de Flujo (UI -> Crypto)
1.  **Validaciones Previas**:
    *   Pass vacío -> Error Dialog.
    *   Lista vacía -> Error Dialog.
    *   Crear carpeta salida (si se especificó) -> `os.MkdirAll(0755)`.

2.  **Iteración**:
    *   Contadores: `success`, `fail`, `skip`.
    *   Recorre `selectedFiles`.

3.  **Lógica Encriptación (`encrypt = true`)**:
    *   Check: Si archivo termina en `.enc` -> Log `[SKIP] ... ya es .enc`.
    *   Llamada: `crypto.EncryptFile(src, dest, pass)`

4.  **Lógica Desencriptación (`encrypt = false`)**:
    *   Check: Si archivo NO termina en `.enc` -> Log `[SKIP] ... no es .enc`.
    *   Protección Sobreescritura: Si nombre base es igual al original, añade `_decrypted`.
    *   Llamada: `crypto.DecryptFile(src, dest, pass)`

5.  **Finalización**:
    *   Log resumen.
    *   Si `failCount == 0` y `!Batch` -> Limpiar estado UI.
