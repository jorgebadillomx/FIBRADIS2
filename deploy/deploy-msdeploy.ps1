# =============================================================
# OPCIÓN MSDeploy — Windows IIS compartido (site4now.net)
# Build completo + deploy via Web Deploy (puerto 8172).
# No requiere descomprimir manualmente en el servidor.
#
# Credenciales: deploy/msdeploy.env (no se commitea)
# Formato:
#   MS_COMPUTER=https://win8232.site4now.net:8172/msdeploy.axd?site=FIBRADIS
#   MS_USER=usuario
#   MS_PASS=contraseña
#
# Para obtener las credenciales: panel hosting → "Show WebDeploy Info"
# Para FTP tradicional usa deploy/deploy-iis.ps1 en su lugar.
# =============================================================

$ErrorActionPreference = "Stop"

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
if (Test-Path $OutZip) { Remove-Item -Force $OutZip }

# -- 2. Build frontend --
Write-Host "=== Build frontend (Main + Ops) ===" -ForegroundColor Cyan
Set-Location $Root
npm run build:main; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
npm run build:ops;  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# -- 3. Publicar API .NET --
Write-Host "=== Publicar API ===" -ForegroundColor Cyan
dotnet clean (Join-Path $Root "src\Server\Api") -c Release -v quiet
dotnet publish (Join-Path $Root "src\Server\Api") -c Release -o $PublishDir

if (-not (Test-Path (Join-Path $PublishDir "Api.dll"))) {
    Write-Error "ERROR: dotnet publish no generó Api.dll en $PublishDir."
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

# -- 5. Excluir uploads y podar runtimes nativos --
$UploadsDir = Join-Path $Wwwroot "uploads"
if (Test-Path $UploadsDir) {
    Remove-Item -Recurse -Force $UploadsDir
    Write-Host "  uploads/ excluido del paquete"
}

$RuntimesDir = Join-Path $PublishDir "runtimes"
if (Test-Path $RuntimesDir) {
    $KeepRids = @('win', 'win-x64', 'win-x86')
    Get-ChildItem $RuntimesDir -Directory |
        Where-Object { $KeepRids -notcontains $_.Name } |
        Remove-Item -Recurse -Force
    Get-ChildItem $RuntimesDir -Recurse -File -Filter *.pdb | Remove-Item -Force
    $rtMb = [math]::Round((Get-ChildItem $RuntimesDir -Recurse -File | Measure-Object Length -Sum).Sum / 1MB, 1)
    Write-Host "  runtimes/ podado a win* sin .pdb ($rtMb MB)"
}

# -- 6. Crear ZIP (paquete MSDeploy) --
Write-Host "=== Crear ZIP ===" -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $OutZip
$sizeMb = [math]::Round((Get-Item $OutZip).Length / 1MB, 1)
Write-Host "  ZIP: $OutZip ($sizeMb MB)"

# -- 7. Cargar credenciales MSDeploy --
Write-Host ""
Write-Host "=== Deploy via MSDeploy ===" -ForegroundColor Cyan
$EnvFile = Join-Path $PSScriptRoot "msdeploy.env"
if (-not (Test-Path $EnvFile)) {
    Write-Warning "No se encontró deploy/msdeploy.env."
    Write-Warning "Crea el archivo con MS_COMPUTER, MS_USER, MS_PASS (ver Show WebDeploy Info en el panel)."
    Write-Host "ZIP listo en: $OutZip" -ForegroundColor Yellow
    exit 0
}

$creds = @{}
Get-Content $EnvFile | Where-Object { $_ -match '^\s*([^#=]+)=(.+)$' } | ForEach-Object {
    $key, $val = $_ -split '=', 2
    $creds[$key.Trim()] = $val.Trim()
}
$MsComputer = $creds['MS_COMPUTER']
$MsUser     = $creds['MS_USER']
$MsPass     = $creds['MS_PASS']

if (-not ($MsComputer -and $MsUser -and $MsPass)) {
    Write-Error "msdeploy.env incompleto. Requiere: MS_COMPUTER, MS_USER, MS_PASS."
    exit 1
}

# -- 8. Buscar msdeploy.exe --
$MsDeploy = Get-Command msdeploy.exe -ErrorAction SilentlyContinue
if (-not $MsDeploy) {
    $candidates = @(
        "C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe",
        "C:\Program Files (x86)\IIS\Microsoft Web Deploy V3\msdeploy.exe"
    )
    $MsDeploy = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $MsDeploy) {
    Write-Error "msdeploy.exe no encontrado. Instala 'Web Deploy 3.6' desde IIS."
    exit 1
}
Write-Host "  msdeploy: $MsDeploy"
Write-Host "  Destino:  $MsComputer"
Write-Host "  Tamaño:   $sizeMb MB"
Write-Host ""

# -- 9. Ejecutar deploy --
$SiteName = ($MsComputer -split 'site=')[1]
$LogFile  = Join-Path $PSScriptRoot "msdeploy-log.txt"

$msArgs = @(
    "-verb:sync",
    "-source:contentPath=$PublishDir",
    "-dest:contentPath=$SiteName,computerName=$MsComputer,userName=$MsUser,password=$MsPass,authType=Basic",
    "-allowUntrusted",
    "-enableRule:AppOffline",
    "-retryAttempts:5",
    "-retryInterval:3000"
)

Start-Process -FilePath $MsDeploy -ArgumentList $msArgs -Wait -NoNewWindow `
    -RedirectStandardOutput $LogFile -RedirectStandardError "$LogFile.err"

$stdout = Get-Content $LogFile -ErrorAction SilentlyContinue
$stderr = Get-Content "$LogFile.err" -ErrorAction SilentlyContinue

$stdout | Select-Object -Last 5 | ForEach-Object { Write-Host "  $_" }

if ($stderr) {
    Write-Error "MSDeploy reportó errores:`n$($stderr -join "`n")"
    exit 1
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "MSDeploy falló con código $LASTEXITCODE."
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "=== Deploy completo ===" -ForegroundColor Green
Write-Host "El Application Pool se recicla automáticamente al terminar."
