$ErrorActionPreference = "Stop"
$root = "C:/Programing/Projects/Marketplace"
$src = Join-Path $root "backend/tests/Marketplace.Tests"
$unit = Join-Path $root "backend/tests/Marketplace.Tests.Unit"
$integration = Join-Path $root "backend/tests/Marketplace.Tests.Integration"

function Ensure-Dir([string]$path) {
    if (-not (Test-Path $path)) { New-Item -ItemType Directory -Path $path -Force | Out-Null }
}

function Move-File([string]$from, [string]$to) {
    if (-not (Test-Path $from)) { return }
    Ensure-Dir (Split-Path $to -Parent)
    if (Test-Path $to) { Remove-Item $to -Force }
    Move-Item -Path $from -Destination $to -Force
}

$integrationMap = @{
    "IntegrationIdentityAccessSqliteTests.cs" = "IdentityAccess/IntegrationIdentityAccessSqliteTests.cs"
    "IntegrationCartCheckoutSqliteTests.cs" = "Cart/IntegrationCartCheckoutSqliteTests.cs"
    "IntegrationOrdersSqliteTests.cs" = "Orders/IntegrationOrdersSqliteTests.cs"
    "IntegrationPaymentsSqliteTests.cs" = "Payments/IntegrationPaymentsSqliteTests.cs"
    "IntegrationPlatformSqliteTests.cs" = "Platform/IntegrationPlatformSqliteTests.cs"
    "IntegrationNotificationsSqliteTests.cs" = "Notifications/IntegrationNotificationsSqliteTests.cs"
    "IntegrationFavoritesSqliteTests.cs" = "Favorites/IntegrationFavoritesSqliteTests.cs"
    "IntegrationCatalogCategoriesSqliteTests.cs" = "Catalog/IntegrationCatalogCategoriesSqliteTests.cs"
    "IntegrationProductsModerationSqliteTests.cs" = "Products/IntegrationProductsModerationSqliteTests.cs"
    "IntegrationReviewsSqliteTests.cs" = "Reviews/IntegrationReviewsSqliteTests.cs"
    "IntegrationCompaniesWorkspaceSqliteTests.cs" = "Companies/IntegrationCompaniesWorkspaceSqliteTests.cs"
    "IntegrationInventorySqliteTests.cs" = "Inventory/IntegrationInventorySqliteTests.cs"
}

foreach ($entry in $integrationMap.GetEnumerator()) {
    Move-File (Join-Path $src $entry.Key) (Join-Path $integration $entry.Value)
}

$apiDomainMap = @{
    "ApiAuthControllerTests.cs" = "Api/IdentityAccess"
    "ApiAccountControllerTests.cs" = "Api/IdentityAccess"
    "ApiUsersControllerTests.cs" = "Api/IdentityAccess"
    "ApiExternalAuthControllerTests.cs" = "Api/IdentityAccess"
    "ApiOrdersControllerTests.cs" = "Api/Orders"
    "ApiPaymentsControllerTests.cs" = "Api/Payments"
    "ApiAdminOutboxControllerTests.cs" = "Api/Platform"
    "ApiMeNotificationsControllerTests.cs" = "Api/Notifications"
    "ApiPushNotificationsControllerTests.cs" = "Api/Notifications"
    "ApiCatalogControllerTests.cs" = "Api/Catalog"
    "ApiAdminCatalogControllerTests.cs" = "Api/Catalog"
    "ApiProductsControllerTests.cs" = "Api/Products"
    "ApiAdminProductsControllerTests.cs" = "Api/Products"
    "ApiInventoryControllerTests.cs" = "Api/Inventory"
    "ApiFavoritesControllerTests.cs" = "Api/Favorites"
    "ApiCompanyMembersControllerTests.cs" = "Api/Companies"
    "ApiProductReviewsControllerTests.cs" = "Api/Reviews"
    "ApiCompanyReviewsControllerTests.cs" = "Api/Reviews"
    "ApiAdminReviewsControllerTests.cs" = "Api/Reviews"
    "ApiReviewRepliesControllerTests.cs" = "Api/Reviews"
}

foreach ($entry in $apiDomainMap.GetEnumerator()) {
    Move-File (Join-Path $src $entry.Key) (Join-Path $unit "$($entry.Value)/$($entry.Key)")
}

Move-File (Join-Path $src "SecurityRegressionTests.cs") (Join-Path $unit "Regression/SecurityRegressionTests.cs")
Move-File (Join-Path $src "ApiRegressionIdempotencyTests.cs") (Join-Path $unit "Regression/ApiRegressionIdempotencyTests.cs")
Move-File (Join-Path $src "PerformanceBaselineTests.cs") (Join-Path $unit "Regression/PerformanceBaselineTests.cs")
Move-File (Join-Path $src "ContractApiRoutesSnapshotTests.cs") (Join-Path $unit "Contract/ContractApiRoutesSnapshotTests.cs")

if (Test-Path (Join-Path $src "Contracts")) {
    Ensure-Dir (Join-Path $unit "Contracts")
    Get-ChildItem (Join-Path $src "Contracts") | ForEach-Object {
        Move-File $_.FullName (Join-Path (Join-Path $unit "Contracts") $_.Name)
    }
}
Get-ChildItem $src -Filter "Contract*.cs" -ErrorAction SilentlyContinue | ForEach-Object {
    Move-File $_.FullName (Join-Path (Join-Path $unit "Contract") $_.Name)
}

if (Test-Path (Join-Path $src "Architecture")) {
    Get-ChildItem (Join-Path $src "Architecture") -Filter "*.cs" | ForEach-Object {
        Move-File $_.FullName (Join-Path (Join-Path $unit "Architecture") $_.Name)
    }
}
if (Test-Path (Join-Path $src "Observability")) {
    Get-ChildItem (Join-Path $src "Observability") -Filter "*.cs" | ForEach-Object {
        Move-File $_.FullName (Join-Path (Join-Path $unit "Platform") $_.Name)
    }
}

function AppSubfolder([string]$name) {
    if ($name -match "Order") { return "Application/Orders" }
    if ($name -match "Payment|LiqPay") { return "Application/Payments" }
    if ($name -match "Cart|Favorite|Checkout") { return "Application/Cart" }
    if ($name -match "Review") { return "Application/Reviews" }
    if ($name -match "Company|Member") { return "Application/Companies" }
    if ($name -match "Product|Catalog|Search") { return "Application/Products" }
    if ($name -match "Notification|Push|Restock") { return "Application/Notifications" }
    if ($name -match "Identity|Auth|AssignUser") { return "Application/IdentityAccess" }
    if ($name -match "Inventory") { return "Application/Inventory" }
    if ($name -match "Admin") { return "Application/Platform" }
    return "Application"
}

Get-ChildItem $src -Filter "*.cs" -File | ForEach-Object {
    $name = $_.Name
    if ($name.StartsWith("Application")) {
        $sub = AppSubfolder ($name -replace '\.cs$','')
        Move-File $_.FullName (Join-Path $unit "$sub/$name")
    }
    elseif ($name.StartsWith("Domain")) {
        Move-File $_.FullName (Join-Path $unit "Domain/$name")
    }
    elseif ($name.StartsWith("Infrastructure") -or $name.StartsWith("OutboxDispatcher")) {
        Move-File $_.FullName (Join-Path $unit "Infrastructure/$name")
    }
    elseif ($name.StartsWith("AuthValidators") -or $name.StartsWith("AssignUserRole")) {
        Move-File $_.FullName (Join-Path $unit "Application/IdentityAccess/$name")
    }
    elseif ($name.StartsWith("LiqPay")) {
        Move-File $_.FullName (Join-Path $unit "Application/Payments/$name")
    }
    elseif ($name.StartsWith("Email")) {
        Move-File $_.FullName (Join-Path $unit "Infrastructure/$name")
    }
}

Write-Host "Remaining files:"
Get-ChildItem $src -Recurse -File | ForEach-Object { $_.FullName }
