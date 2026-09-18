[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$packagePath = Join-Path $projectRoot 'dist\FirawSelector-1.1.5.0-x86-sideload.msix'
$certificatePath = Join-Path $projectRoot 'dist\Firawynix-Laboratorio.cer'
$reportPath = Join-Path $projectRoot 'dist\msix-install-result.json'
$identity = 'Firawynix.FirawSelector.Lab'
$thumbprint = '0A163BEB87A46058051A717F5456E5FD2D00FD80'

$principal = New-Object Security.Principal.WindowsPrincipal(
    [Security.Principal.WindowsIdentity]::GetCurrent()
)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Execute este teste em uma janela do PowerShell como administrador.'
}

$addedStores = New-Object Collections.Generic.List[string]
$installedByTest = $false
$result = [ordered]@{
    startedAt = (Get-Date).ToString('o')
    success = $false
    package = $null
    removed = $false
    error = $null
}

try {
    if (Get-AppxPackage -Name $identity -ErrorAction SilentlyContinue) {
        throw "O pacote de laboratório já estava instalado: $identity"
    }
    foreach ($store in @('TrustedPeople', 'Root')) {
        $storePath = "Cert:\LocalMachine\$store"
        if (-not (Test-Path -LiteralPath (Join-Path $storePath $thumbprint))) {
            Import-Certificate -FilePath $certificatePath -CertStoreLocation $storePath | Out-Null
            $addedStores.Add($store)
        }
    }
    Add-AppxPackage -Path $packagePath -ForceApplicationShutdown
    $installedByTest = $true
    $installed = Get-AppxPackage -Name $identity -ErrorAction Stop
    $result.package = [ordered]@{
        name = $installed.Name
        version = $installed.Version.ToString()
        architecture = $installed.Architecture.ToString()
        status = $installed.Status.ToString()
    }
    $result.success = $true
} catch {
    $result.error = $_.Exception.Message
} finally {
    if ($installedByTest) {
        $installed = Get-AppxPackage -Name $identity -ErrorAction SilentlyContinue
        if ($installed) {
            Remove-AppxPackage -Package $installed.PackageFullName -ErrorAction Stop
            $result.removed = -not [bool](Get-AppxPackage -Name $identity -ErrorAction SilentlyContinue)
        }
    }
    foreach ($store in $addedStores) {
        $certificate = Get-Item -LiteralPath "Cert:\LocalMachine\$store\$thumbprint" -ErrorAction SilentlyContinue
        if ($certificate) { Remove-Item -LiteralPath $certificate.PSPath -Force }
    }
    $result.finishedAt = (Get-Date).ToString('o')
    [IO.File]::WriteAllText(
        $reportPath,
        ($result | ConvertTo-Json -Depth 5),
        [Text.UTF8Encoding]::new($false)
    )
}

if (-not $result.success) { throw $result.error }
if (-not $result.removed) { throw 'O pacote foi instalado, mas a remoção não foi confirmada.' }
Write-Host "Instalação e remoção validadas. Relatório: $reportPath" -ForegroundColor Green
