# =============================================================
# OPCIÓN IIS — Windows + SQL Server
# Genera el ZIP de producción listo para subir a site4now.net.
# Para Linux + Docker, usa deploy/deploy.sh en su lugar.
# =============================================================
# Uso (desde cualquier directorio):
#   .\deploy\deploy-iis.ps1
#
# Pasos posteriores (manual en site4now.net):
#   1. DETÉN el Application Pool (IIS bloquea los DLLs mientras está activo)
#   2. Sube deploy/release/fibradis.zip al panel de control
#   3. Extrae en el directorio raíz del sitio (reemplaza todo)
#   4. INICIA el Application Pool
# =============================================================

$ErrorActionPreference = "Stop"

# Paths absolutos para evitar problemas de working directory
$Root       = Split-Path $PSScriptRoot -Parent
$PublishDir = Join-Path $Root "publish\iis"
$OutZip     = Join-Path $Root "deploy\release\fibradis.zip"

Write-Host "Root:       $Root"
Write-Host "PublishDir: $PublishDir"
Write-Host "OutZip:     $OutZip"
Write-Host ""

# -- 1. Limpiar output anterior --
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }
New-Item -ItemType Directory -Force $PublishDir | Out-Null
if (Test-Path $OutZip)     { Remove-Item -Force $OutZip }

# -- 2. Build frontend --
Write-Host "=== Build frontend (Main + Ops) ===" -ForegroundColor Cyan
Set-Location $Root
npm run build:main
npm run build:ops

# -- 3. Publicar API .NET (clean fuerza recompilación total) --
Write-Host "=== Publicar API ===" -ForegroundColor Cyan
dotnet clean (Join-Path $Root "src\Server\Api") -c Release -v quiet
dotnet publish (Join-Path $Root "src\Server\Api") -c Release -o $PublishDir

# Verificar que los DLLs están ahí antes de continuar
if (-not (Test-Path (Join-Path $PublishDir "Api.dll"))) {
    Write-Error "ERROR: dotnet publish no generó Api.dll en $PublishDir. Revisa los errores de compilación."
    exit 1
}
Write-Host "  Api.dll: OK ($(((Get-Item (Join-Path $PublishDir 'Api.dll')).LastWriteTime).ToString('yyyy-MM-dd HH:mm')))"

# -- 4. Copiar SPAs a wwwroot --
Write-Host "=== Copiar SPAs a wwwroot ===" -ForegroundColor Cyan
$Wwwroot = Join-Path $PublishDir "wwwroot"
New-Item -ItemType Directory -Force $Wwwroot | Out-Null
New-Item -ItemType Directory -Force (Join-Path $Wwwroot "ops") | Out-Null
Copy-Item -Recurse -Force (Join-Path $Root "src\Web\Main\dist\*") $Wwwroot
Copy-Item -Recurse -Force (Join-Path $Root "src\Web\Ops\dist\*")  (Join-Path $Wwwroot "ops")

# -- 5. Crear ZIP --
Write-Host "=== Crear ZIP ===" -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $OutZip

$sizeMb = [math]::Round((Get-Item $OutZip).Length / 1MB, 1)
Write-Host ""
Write-Host "=== Listo === ZIP: $OutZip ($sizeMb MB)" -ForegroundColor Green
Write-Host "Sube el ZIP al panel de site4now.net y reinicia el Application Pool."
