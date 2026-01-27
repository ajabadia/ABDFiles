$project = "EtlConverter"
$src = "src\$project"
$output = "dist_modular\$project"

Write-Host "Building $project..."
dotnet publish "$src\$project.csproj" -c Release -o $output /p:UseAppHost=true /p:PublishSingleFile=true /p:SelfContained=false /p:DebugType=None /p:DebugSymbols=false

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!"
    $size = (Get-Item "$output\$project.exe").Length / 1MB
    Write-Host ("Size: {0:N2} MB" -f $size)
}
else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
