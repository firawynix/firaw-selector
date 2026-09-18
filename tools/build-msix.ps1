[CmdletBinding()]
param(
    [ValidateSet('Store', 'Lab')]
    [string]$Channel = 'Store'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$templatePath = Join-Path $projectRoot 'packaging\windows\msix\AppxManifest.xml.in'
$stageRoot = Join-Path $projectRoot 'build\msix'
$outputRoot = Join-Path $projectRoot 'dist'

$versionSource = Get-Content -LiteralPath (Join-Path $projectRoot 'Core.cs') -Raw
$versionMatch = [regex]::Match(
    $versionSource,
    'public const string Versao\s*=\s*"([0-9]+\.[0-9]+\.[0-9]+)"'
)
if (-not $versionMatch.Success) {
    throw 'Não foi possível obter Amb.Versao em Core.cs.'
}
$version = "$($versionMatch.Groups[1].Value).0"

$sdkBin = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Directory |
    Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
    Sort-Object { [version]$_.Name } -Descending |
    Select-Object -First 1
if (-not $sdkBin) {
    throw 'Windows SDK não encontrado.'
}
$makeAppx = Join-Path $sdkBin.FullName 'x64\makeappx.exe'
$signTool = Join-Path $sdkBin.FullName 'x64\signtool.exe'
if (-not (Test-Path -LiteralPath $makeAppx)) {
    throw 'MakeAppx.exe não encontrado no Windows SDK.'
}

& (Join-Path $projectRoot 'build.cmd')
if ($LASTEXITCODE -ne 0) {
    throw 'Falha na compilação do FirawSelector.'
}

$packageName = 'Firawynix.FirawSelector'
$publisher = 'CN=1FDE3668-C222-4506-AFE6-E2E425EAECD8'
$suffix = 'store'
$signingThumbprint = ''
if ($Channel -eq 'Lab') {
    $certificate = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
        Where-Object {
            $_.Subject -eq 'CN=Firawynix Laboratorio' -and
            $_.HasPrivateKey -and
            $_.NotAfter -gt (Get-Date)
        } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1
    if (-not $certificate) {
        throw 'Certificado CN=Firawynix Laboratorio não encontrado.'
    }
    $packageName = 'Firawynix.FirawSelector.Lab'
    $publisher = $certificate.Subject
    $signingThumbprint = $certificate.Thumbprint
    $suffix = 'sideload'
}

$stage = Join-Path $stageRoot $suffix
if (Test-Path -LiteralPath $stage) {
    $resolvedStageRoot = [IO.Path]::GetFullPath($stageRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedStage = [IO.Path]::GetFullPath($stage)
    if (-not $resolvedStage.StartsWith($resolvedStageRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Diretório de estágio fora da raiz esperada: $resolvedStage"
    }
    Remove-Item -LiteralPath $stage -Recurse -Force
}
New-Item -ItemType Directory -Path (Join-Path $stage 'Assets') -Force | Out-Null

foreach ($file in @(
    'FirawSelector.exe',
    'FirawSelector Studio.exe',
    'FirawSelector Host.exe'
)) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $file) -Destination $stage
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'extensions') `
    -Destination (Join-Path $stage 'Extensions') -Recurse

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
$iconTool = Join-Path $stageRoot 'mkico.exe'
& $csc /nologo /target:exe /codepage:65001 /r:System.dll /r:System.Drawing.dll `
    "/out:$iconTool" (Join-Path $projectRoot 'tools\mkico.cs')
if ($LASTEXITCODE -ne 0) {
    throw 'Falha ao compilar o gerador de ativos.'
}
& $iconTool (Join-Path $stage 'firawselector.ico') (Join-Path $stage 'Assets')
if ($LASTEXITCODE -ne 0) {
    throw 'Falha ao gerar os ativos MSIX.'
}

$manifest = (Get-Content -LiteralPath $templatePath -Raw).
    Replace('@@PACKAGE_NAME@@', $packageName).
    Replace('@@PUBLISHER@@', $publisher).
    Replace('@@VERSION@@', $version)
$manifestPath = Join-Path $stage 'AppxManifest.xml'
[IO.File]::WriteAllText($manifestPath, $manifest, [Text.UTF8Encoding]::new($false))

$output = Join-Path $outputRoot "FirawSelector-$version-x86-$suffix.msix"
& $makeAppx pack /o /d $stage /p $output
if ($LASTEXITCODE -ne 0) {
    throw 'MakeAppx não conseguiu gerar o pacote.'
}

if ($Channel -eq 'Lab') {
    if (-not (Test-Path -LiteralPath $signTool)) {
        throw 'SignTool.exe não encontrado no Windows SDK.'
    }
    & $signTool sign /sha1 $signingThumbprint /fd SHA256 $output
    if ($LASTEXITCODE -ne 0) {
        throw 'Falha ao assinar o MSIX de sideload.'
    }
    & $signTool verify /pa /v $output
    if ($LASTEXITCODE -ne 0) {
        throw 'A assinatura do MSIX de sideload não passou na validação.'
    }
}

$hash = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant()
$hashPath = "$output.sha256"
[IO.File]::WriteAllText(
    $hashPath,
    "$hash  $([IO.Path]::GetFileName($output))$([Environment]::NewLine)",
    [Text.Encoding]::ASCII
)
Write-Host "Pacote criado: $output" -ForegroundColor Green
Write-Host "SHA-256: $hash" -ForegroundColor Cyan
