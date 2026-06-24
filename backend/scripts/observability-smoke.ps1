param(
    [string]$ApiBaseUrl = "http://localhost:8080",
    [string]$PrometheusUrl = "http://localhost:9090",
    [string]$JaegerUrl = "http://localhost:16686",
    [string]$GrafanaUrl = "http://localhost:3001"
)

$ErrorActionPreference = "Stop"

Write-Host "Health check (liveness)..."
Invoke-RestMethod -Uri "$ApiBaseUrl/health" | Out-Null

Write-Host "Health check (readiness)..."
$ready = Invoke-RestMethod -Uri "$ApiBaseUrl/health/ready"
if ($null -eq $ready.status) { throw "/health/ready did not return status." }

Write-Host "Prometheus targets..."
$targets = Invoke-RestMethod -Uri "$PrometheusUrl/api/v1/targets"
$up = @($targets.data.activeTargets | Where-Object { $_.health -eq "up" })
if ($up.Count -eq 0) { throw "No Prometheus targets are UP." }

Write-Host "Jaeger UI..."
Invoke-WebRequest -Uri "$JaegerUrl" -UseBasicParsing | Out-Null

Write-Host "Grafana UI..."
Invoke-WebRequest -Uri "$GrafanaUrl/login" -UseBasicParsing | Out-Null

Write-Host "Observability smoke: OK"
