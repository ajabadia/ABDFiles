$project = "src/GawebVerifier/GawebVerifier.csproj"
$output = "dist/GawebVerifier"

Write-Host "Building GawebVerifier..." -ForegroundColor Cyan

if (Test-Path $output) { Remove-Item $output -Recurse -Force }

dotnet publish $project -c Release -o $output -r win-x64 --self-contained false

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build Successful!" -ForegroundColor Green
    Write-Host "Output: $output" -ForegroundColor Gray
}
else {
    Write-Host "Build Failed!" -ForegroundColor Red
    exit 1
}
