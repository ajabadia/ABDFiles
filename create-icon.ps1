Add-Type -AssemblyName System.Drawing

$png = [System.Drawing.Image]::FromFile("$PWD\src\CryptoTool\Assets\cryptotool.png")
$icon = [System.Drawing.Icon]::FromHandle($png.GetHicon())
$stream = [System.IO.File]::Create("$PWD\src\CryptoTool\Assets\cryptotool.ico")
$icon.Save($stream)
$stream.Close()
$png.Dispose()

Write-Host "Icon created successfully"
