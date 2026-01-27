$ErrorActionPreference = "Stop"

Write-Host "Building GeneradorCartas..."

# 1. Restore
dotnet restore src/GeneradorCartas/GeneradorCartas.csproj

# 2. Build
dotnet build src/GeneradorCartas/GeneradorCartas.csproj -c Release -o dist/GeneradorCartas

if ($LASTEXITCODE -eq 0) {
    Write-Host "GeneradorCartas built successfully in dist/GeneradorCartas"
}
else {
    Write-Host "Build Failed!" -ForegroundColor Red
    exit 1
}
