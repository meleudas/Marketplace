param(
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$Database = "marketplace",
    [string]$User = "postgres",
    [string]$Password = $env:POSTGRES_PASSWORD,
    [string]$BackupDir = "./backups/postgres"
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($Password)) { $Password = "postgres" }

New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
$stamp = (Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ")
$outFile = Join-Path (Resolve-Path $BackupDir) "$Database-$stamp.sql"

$env:PGPASSWORD = $Password
& pg_dump -h $HostName -p $Port -U $User -d $Database --no-owner --no-acl -f $outFile

if (-not (Test-Path $outFile) -or (Get-Item $outFile).Length -eq 0) {
    throw "Backup file is empty: $outFile"
}

Write-Host "Backup written: $outFile"
