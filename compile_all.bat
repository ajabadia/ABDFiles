@echo off
setlocal

set OUTDIR=dist_final
set BINDIR=%OUTDIR%\bin

echo Cleaning output directory...
if exist "%OUTDIR%" rmdir /s /q "%OUTDIR%"
mkdir "%BINDIR%"

REM ---------------------------------------------------------
REM 1. GeneradorCartas (GUI)
REM ---------------------------------------------------------
echo.
echo [1/5] Building GeneradorCartas...
dotnet restore src\GeneradorCartas\GeneradorCartas.csproj
if %errorlevel% neq 0 goto ERROR
dotnet publish src\GeneradorCartas\GeneradorCartas.csproj -c Release -o "%BINDIR%\GeneradorCartas" -v minimal /p:AppendTargetFrameworkToOutputPath=false --self-contained false
if %errorlevel% neq 0 goto ERROR

echo Creating Launcher...
(
    echo @echo off
    echo start "" "bin\GeneradorCartas\GeneradorCartas.exe" %%*
) > "%OUTDIR%\GeneradorCartas.bat"

echo Copying Presets...
xcopy /s /i /y presets "%BINDIR%\GeneradorCartas\presets" >nul

REM ---------------------------------------------------------
REM 2. EtlConfig (GUI)
REM ---------------------------------------------------------
echo.
echo [2/5] Building EtlConfig...
dotnet restore src\EtlConfig\EtlConfig.csproj
if %errorlevel% neq 0 goto ERROR
dotnet publish src\EtlConfig\EtlConfig.csproj -c Release -o "%BINDIR%\EtlConfig" -v minimal /p:AppendTargetFrameworkToOutputPath=false --self-contained false
if %errorlevel% neq 0 goto ERROR

echo Creating Launcher...
(
    echo @echo off
    echo start "" "bin\EtlConfig\EtlConfig.exe" %%*
) > "%OUTDIR%\EtlConfig.bat"

REM ---------------------------------------------------------
REM 3. LetterConfig (GUI)
REM ---------------------------------------------------------
echo.
echo [3/5] Building LetterConfig...
dotnet restore src\LetterConfig\LetterConfig.csproj
if %errorlevel% neq 0 goto ERROR
dotnet publish src\LetterConfig\LetterConfig.csproj -c Release -o "%BINDIR%\LetterConfig" -v minimal /p:AppendTargetFrameworkToOutputPath=false --self-contained false
if %errorlevel% neq 0 goto ERROR

echo Creating Launcher...
(
    echo @echo off
    echo start "" "bin\LetterConfig\LetterConfig.exe" %%*
) > "%OUTDIR%\LetterConfig.bat"

REM ---------------------------------------------------------
REM 4. CryptoTool (GUI)
REM ---------------------------------------------------------
echo.
echo [4/5] Building CryptoTool...
dotnet restore src\CryptoTool\CryptoTool.csproj
if %errorlevel% neq 0 goto ERROR
dotnet publish src\CryptoTool\CryptoTool.csproj -c Release -o "%BINDIR%\CryptoTool" -v minimal /p:AppendTargetFrameworkToOutputPath=false --self-contained false
if %errorlevel% neq 0 goto ERROR

echo Creating Launcher...
(
    echo @echo off
    echo start "" "bin\CryptoTool\CryptoTool.exe" %%*
) > "%OUTDIR%\CryptoTool.bat"

REM ---------------------------------------------------------
REM 5. EtlConverter (Console)
REM ---------------------------------------------------------
echo.
echo [5/5] Building EtlConverter...
dotnet restore src\EtlConverter\EtlConverter.csproj
if %errorlevel% neq 0 goto ERROR
dotnet publish src\EtlConverter\EtlConverter.csproj -c Release -o "%BINDIR%\EtlConverter" -v minimal /p:AppendTargetFrameworkToOutputPath=false --self-contained false
if %errorlevel% neq 0 goto ERROR

echo Creating Launcher...
(
    echo @echo off
    echo "bin\EtlConverter\EtlConverter.exe" %%*
) > "%OUTDIR%\EtlConverter.bat"

REM ---------------------------------------------------------
REM 6. GawebVerifier (GUI)
REM ---------------------------------------------------------
echo.
echo [6/6] Building GawebVerifier...
dotnet restore src\GawebVerifier\GawebVerifier.csproj
if %errorlevel% neq 0 goto ERROR
dotnet publish src\GawebVerifier\GawebVerifier.csproj -c Release -o "%BINDIR%\GawebVerifier" -v minimal /p:AppendTargetFrameworkToOutputPath=false --self-contained false
if %errorlevel% neq 0 goto ERROR

echo Creating Launcher...
(
    echo @echo off
    echo start "" "bin\GawebVerifier\GawebVerifier.exe" %%*
) > "%OUTDIR%\GawebVerifier.bat"


echo.
echo ========================================================
echo BUILD SUCCESSFUL!
echo Output directory: %OUTDIR%
echo Launchers available for all tools.
echo ========================================================
exit /b 0

:ERROR
echo.
echo ========================================================
echo BUILD FAILED!
echo ========================================================
exit /b 1
