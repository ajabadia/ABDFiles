# Build CryptoTool
# Generates standalone .exe in dist/CryptoTool

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "Building CryptoTool..." -ForegroundColor Cyan

# Clean previous build
if (Test-Path "dist/CryptoTool") {
    Remove-Item -Recurse -Force "dist/CryptoTool"
}

# Build and publish
dotnet publish src/CryptoTool/CryptoTool.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o dist/CryptoTool

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✓ Build successful!" -ForegroundColor Green
    Write-Host "Output: dist/CryptoTool/CryptoTool.exe" -ForegroundColor Yellow
    
    # Show file size
    $exe = Get-Item "dist/CryptoTool/CryptoTool.exe"
    $sizeMB = [math]::Round($exe.Length / 1MB, 2)
    Write-Host "Size: $sizeMB MB" -ForegroundColor Gray
} else {
    Write-Host "`n✗ Build failed!" -ForegroundColor Red
    exit 1
}
