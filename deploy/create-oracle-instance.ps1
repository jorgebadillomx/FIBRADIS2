# Script de reintentos para crear instancia Oracle Cloud ARM
# Corre en PowerShell — se detiene solo cuando la instancia se crea exitosamente
# Uso: .\deploy\create-oracle-instance.ps1

$Env:OCI_CLI_SUPPRESS_FILE_PERMISSIONS_WARNING = "True"

$compartmentId    = "ocid1.tenancy.oc1..aaaaaaaabdp36fddg5ucwcvtql3doolfcs233bl6ozgnxo46hwu7e7iyw5dq"
$availabilityDomain = "XlaJ:MX-MONTERREY-1-AD-1"
$imageId          = "ocid1.image.oc1.mx-monterrey-1.aaaaaaaaceynxvqo6j6txzkdpwuf4jckqf73y3ui5ozihvwggsmd5sccvryq"
$subnetId         = "ocid1.subnet.oc1.mx-monterrey-1.aaaaaaaazdum7gtdr2t4jing6qjv33fhb74rxtatrgjxohpy6uidadjyzsja"
$sshKeyFile       = "C:\Users\jorge\.ssh\jbadillo_server.pub"
$instanceName     = "fibradis-server"
$delaySeconds     = 180   # 3 minutos entre intentos (evita rate limit)

$attempt = 1

Write-Host ""
Write-Host "=== Oracle Cloud — Creando instancia ARM ===" -ForegroundColor Cyan
Write-Host "Shape: VM.Standard.A1.Flex (1 OCPU / 6 GB RAM — escalar a 4/24 después)"
Write-Host "Región: MX-MONTERREY-1 | Reintento cada $delaySeconds segundos"
Write-Host ""

while ($true) {
    Write-Host "[$((Get-Date).ToString('HH:mm:ss'))] Intento $attempt..." -ForegroundColor Yellow

    $result = oci compute instance launch `
        --availability-domain  $availabilityDomain `
        --compartment-id       $compartmentId `
        --shape                "VM.Standard.A1.Flex" `
        --shape-config         '{"ocpus": 1, "memoryInGBs": 6}' `
        --image-id             $imageId `
        --subnet-id            $subnetId `
        --assign-public-ip     true `
        --ssh-authorized-keys-file $sshKeyFile `
        --display-name         $instanceName `
        --boot-volume-size-in-gbs 100 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "  INSTANCIA CREADA EXITOSAMENTE" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host $result
        Write-Host ""
        Write-Host "Busca la IP pública en la consola Oracle: Compute → Instancias → fibradis-server"
        break
    }

    $errorText = $result | Out-String

    if ($errorText -like "*LimitExceeded*" -or $errorText -like "*QuotaExceeded*") {
        Write-Host "ERROR: Límite de recursos del free tier alcanzado." -ForegroundColor Red
        Write-Host "Puede que ya tengas una instancia ARM activa."
        break
    }

    if ($errorText -like "*InsufficientServiceLimits*") {
        Write-Host "ERROR: Límite de servicio insuficiente para tu cuenta." -ForegroundColor Red
        break
    }

    $shortError = if ($errorText -match '"message":\s*"([^"]+)"') { $Matches[1] } else { "sin capacidad o rate limit" }
    Write-Host "  Falló: $shortError" -ForegroundColor Gray
    Write-Host "  Esperando $delaySeconds segundos para reintentar..." -ForegroundColor Gray

    $attempt++
    Start-Sleep -Seconds $delaySeconds
}
