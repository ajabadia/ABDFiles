# Notas de Desarrollo: CryptoTool

## 1. Resumen de Implementación
Se ha completado el desarrollo de la primera herramienta modular `CryptoTool`.
- **Estado**: Finalizado (v1.0.0)
- **Tecnología**: C# / .NET 8 / WinForms
- **Entregables**: Ejecutable ligero (.exe) e Instalador (.msi)

## 2. Decisiones Arquitectónicas Críticas

### Cambio de WPF a WinForms
Inicialmente se implementó en **WPF** con Material Design.
- **Problema**: El ejecutable generado (Self-Contained) pesaba **~150 MB** y presentaba problemas de arranque en algunos entornos debido a dependencias de DirectX/Rendering.
- **Solución**: Se migró a **WinForms**.
- **Resultado**: 
  - Ejecutable base: **~300 KB** (Framework Dependent).
  - Instalador MSI completo: **~900 KB**.
  - Arranque inmediato y compatibilidad nativa robusta.

### Librería Core Compartida (`ABDTools.Core`)
Se mantuvo la arquitectura modular. La lógica de encriptación reside en `ABDTools.Core`, desacoplada de la UI.
- Esto permitirá reutilizar `CryptoService` (AES-256-GCM) en las futuras herramientas (`EtlConverter`, etc.) sin duplicar código.

## 3. Proceso de Compilación (Build)

### Entorno y Dependencias
1.  **NuGet**: Se requirió crear `nuget.config` para habilitar fuentes online (`api.nuget.org`), ya que la configuración local solo apuntaba a fuentes offline.
2.  **Visual Studio**: El script de build detecta automáticamente la instalación de VS 2022 (Community/Pro/Ent) y versiones Preview (VS 18).
3.  **.NET SDK**: Se utiliza el SDK instalado (detectado .NET 10 en el entorno, pero targeteando .NET 8 para compatibilidad estándar).

### Optimización del Ejecutable
Se optó por **Framework Dependent Deployment**:
- `SelfContained=false`: Utiliza el runtime de .NET instalado en la máquina del usuario.
- **Ventaja**: Reducción drástica de tamaño (de 150MB a <1MB).
- **Requisito**: El usuario debe tener .NET Desktop Runtime 8 (o compatible) instalado.

### Generación del Instalador (MSI)
Se integró **WiX Toolset v4**.
- **Desafío**: WiX es estricto con las rutas de archivos.
- **Solución**: Se copiaron los recursos (Icono) a la carpeta de compilación del instalador y se usaron referencias locales en `Package.wxs` o rutas absolutas cuando fue necesario.
- **Resultado**: Un archivo `.msi` standard que instala en `Program Files` y crea accesos directos.

## 4. Instrucciones de Build

Para regenerar todo el proyecto (Limpio -> Build -> Publish -> MSI), ejecutar:

```powershell
.\build-cryptotool.bat
```

Este script automatiza:
1.  Limpieza de carpetas `dist/`.
2.  Restauración de paquetes NuGet.
3.  Copia de recursos (iconos).
4.  Publicación del EXE.
5.  Compilación del MSI con WiX.

## 5. Próximos Pasos (Siguientes Herramientas)
Aplicar este mismo patrón (WinForms + Core + MSI) para:
- **LetterConfig** (Configuración GAWEB)
- **EtlConfig** (Configuración ETL)
- **EtlConverter** (Motor de conversión)
