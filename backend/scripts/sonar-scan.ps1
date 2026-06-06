param(
    [string]$SonarHostUrl = $env:SONAR_HOST_URL,
    [string]$SonarToken = $env:SONAR_TOKEN
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SonarHostUrl) -or [string]::IsNullOrWhiteSpace($SonarToken)) {
    $repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $envFile = Join-Path $repoRoot "backend/.env"

    if (Test-Path $envFile) {
        Get-Content $envFile | ForEach-Object {
            $line = $_.Trim()
            if ($line -eq "" -or $line.StartsWith("#") -or -not $line.Contains("=")) { return }

            $parts = $line.Split("=", 2)
            $key = $parts[0].Trim()
            $value = if ($parts.Count -gt 1) { $parts[1].Trim() } else { "" }

            if ($key -eq "SONAR_HOST_URL" -and [string]::IsNullOrWhiteSpace($SonarHostUrl)) {
                $SonarHostUrl = $value
            }
            elseif ($key -eq "SONAR_TOKEN" -and [string]::IsNullOrWhiteSpace($SonarToken)) {
                $SonarToken = $value
            }
        }
    }
}

if ([string]::IsNullOrWhiteSpace($SonarHostUrl) -or [string]::IsNullOrWhiteSpace($SonarToken)) {
    throw "Set SONAR_HOST_URL and SONAR_TOKEN environment variables."
}

$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

$sonarPropsPath = Join-Path $root "sonar-project.properties"
$tempDisabledSonarPropsPath = Join-Path $root "sonar-project.properties.disabled-by-dotnet-scanner"
$sonarPropsTemporarilyDisabled = $false

try {
    # SonarScanner for .NET не підтримує sonar-project.properties у робочому каталозі.
    if (Test-Path $sonarPropsPath) {
        if (Test-Path $tempDisabledSonarPropsPath) {
            throw "Temporary file already exists: $tempDisabledSonarPropsPath. Remove it and retry."
        }

        Move-Item -Path $sonarPropsPath -Destination $tempDisabledSonarPropsPath
        $sonarPropsTemporarilyDisabled = $true
    }

    dotnet tool install --global dotnet-sonarscanner --ignore-failed-sources | Out-Null

    # SonarScanner for .NET: не передавати sonar.sources/tests (обчислюються автоматично)
    # і не задавати sonar.cs.analyzer.projectOutPaths з glob (** ламає C# plugin).
    dotnet sonarscanner begin `
        /k:"marketplace-backend" `
        /d:sonar.host.url="$SonarHostUrl" `
        /d:sonar.token="$SonarToken" `
        /d:sonar.exclusions="**/Migrations/**,**/Contracts/**,**/test-results/**" `
        /d:sonar.cs.opencover.reportsPaths="tests/**/test-results/**/coverage.cobertura.xml,tests/**/sonar-coverage/**/coverage.cobertura.xml"

    dotnet build Marketplace.slnx -c Release
    dotnet test tests/Marketplace.Tests.Unit/Marketplace.Tests.Unit.csproj -c Release --no-build `
        /p:CollectCoverage=true `
        /p:CoverletOutput=test-results/sonar-coverage/ `
        /p:CoverletOutputFormat=cobertura
    dotnet test tests/Marketplace.Tests.Integration/Marketplace.Tests.Integration.csproj -c Release --no-build `
        /p:CollectCoverage=true `
        /p:CoverletOutput=test-results/sonar-coverage-integration/ `
        /p:CoverletOutputFormat=cobertura

    dotnet sonarscanner end /d:sonar.token="$SonarToken"

    # Після end проект/аналіз може з'явитися в SonarQube з затримкою (через compute engine),
    # тож робимо короткий retry на project_status.
    $pair = "${SonarToken}:"
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($pair)
    $base64 = [System.Convert]::ToBase64String($bytes)
    $authHeaders = @{ Authorization = "Basic $base64" }

    $maxAttempts = 120
    $delaySeconds = 2
    $status = $null

    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        try {
            $status = Invoke-RestMethod -Uri "$SonarHostUrl/api/qualitygates/project_status?projectKey=marketplace-backend" `
                -Headers $authHeaders
            break
        }
        catch {
            $respCode = $null
            try { $respCode = [int]$_.Exception.Response.StatusCode } catch { }

            # Sonar може повертати 404, поки проект/analysis ще не з'явився (compute engine delay).
            if ($respCode -eq 404 -or $_.Exception.Message -like "*(404)*") {
                Start-Sleep -Seconds $delaySeconds
                continue
            }

            throw
        }
    }

    $gateStatus = if ($status -and $status.projectStatus) { $status.projectStatus.status } else { "<no-status>" }

    switch ($gateStatus) {
        "OK" {
            Write-Host "SonarQube Quality Gate: OK"
        }
        "NONE" {
            Write-Host "Sonar scan uploaded successfully. Quality Gate is not configured yet (status=NONE)."
            Write-Host "Assign a Quality Gate in Sonar UI: http://localhost:9002 (Project Settings -> Quality Gate)."
        }
        default {
            throw "SonarQube Quality Gate failed: $gateStatus"
        }
    }
}
finally {
    if ($sonarPropsTemporarilyDisabled -and (Test-Path $tempDisabledSonarPropsPath)) {
        Move-Item -Path $tempDisabledSonarPropsPath -Destination $sonarPropsPath -Force
    }

    Pop-Location
}
