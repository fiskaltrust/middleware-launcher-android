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

$tssResponse = Invoke-WebRequest -Headers $defaultHeaders -Method PUT -Uri "$baseUrl/tss/$TssId" -Body "{}"
if ($tssResponse.StatusCode -ne 200) {
    throw "Could not create TSS"
}

$tss = $tssResponse | ConvertFrom-Json

$updateStateBody = @{
    "state" = "UNINITIALIZED";
} | ConvertTo-Json

$updateStateResponse = Invoke-WebRequest -Headers $defaultHeaders -Method PATCH -Uri "$baseUrl/tss/$TssId" -Body $updateStateBody
if ($updateStateResponse.StatusCode -ne 200) {
    throw "Could not update TSS state"
}

$updatePinBody = @{
    "admin_puk"     = $tss.admin_puk;
    "new_admin_pin" = "$TssAdminPin";
} | ConvertTo-Json

$updatePinResponse = Invoke-WebRequest -Headers $defaultHeaders -Method PATCH -Uri "$baseUrl/tss/$TssId/admin" -Body $updatePinBody
if ($updatePinResponse.StatusCode -ne 200) {
    throw "Could not set admin_pin"
}
