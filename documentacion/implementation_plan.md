# Plan de Implementación - Suite Modular de Herramientas

## Objetivo General
Migrar las aplicaciones Go existentes a C# como herramientas **independientes y profesionales**, mejorando la usabilidad y eliminando dependencias rígidas de rutas fijas.

---

## 1. Arquitectura Propuesta

### Suite de 7 Aplicaciones Independientes

#### Grupo A: Criptografía
1. **CryptoTool** - Encriptador/Desencriptador de archivos

#### Grupo B: GAWEB (Comunicados)
2. **LetterGenerator** - Generador de lotes GAWEB
3. **LetterConfig** - Configurador de presets GAWEB
4. **LetterVerifier** - Verificador de ficheros GAWEB

#### Grupo C: ETL (Procesador de archivos)
5. **EtlConverter** - Convertidor TXT → CSV/JSON
6. **EtlConfig** - Editor de configuraciones ETL

#### Grupo D: Utilidades (Opcional)
7. **PresetManager** - Gestor centralizado de presets (si se requiere)

---

## 2. Mejoras de Usabilidad Clave

### 2.1 Gestión Flexible de Presets
**Problema Actual**: Rutas hardcoded (`presets/gaweb/*.json`).

**Solución**:
- **Configuración de Carpeta Base**:
  - Menú: `Herramientas > Configuración > Carpeta de Presets`.
  - Guardar en `appsettings.json` o registro de Windows.
  - Default: `%APPDATA%\ABDTools\Presets\{AppName}`.
- **Carga Individual**:
  - Botón "Abrir Preset" con diálogo de archivo estándar.
  - Historial de "Presets Recientes" (últimos 5).
- **Importar/Exportar**:
  - Exportar preset actual a cualquier ubicación.
  - Importar preset desde archivo externo.

### 2.2 Configuradores Profesionales
**Problema Actual**: Formularios básicos sin validación en tiempo real.

**Solución**:
- **Validación en Tiempo Real**:
  - Feedback inmediato (iconos ✓/✗ junto a campos).
  - Tooltips explicativos en hover.
  - Desactivar "Guardar" hasta que formulario sea válido.
- **Preview/Vista Previa**:
  - `LetterConfig`: Vista previa del registro GAWEB de 251 bytes generado.
  - `EtlConfig`: Vista previa de parsing con datos de ejemplo.
- **Asistentes (Wizards)**:
  - Modo "Asistente" paso a paso para usuarios nuevos.
  - Modo "Avanzado" para usuarios expertos.
- **Plantillas**:
  - Presets predefinidos de fábrica (solo lectura).
  - Opción "Crear desde Plantilla".

### 2.3 Interfaz Moderna
**Stack Tecnológico**:
- **Framework**: WPF con Material Design o Fluent UI.
- **Patrón**: MVVM estricto.
- **Temas**: Claro/Oscuro con persistencia.

---

## 3. Especificación por Aplicación

### 3.1 CryptoTool
**Funcionalidad**: Igual que actual.
**Mejoras**:
- Drag & Drop de archivos.
- Historial de contraseñas (opcional, con advertencia de seguridad).
- Opción "Recordar carpeta de salida".

---

### 3.2 LetterGenerator (Generador GAWEB)
**Funcionalidad**:
- Generar lotes GAWEB (DOCX → PDF → ZIP → MD5).
- Requiere preset cargado.

**Mejoras**:
- **Selector de Preset**:
  - Dropdown con presets de carpeta configurada.
  - Botón "Abrir Preset Externo".
  - Botón "Editar Preset" (abre LetterConfig).
- **Validación Pre-Generación**:
  - Verificar que Word está instalado.
  - Verificar permisos de escritura.
  - Validar preset antes de iniciar.
- **Progreso Detallado**:
  - Barra con etapas: "Generando DOCX", "Convirtiendo PDF", "Empaquetando ZIP".
  - Estimación de tiempo restante.
- **Logs Exportables**:
  - Botón "Exportar Log" (TXT/CSV).

**Dependencias**:
- Preset GAWEB (JSON).
- Microsoft Word (validar en startup).

---

### 3.3 LetterConfig (Configurador GAWEB)
**Funcionalidad**:
- CRUD de presets GAWEB.

**Mejoras**:
- **Menú Archivo**:
  - Nuevo, Abrir, Guardar, Guardar Como, Cerrar.
  - Exportar/Importar.
- **Validación Visual**:
  - Campos obligatorios marcados con `*`.
  - Bordes rojos en campos inválidos.
  - Panel de "Errores" en la parte inferior.
- **Preview GAWEB**:
  - Panel lateral con vista del registro de 251 bytes.
  - Actualización en tiempo real al editar campos.
- **Mapeo Dinámico**:
  - Grid editable para mapear campos GAWEB ↔ Columnas Excel.
  - Botón "Auto-mapear" (por nombre).
  - Validación: advertir si falta mapeo obligatorio.
- **Tablas de Referencia**:
  - Cargar CSVs de `tablasFilemaker` si existen.
  - Fallback a valores hardcoded.
  - Indicador visual si CSV no encontrado.

**Independencia**:
- No requiere otras apps.
- Puede ejecutarse standalone.

---

### 3.4 LetterVerifier (Verificador GAWEB)
**Funcionalidad**:
- Validar ficheros `.GAWEB`.

**Mejoras**:
- **Drag & Drop** de fichero.
- **Filtros de Visualización**:
  - Mostrar solo errores.
  - Mostrar solo advertencias.
  - Mostrar todo.
- **Exportación**:
  - CSV, Excel, PDF (reporte profesional).
- **Estadísticas**:
  - Panel resumen: Total líneas, Válidas, Errores, Advertencias.
  - Gráfico de barras simple.

**Independencia**:
- No requiere preset.
- Validación hardcoded según especificación GAWEB.

---

### 3.5 EtlConverter (Convertidor ETL)
**Funcionalidad**:
- Convertir TXT → CSV/JSON usando preset.

**Mejoras**:
- **Selector de Preset**:
  - Similar a LetterGenerator.
  - Botón "Editar Preset" (abre EtlConfig).
- **Preview de Datos**:
  - Mostrar primeras 10 líneas parseadas en grid.
  - Permitir ajustar preset y re-previsualizar sin procesar todo.
- **Detección Automática**:
  - Sugerir encoding (UTF-8/Latin1).
  - Estimar número de líneas (para progreso).
- **Opciones de Salida**:
  - Checkbox: "Generar un archivo por tipo de registro".
  - Checkbox: "Incluir BOM UTF-8".

**Dependencias**:
- Preset ETL (JSON).

---

### 3.6 EtlConfig (Editor ETL)
**Funcionalidad**:
- CRUD de configuraciones ETL.

**Mejoras**:
- **Editor de Campos**:
  - Grid editable: Nombre, Inicio, Longitud.
  - Validación: Inicio ≥ 0, Longitud > 0.
  - Detección de solapamientos (advertencia).
- **Preview con Datos de Prueba**:
  - Cargar fichero TXT de ejemplo.
  - Mostrar parsing en tiempo real.
  - Resaltar campos en la línea raw (colores).
- **Tipos de Registro**:
  - Gestión de múltiples tipos.
  - Configuración de detección (posición, longitud, default).
- **Validación de Cabecera**:
  - Checkbox: "Validar cabecera obligatoria".
  - Selector de tipo de registro esperado.

**Independencia**:
- No requiere otras apps.

---

## 4. Arquitectura Técnica

### 4.1 Tecnologías
- **Lenguaje**: C# 12 (.NET 8).
- **UI**: WPF + Material Design In XAML o Fluent UI.
- **Patrón**: MVVM (CommunityToolkit.Mvvm).
- **Configuración**: `System.Text.Json` para presets, `appsettings.json` para app.
- **Logging**: Serilog (archivo + UI).
- **Testing**: xUnit + FluentAssertions.

### 4.2 Librerías Compartidas
**Proyecto**: `ABDTools.Core` (Class Library)
- `ABDTools.Core.Crypto`: Encriptación AES-GCM.
- `ABDTools.Core.Gaweb`: Modelos, validación, serialización GAWEB.
- `ABDTools.Core.Etl`: Parsing de ancho fijo, detección encoding.
- `ABDTools.Core.Common`: Utilidades (hash, ZIP, DOCX, PDF).

**Ventajas**:
- Reutilización de código.
- Testing unitario independiente.
- Versionado semántico.

### 4.3 Estructura de Solución
```
ABDTools.sln
├── src/
│   ├── ABDTools.Core/           (Shared Library)
│   ├── CryptoTool/              (WPF App)
│   ├── LetterGenerator/         (WPF App)
│   ├── LetterConfig/            (WPF App)
│   ├── LetterVerifier/          (WPF App)
│   ├── EtlConverter/            (WPF App)
│   └── EtlConfig/               (WPF App)
├── tests/
│   ├── ABDTools.Core.Tests/
│   ├── LetterGenerator.Tests/
│   └── EtlConverter.Tests/
└── docs/
    └── specs/                   (Documentación técnica)
```

---

## 5. Gestión de Configuración

### 5.1 Configuración de Usuario
**Ubicación**: `%APPDATA%\ABDTools\{AppName}\config.json`

**Contenido Ejemplo** (`LetterGenerator`):
```json
{
  "PresetDirectory": "C:\\Users\\User\\Documents\\GAWEB_Presets",
  "RecentPresets": [
    "C:\\Presets\\Preset1.json",
    "C:\\Presets\\Preset2.json"
  ],
  "LastOutputDirectory": "C:\\Output",
  "Theme": "Dark",
  "WordPath": "C:\\Program Files\\Microsoft Office\\..."
}
```

### 5.2 Presets
**Ubicación Default**: `%APPDATA%\ABDTools\Presets\{Type}\`
- `{Type}` = `GAWEB` o `ETL`.

**Flexibilidad**:
- Usuario puede cambiar carpeta base.
- Cada preset es un archivo JSON independiente.
- Nombre de archivo = Nombre del preset (sanitizado).

---

## 6. Conversión PDF (Crítico)

### Problema
Dependencia de Word Interop (COM).

### Soluciones Propuestas
1. **Mantener Word Interop** (Opción conservadora):
   - Validar instalación en startup.
   - Mensaje claro si falta Word.
   - Documentar requisito.

2. **Librería .NET Pura** (Opción moderna):
   - **Syncfusion DocIO** (comercial, trial disponible).
   - **Aspose.Words** (comercial).
   - **Open XML SDK + iTextSharp** (gratuito, más complejo).

3. **Híbrido**:
   - Detectar Word.
   - Si existe, usar Interop.
   - Si no, usar librería .NET (con advertencia de posibles diferencias).

**Recomendación**: Opción 1 inicialmente, evaluar Opción 3 si hay problemas.

---

## 7. Instalación y Distribución

### 7.1 Instalador
- **Tecnología**: WiX Toolset o Inno Setup.
- **Opciones**:
  - Instalar todas las apps.
  - Instalar apps individuales (componentes).
- **Shortcuts**: Menú Inicio + Escritorio (opcional).

### 7.2 Portable
- Versión ZIP con todas las apps.
- Configuración en carpeta local (no `%APPDATA%`).

### 7.3 Auto-Update (Futuro)
- Integración con Squirrel.Windows o ClickOnce.

---

## 8. Plan de Migración

### Fase 1: Infraestructura (2-3 días)
- [ ] Crear solución y proyectos.
- [ ] Configurar `ABDTools.Core`.
- [ ] Migrar lógica de `pkg/crypto` a `ABDTools.Core.Crypto`.
- [ ] Migrar lógica de `pkg/gaweb` a `ABDTools.Core.Gaweb`.
- [ ] Migrar lógica de `pkg/common` a `ABDTools.Core.Common`.
- [ ] Tests unitarios de Core.

### Fase 2: CryptoTool (1 día)
- [ ] UI WPF.
- [ ] Integración con `ABDTools.Core.Crypto`.
- [ ] Testing manual.

### Fase 3: LetterConfig (2 días)
- [ ] UI WPF con validación.
- [ ] Preview GAWEB.
- [ ] Gestión de presets flexible.

### Fase 4: LetterGenerator (3 días)
- [ ] UI WPF.
- [ ] Integración Word/PDF.
- [ ] Generación de lotes.
- [ ] Testing con datos reales.

### Fase 5: LetterVerifier (1 día)
- [ ] UI WPF.
- [ ] Validación GAWEB.
- [ ] Exportación reportes.

### Fase 6: EtlConfig (2 días)
- [ ] UI WPF.
- [ ] Editor de campos.
- [ ] Preview con datos.

### Fase 7: EtlConverter (2 días)
- [ ] UI WPF.
- [ ] Procesamiento ETL.
- [ ] Testing con archivos grandes.

### Fase 8: Integración y Pulido (2 días)
- [ ] Instalador.
- [ ] Documentación de usuario.
- [ ] Testing end-to-end.

**Total Estimado**: 15-17 días de desarrollo.

---

## 9. Riesgos y Mitigaciones

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|--------------|---------|------------|
| Word no disponible | Media | Alto | Validación temprana + mensaje claro |
| Rendimiento con archivos grandes | Baja | Medio | Async/await + streaming |
| Compatibilidad de presets Go→C# | Baja | Bajo | Validación de schema JSON |
| Curva de aprendizaje WPF | Media | Bajo | Usar templates + CommunityToolkit |

---

## 10. Próximos Pasos

1. **Revisar y Aprobar Plan**.
2. **Priorizar Apps** (¿cuál primero?).
3. **Definir Diseño UI** (mockups o wireframes).
4. **Iniciar Fase 1** (Infraestructura).
