# Build LetterConfig
# Generates executable in dist/LetterConfig

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "Building LetterConfig..." -ForegroundColor Cyan

# Clean previous build
if (Test-Path "dist/LetterConfig") {
    Remove-Item -Recurse -Force "dist/LetterConfig"
}

# Build and publish
# Note: Using --self-contained false as per project optimization settings (Framework Dependent)
dotnet publish src/LetterConfig/LetterConfig.csproj -c $Configuration -r win-x64 --self-contained false -p:PublishSingleFile=false -o dist/LetterConfig

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
    Write-Host "Output: dist/LetterConfig/LetterConfig.exe" -ForegroundColor Yellow
    
    # Show file size
    if (Test-Path "dist/LetterConfig/LetterConfig.exe") {
        $exe = Get-Item "dist/LetterConfig/LetterConfig.exe"
        $sizeMB = [math]::Round($exe.Length / 1MB, 2)
        Write-Host "Size: $sizeMB MB" -ForegroundColor Gray
    }
}
else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
