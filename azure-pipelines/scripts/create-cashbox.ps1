param(
  [string]$AccountId,
  [string]$AccountAccessToken,
  [string]$TemplatePath,
  [string]$FiskalyApiKey,
  [string]$FiskalyApiSecret,
  [string]$TssAdminPin,
  [string]$TssId)

$template = [string](Get-Content $TemplatePath -Raw)

$uri = "https://helipad-sandbox.fiskaltrust.cloud/api/configuration?description=delete-launcher-smoketests-$($env:BUILD_BUILDID)&fyapikey=$([uri]::EscapeDataString($FiskalyApiKey))&fyapisecret=$([uri]::EscapeDataString($FiskalyApiSecret))&fytssid=$([uri]::EscapeDataString($TssId))&fyadminpin=$([uri]::EscapeDataString($TssAdminPin))"
$headers = @{ accountid = $AccountId; accesstoken = $AccountAccessToken }

echo "template:"
echo $TemplatePath
echo $template
echo $($template | ConvertTo-Json)

$response = Invoke-WebRequest -uri $uri -Headers $headers -Method POST -ContentType "application/json; charset=utf-8" -Body $($template | ConvertTo-Json).value | ConvertFrom-Json

echo "##vso[task.setvariable variable=cashboxId;isOutput=true]$($response.cashboxid)"
echo "##vso[task.setvariable variable=accessToken;isOutput=true]$($response.accesstoken)"
echo "##vso[task.setvariable variable=url;isOutput=true]$($($response.configuration | ConvertFrom-Json).ftQueues[0].Url[0])"