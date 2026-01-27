@echo off
REM CryptoTool Build Script
REM Compila una versión ligera (WinForms, Framework Dependent)

echo ========================================
echo CryptoTool Build Script (WinForms)
echo ========================================

REM 1. Inicializar VS
if defined VSCMD_VER goto :EnvReady
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat" (
    call "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat" -arch=x64 -host_arch=x64
    goto :EnvReady
)
REM (Add other paths if needed, but VS 2022 Community is standard)
:EnvReady

REM 2. Preparar Icono
echo.
echo 2. Preparando recursos...
copy /Y "assets\images\ICON03.ico" "src\CryptoTool\AppIcon.ico" >nul

REM 3. Limpiar
echo.
echo 3. Limpiando...
if exist dist\CryptoTool rmdir /s /q dist\CryptoTool

REM 4. Compilar y Publicar
echo.
echo 4. Publicando...
dotnet publish src/CryptoTool/CryptoTool.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained false ^
    -o dist/CryptoTool

if %errorlevel% neq 0 (
    echo Error: Publicacion fallida
    pause
    exit /b 1
)

REM 5. Generar MSI
echo.
echo 5. Generando MSI...
copy /Y "assets\images\ICON03.ico" "src\installer\AppIcon.ico" >nul

pushd src\installer
wix build Package.wxs -o ..\..\dist\CryptoTool\CryptoTool.msi
if %errorlevel% neq 0 (
    echo Error: Generacion MSI fallida
    popd
    pause
    exit /b 1
)
popd

echo.
echo ========================================
echo BUILD COMPLETADO
echo ========================================
echo.
echo Ejecutable: dist\CryptoTool\CryptoTool.exe
echo Instalador: dist\CryptoTool\CryptoTool.msi
pause
