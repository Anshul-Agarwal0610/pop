param(
    [Parameter(Mandatory = $true)]
    [string]$BackendUrl,
    [string]$OutputDirectory = ""
)

$frontendRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$repositoryRoot = (Resolve-Path (Join-Path $frontendRoot "..")).Path
$deployRoot = Join-Path $repositoryRoot ".deploy"
$packageRoot = if ($OutputDirectory) {
    [IO.Path]::GetFullPath($OutputDirectory)
} else {
    Join-Path $deployRoot "frontend"
}
$packageZip = "$packageRoot.zip"

if (-not $packageRoot.StartsWith($repositoryRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDirectory must be inside the repository."
}

Push-Location $frontendRoot
try {
    $env:NEXT_PUBLIC_API_URL = $BackendUrl.TrimEnd("/")
    npm run build
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} finally {
    Pop-Location
}

if (Test-Path $packageRoot) {
    Remove-Item -LiteralPath $packageRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null

Copy-Item (Join-Path $frontendRoot ".next\standalone\*") $packageRoot -Recurse -Force
New-Item -ItemType Directory -Force -Path (Join-Path $packageRoot ".next\static") | Out-Null
Copy-Item (Join-Path $frontendRoot ".next\static\*") (Join-Path $packageRoot ".next\static") -Recurse -Force
Copy-Item (Join-Path $frontendRoot "public") (Join-Path $packageRoot "public") -Recurse -Force
Copy-Item (Join-Path $PSScriptRoot "azure-web.config") (Join-Path $packageRoot "web.config") -Force

$serverPath = Join-Path $packageRoot "server.js"
$serverSource = Get-Content $serverPath -Raw
$patchedServer = $serverSource.Replace(
    "const currentPort = parseInt(process.env.PORT, 10) || 3000",
    "const currentPort = process.env.PORT || 3000"
)
if ($patchedServer -eq $serverSource) {
    throw "Could not patch the standalone server PORT handling."
}
Set-Content -LiteralPath $serverPath -Value $patchedServer -NoNewline

if (Test-Path $packageZip) {
    Remove-Item -LiteralPath $packageZip -Force
}
Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $packageZip -Force
Write-Output $packageZip
