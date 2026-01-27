$ErrorActionPreference = "Stop"
$rootDir = Get-Location
$outDir = Join-Path $rootDir "dist_final"
$binDir = Join-Path $outDir "bin"

Write-Host "Started Final Build Bundle (Clean Layout)..." -ForegroundColor Cyan

# 1. Clean Output Directory
if (Test-Path $outDir) {
    Write-Host "Cleaning existing dist_final..." -ForegroundColor Yellow
    Remove-Item $outDir -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $binDir | Out-Null

# 2. Define Projects
$projects = @(
    @{ Name = "GeneradorCartas"; Path = "src/GeneradorCartas/GeneradorCartas.csproj" },
    @{ Name = "EtlConfig"; Path = "src/EtlConfig/EtlConfig.csproj" },
    @{ Name = "LetterConfig"; Path = "src/LetterConfig/LetterConfig.csproj" },
    @{ Name = "EtlConverter"; Path = "src/EtlConverter/EtlConverter.csproj" },
    @{ Name = "CryptoTool"; Path = "src/CryptoTool/CryptoTool.csproj" }
)

# 3. Build Loop (Using dotnet build as it is proven to work in this env)
foreach ($proj in $projects) {
    Write-Host "Building $($proj.Name)..." -ForegroundColor Green
    
    $projPath = Join-Path $rootDir $proj.Path
    $projOut = Join-Path $binDir $proj.Name
    
    # Using 'dotnet build' -c Release 
    # This produces artifacts in the subfolder, keeping root clean.
    dotnet build $projPath `
        -c Release `
        -o $projOut `
        -v minimal
        
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build $($proj.Name)"
        exit 1
    }
    
    # 4. Create Launcher Bat in Root
    $batPath = Join-Path $outDir "$($proj.Name).bat"
    
    # For GUI apps (Start detaches)
    if ($proj.Name -in "GeneradorCartas", "EtlConfig", "LetterConfig", "CryptoTool") {
        $batContent = "@echo off`r`nstart `"`" `"bin\$($proj.Name)\$($proj.Name).exe`" %*"
    }
    else {
        # For Console (Stay attached)
        $batContent = "@echo off`r`n`"bin\$($proj.Name)\$($proj.Name).exe`" %*"
    }
    
    Set-Content -Path $batPath -Value $batContent
}

# 5. Copy Assets for GeneradorCartas
$presetsSrc = Join-Path $rootDir "presets"
$presetsDst = Join-Path $binDir "GeneradorCartas\presets"

if (Test-Path $presetsSrc) {
    Write-Host "Copying Presets to GeneradorCartas..." -ForegroundColor Yellow
    Copy-Item -Path $presetsSrc -Destination $presetsDst -Recurse -Force
}

# 6. Copy Assets for EtlConfig (if any embedded presets needed? Usually in AppData)
# But we can create the 'Presets' folder structure if needed
$etlPresetDst = Join-Path $binDir "EtlConfig\Presets"
if (-not (Test-Path $etlPresetDst)) { New-Item -ItemType Directory -Path $etlPresetDst | Out-Null }

Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "Run the .bat files in: $outDir" -ForegroundColor Cyan
