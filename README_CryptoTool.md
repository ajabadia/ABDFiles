# CryptoTool

Herramienta de encriptación/desencriptación de archivos usando AES-256-GCM.

## Estructura del Proyecto

```
CryptoTool.sln
├── src/
│   ├── ABDTools.Core/          # Librería compartida
│   │   ├── Crypto/
│   │   │   └── CryptoService.cs
│   │   ├── Common/
│   │   │   └── FileUtils.cs
│   │   └── Configuration/
│   │       └── ConfigManager.cs
│   └── CryptoTool/             # Aplicación WPF
│       ├── Models/
│       │   ├── AppConfig.cs
│       │   └── FileItem.cs
│       ├── ViewModels/
│       │   └── MainViewModel.cs
│       ├── Views/
│       │   └── MainWindow.xaml
│       ├── Converters/
│       │   └── ValueConverters.cs
│       └── App.xaml
└── dist/                       # Salida de compilación
    └── CryptoTool/
        └── CryptoTool.exe
```

## Compilar

```powershell
# Desde la raíz del proyecto
.\build-cryptotool.ps1
```

Genera un .exe standalone en `dist/CryptoTool/CryptoTool.exe` (~50MB).

## Características

- ✅ Encriptación AES-256-GCM
- ✅ Derivación de clave PBKDF2 (100,000 iteraciones)
- ✅ Interfaz Material Design (tema oscuro)
- ✅ Drag & Drop de archivos
- ✅ Modo Batch
- ✅ Configuración persistente
- ✅ Log de operaciones

## Tecnologías

- .NET 8
- WPF
- Material Design In XAML
- CommunityToolkit.Mvvm

## Configuración

La configuración se guarda en:
```
%APPDATA%\ABDTools\CryptoTool\config.json
```
