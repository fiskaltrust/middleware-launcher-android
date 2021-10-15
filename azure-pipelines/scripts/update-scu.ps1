param(
    [string]$AccountId,
    [string]$AccountAccessToken,
    [string]$ScuId,
    [string]$FiskalyApiKey,
    [string]$FiskalyApiSecret,
    [string]$TssAdminPin,
    [string]$TssId)

# Update SCU
$helipadUrl = "https://helipad-sandbox.fiskaltrust.cloud"
$helipadHeaders = @{"accountid" = $AccountId; "accesstoken" = $AccountAccessToken; "Content-Type" = "application/json" }

$scuResponse = Invoke-WebRequest -Method GET -Uri "$helipadUrl/api/SignaturCreationUnitDE/$ScuId" -Headers $helipadHeaders
$scu = $scuResponse | ConvertFrom-Json

$configuration = $scu.configuration -replace '\\"', '"' | ConvertFrom-Json
$configuration.ApiKey = $FiskalyApiKey
$configuration.ApiSecret = $FiskalyApiSecret
$configuration.TssId = $TssId
$configuration.AdminPin = $TssAdminPin

$updateScuBody = @{
    "ftSignaturCreationUnitDEId" = $scu.ftSignaturCreationUnitDEId;
    "configuration"              = "$(($configuration | ConvertTo-Json -Compress))";
    "description"                = $scu.description;
    "packageName"                = $scu.packageName;
    "packageVersion"             = $scu.packageVersion;
    "posOperatorId"              = $scu.posOperatorId;
    "url"                        = $scu.url;
    "timeStamp"                  = (Get-Date).Ticks;
    "modeConfigurationJson"      = $scu.modeConfigurationJson;
    "mode"                       = $scu.mode;
} | ConvertTo-Json
$updateScuResponse = Invoke-WebRequest -Method PUT -Uri "$helipadUrl/api/SignaturCreationUnitDE" -Headers $helipadHeaders -Body $updateScuBody
if ($updateScuResponse.StatusCode -ne 200) {
    throw "Could not update SCU"
}

