param(
    [string]$ApiUrl = "http://localhost:8080",
    [string]$FrontendUrl = "http://localhost:3000"
)
$ErrorActionPreference = "Stop"
$liveness = Invoke-RestMethod "$ApiUrl/api/health"
if ($liveness.status -ne "Healthy") { throw "API liveness failed." }
$readiness = Invoke-RestMethod "$ApiUrl/api/health/ready"
if ($readiness.status -ne "Healthy" -or $readiness.checks.database -ne "Healthy") {
    throw "Database readiness failed."
}
$response = Invoke-WebRequest "$ApiUrl/api/health/ready" -Headers @{ Origin = $FrontendUrl }
if ($response.Headers["Access-Control-Allow-Origin"] -notcontains $FrontendUrl) {
    throw "Frontend origin is not allowed by the API."
}
$page = Invoke-WebRequest $FrontendUrl
if ($page.StatusCode -ne 200 -or $page.Content -notmatch "A simpler daily commute") {
    throw "Frontend landing page failed."
}
Write-Host "PASS: Frontend HTML, API liveness, database readiness, and frontend CORS."
