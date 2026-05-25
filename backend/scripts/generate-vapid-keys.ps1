# Generates a VAPID key pair for Web Push (requires Node.js + npx).
# Copy keys into backend/.env as WEBPUSH__PUBLICKEY / WEBPUSH__PRIVATEKEY; set WEBPUSH__ENABLED=true.
# Do not commit the private key. Verify: GET /web-push/vapid-public-key after restarting the API.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "Running: npx --yes web-push generate-vapid-keys" -ForegroundColor Cyan
npx --yes web-push generate-vapid-keys

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Copy Public Key  -> WEBPUSH__PUBLICKEY in backend/.env"
Write-Host "  2. Copy Private Key -> WEBPUSH__PRIVATEKEY in backend/.env"
Write-Host "  3. Set WEBPUSH__ENABLED=true (and WEBPUSH__SUBJECT for production, e.g. mailto:ops@yourdomain)"
Write-Host "  4. Restart API; call GET /web-push/vapid-public-key to confirm publicKey and subject."
