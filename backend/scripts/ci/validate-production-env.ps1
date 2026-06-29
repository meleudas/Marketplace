param(
    [string]$EnvFile = "backend/.env"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../../..")
Set-Location $repoRoot

$env:ASPNETCORE_ENVIRONMENT = "Production"

if (Test-Path $EnvFile) {
    Get-Content $EnvFile | ForEach-Object {
        $line = $_.Trim()
        if ($line -eq "" -or $line.StartsWith("#")) { return }
        $idx = $line.IndexOf("=")
        if ($idx -lt 1) { return }
        $name = $line.Substring(0, $idx).Trim()
        $value = $line.Substring($idx + 1).Trim()
        if ($value.StartsWith('"') -and $value.EndsWith('"')) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        [Environment]::SetEnvironmentVariable($name, $value, "Process")
    }
}

Write-Host "Validating production configuration (env file: $EnvFile)..."
dotnet run --project backend/src/Marketplace.API/Marketplace.API.csproj -c Release -- --validate-config-only
if ($LASTEXITCODE -ne 0) {
    Write-Error "Production configuration validation FAILED."
    exit 1
}

Write-Host "PASS: Production configuration validation succeeded."
