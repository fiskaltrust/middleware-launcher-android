param(
  [string]$AccountId,
  [string]$AccountAccessToken,
  [string]$TemplatePath,
  [string]$FiskalyApiKey,
  [string]$FiskalyApiSecret,
  [string]$TssId)

$template = Get-Content $TemplatePath -Raw
$templateJson = $template | ConvertFrom-Json

$uri = "https://helipad-sandbox.fiskaltrust.cloud/api/configuration?description=delete-launcher-smoketests-$($env:BUILD_BUILDID)&fyapikey=$([uri]::EscapeDataString($FiskalyApiKey))&fyapisecret=$([uri]::EscapeDataString($FiskalyApiSecret))&fytssid=$([uri]::EscapeDataString($TssId))"
$headers = @{ accountid = $AccountId; accesstoken = $AccountAccessToken }

$response = Invoke-WebRequest -uri $uri -Headers $headers -Method POST -ContentType "application/json; charset=utf-8" -Body $($template | ConvertTo-Json) | ConvertFrom-Json

echo $($response | ConvertTo-Json)

echo "##vso[task.setvariable variable=cashboxId;isOutput=true]$($response.cashboxid)"
echo "##vso[task.setvariable variable=accessToken;isOutput=true]$($response.accesstoken)"
echo "##vso[task.setvariable variable=url;isOutput=true]$($templateJson.ftQueues[0].Url[0])"