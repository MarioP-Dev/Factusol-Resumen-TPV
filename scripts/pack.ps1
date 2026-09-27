# Empaqueta ResumenTPV: Velopack (OTA) + asistente de instalación tradicional.
# Uso: powershell -ExecutionPolicy Bypass -File scripts/pack.ps1 [-Version 0.3.0]

param(
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if (-not $Version) {
    $csproj = Get-Content "src/ResumenTPV/ResumenTPV.csproj" -Raw
    if ($csproj -match "<Version>([^<]+)</Version>") {
        $Version = $Matches[1]
    } else {
        throw "No se pudo leer <Version> del csproj."
    }
}

Write-Host "=== ResumenTPV pack $Version ===" -ForegroundColor Cyan

Write-Host "1/4 Publish app win-x64..."
dotnet publish src/ResumenTPV/ResumenTPV.csproj `
    -c Release -r win-x64 --self-contained true `
    -p:Version=$Version `
    -o publish
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falló" }

Write-Host "2/4 Velopack pack (instalador interno one-click)..."
if (Test-Path releases) { Remove-Item releases -Recurse -Force }
New-Item -ItemType Directory releases | Out-Null

vpk pack `
    --packId ResumenTPV `
    --packVersion $Version `
    --packDir publish `
    --mainExe ResumenTPV.exe `
    --packTitle ResumenTPV `
    --packAuthors "Mario P. Dev" `
    --icon src/ResumenTPV/Assets/app.ico `
    --splashImage src/ResumenTPV/Assets/installer-splash.png `
    --splashProgressColor "#FFFFFF" `
    --shortcuts StartMenuRoot `
    --runtime win-x64 `
    --outputDir releases
if ($LASTEXITCODE -ne 0) { throw "vpk pack falló" }

$inner = Join-Path $root "releases\ResumenTPV-win-Setup.exe"
if (-not (Test-Path $inner)) {
    $inner = Get-ChildItem releases -Filter "*Setup.exe" | Select-Object -First 1 -ExpandProperty FullName
}
if (-not $inner -or -not (Test-Path $inner)) {
    throw "No se encontró el Setup.exe de Velopack en releases/"
}

$innerDest = Join-Path $root "releases\ResumenTPV-win-Setup.inner.exe"
Move-Item -Force $inner $innerDest
Write-Host "   Inner setup -> $innerDest"

Write-Host "3/4 Publish asistente (incrusta inner setup)..."
dotnet publish src/ResumenTPV.Setup/ResumenTPV.Setup.csproj `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:Version=$Version `
    -p:VelopackSetupPath="$innerDest" `
    -o publish-setup
if ($LASTEXITCODE -ne 0) { throw "dotnet publish Setup falló" }

Write-Host "4/4 Copiar asistente como Setup de distribución..."
$wizard = Join-Path $root "publish-setup\ResumenTPV-Setup.exe"
if (-not (Test-Path $wizard)) {
    throw "No se generó ResumenTPV-Setup.exe"
}

$final = Join-Path $root "releases\ResumenTPV-win-Setup.exe"
Copy-Item -Force $wizard $final

# Limpiar el inner del directorio de releases (no distribuir one-click suelto).
Remove-Item -Force $innerDest -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Listo:" -ForegroundColor Green
Get-ChildItem releases | Select-Object Name, @{N='MB';E={[math]::Round($_.Length/1MB,1)}} | Format-Table -AutoSize
Write-Host "Distribuir: releases\ResumenTPV-win-Setup.exe (asistente con términos + acceso directo)"
Write-Host "OTA:        releases\*.nupkg + releases.win.json"
