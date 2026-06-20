# =============================================================
# OPCIÓN IIS — Windows + SQL Server
# Genera el ZIP de producción y lo sube automáticamente por FTP.
# Para Linux + Docker, usa deploy/deploy.sh en su lugar.
# =============================================================
# Uso (desde cualquier directorio):
#   .\deploy\deploy-iis.ps1
#
# Credenciales FTP: deploy/ftp.env (no se commitea, ver .gitignore)
# Formato del archivo:
#   FTP_HOST=win8232.site4now.net
#   FTP_USER=usuario
#   FTP_PASS=contraseña
#   FTP_PATH=/FIBRADIS
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
npm run build:main; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
npm run build:ops;  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

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

# -- 5. Limpiar contenido runtime (uploads generados en dev/prod, no van en el paquete) --
$UploadsDir = Join-Path $Wwwroot "uploads"
if (Test-Path $UploadsDir) {
    Remove-Item -Recurse -Force $UploadsDir
    Write-Host "  uploads/ excluido del paquete"
}

# -- 5b. Podar runtimes nativos innecesarios --
# SkiaSharp publica binarios nativos para TODOS los RIDs (Linux, macOS, Windows x86/x64/arm64)
# mas archivos .pdb gigantes de debug (~244 MB). IIS corre en Windows: solo necesitamos win*.
# Esto reduce el paquete de ~164 MB a ~20 MB. Para Linux/Docker usa deploy/deploy.sh.
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

# -- 6. Crear ZIP --
Write-Host "=== Crear ZIP ===" -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $OutZip

$sizeMb = [math]::Round((Get-Item $OutZip).Length / 1MB, 1)
Write-Host ""
Write-Host "=== ZIP generado: $OutZip ($sizeMb MB) ===" -ForegroundColor Green

# -- 7. Cargar credenciales FTP --
Write-Host "=== Subir por FTP ===" -ForegroundColor Cyan
$FtpEnvFile = Join-Path $PSScriptRoot "ftp.env"
if (-not (Test-Path $FtpEnvFile)) {
    Write-Warning "No se encontró deploy/ftp.env. Coloca el archivo con FTP_HOST, FTP_USER, FTP_PASS, FTP_PATH para activar la subida automática."
    Write-Host "ZIP listo en: $OutZip" -ForegroundColor Yellow
    exit 0
}
$ftpCreds = @{}
Get-Content $FtpEnvFile | Where-Object { $_ -match '^\s*([^#=]+)=(.+)$' } | ForEach-Object {
    $key, $val = $_ -split '=', 2
    $ftpCreds[$key.Trim()] = $val.Trim()
}
$FtpHost = $ftpCreds['FTP_HOST']
$FtpUser = $ftpCreds['FTP_USER']
$FtpPass = $ftpCreds['FTP_PASS']
$FtpPath = $ftpCreds['FTP_PATH']

if (-not ($FtpHost -and $FtpUser -and $FtpPass -and $FtpPath)) {
    Write-Error "deploy/ftp.env incompleto. Requiere: FTP_HOST, FTP_USER, FTP_PASS, FTP_PATH."
    exit 1
}

$RemoteFile = "$($FtpPath.TrimEnd('/'))/fibradis.zip"
$FtpUri     = "ftp://$FtpHost$RemoteFile"
Write-Host "  Destino: $FtpUri"
Write-Host "  Tamaño:  $sizeMb MB — iniciando transferencia..."

try {
    $request = [System.Net.FtpWebRequest]::Create($FtpUri)
    $request.Method      = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $request.Credentials = New-Object System.Net.NetworkCredential($FtpUser, $FtpPass)
    $request.UseBinary   = $true
    $request.UsePassive  = $true
    $request.KeepAlive   = $false
    $request.ContentLength = (Get-Item $OutZip).Length

    $fs     = [System.IO.File]::OpenRead($OutZip)
    $rs     = $request.GetRequestStream()
    $buffer = New-Object byte[] 65536
    $sent   = 0
    $total  = $request.ContentLength
    while (($read = $fs.Read($buffer, 0, $buffer.Length)) -gt 0) {
        $rs.Write($buffer, 0, $read)
        $sent += $read
        $pct   = [math]::Round($sent / $total * 100, 0)
        Write-Progress -Activity "Subiendo fibradis.zip a FTP" -PercentComplete $pct -Status "$pct%  ($([math]::Round($sent/1MB,1)) / $sizeMb MB)"
    }
    $rs.Close()
    $fs.Close()

    $response = $request.GetResponse()
    Write-Progress -Activity "Subiendo fibradis.zip a FTP" -Completed
    Write-Host "  FTP respuesta: $($response.StatusDescription.Trim())" -ForegroundColor Green
    $response.Close()
}
catch {
    Write-Progress -Activity "Subiendo fibradis.zip a FTP" -Completed
    Write-Error "ERROR en FTP: $_"
    exit 1
}

Write-Host ""
Write-Host "=== Deploy completo ===" -ForegroundColor Green
Write-Host "El ZIP fue subido a $FtpUri"
Write-Host "Recuerda reiniciar el Application Pool en site4now.net si es necesario."
