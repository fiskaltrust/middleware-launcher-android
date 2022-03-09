param(
    [string]$FiskalyApiKey,
    [string]$FiskalyApiSecret,
    [string]$TssAdminPin,
    [string]$TssId)

# Create a new fiskaly v2 TSE
$baseUrl = "https://kassensichv-middleware.fiskaly.com/api/v2"

$authBody = @{
    "api_key"    = $FiskalyApiKey; 
    "api_secret" = $FiskalyApiSecret;
} | ConvertTo-Json
$headers = @{ "Content-Type" = "application/json" }
$authResponse = Invoke-WebRequest -Method POST -Uri "$baseUrl/auth" -Headers $headers -Body $authBody
if ($authResponse.StatusCode -ne 200) {
    throw "Could not authenticate"
}

$auth = $authResponse | ConvertFrom-Json
$defaultHeaders = @{
    "Authorization"  = "Bearer $($auth.access_token)";
    "Content-Type"   = "application/json"
}

$adminAuthBody = @{
    "admin_pin"    = $TssAdminPin;
} | ConvertTo-Json
$adminAuthResponse = Invoke-WebRequest -Headers $defaultHeaders -Method POST -Uri "$baseUrl/tss/$TssId/admin/auth" -Body $adminAuthBody
if ($adminAuthResponse.StatusCode -ne 200) {
    throw "Could not authenticate admin"
}

$disableTssBody = @{
    "state"    = "DISABLED";
} | ConvertTo-Json
$disableTssResponse = Invoke-WebRequest -Headers $defaultHeaders -Method PATCH -Uri "$baseUrl/tss/$TssId" -Body $disableTssBody
if ($disableTssResponse.StatusCode -ne 200) {
    throw "Could not disable TSS"
}
