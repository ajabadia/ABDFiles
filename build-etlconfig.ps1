# Build EtlConfig
# Generates executable in dist/EtlConfig

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "Building EtlConfig..." -ForegroundColor Cyan

# Clean previous build
if (Test-Path "dist/EtlConfig") {
    Remove-Item -Recurse -Force "dist/EtlConfig"
}

# Build and publish
# Note: Using --self-contained false as per project optimization settings (Framework Dependent)
dotnet publish src/EtlConfig/EtlConfig.csproj -c $Configuration -r win-x64 --self-contained false -p:PublishSingleFile=false -o dist/EtlConfig

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
    Write-Host "Output: dist/EtlConfig/EtlConfig.exe" -ForegroundColor Yellow
    
    # Show file size
    if (Test-Path "dist/EtlConfig/EtlConfig.exe") {
        $exe = Get-Item "dist/EtlConfig/EtlConfig.exe"
        $sizeMB = [math]::Round($exe.Length / 1MB, 2)
        Write-Host "Size: $sizeMB MB" -ForegroundColor Gray
    }
}
else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
