param(
  [string]$AccountId,
  [string]$AccountAccessToken,
  [string]$TemplatePath)

$template = Get-Content $TemplatePath -Raw
$templateJson = $template | ConvertFrom-Json

$uri = "https://helipad-sandbox.fiskaltrust.cloud/api/configuration"
$headers = @{ accountid = $AccountId ; accesstoken = $AccountAccessToken }

$response = Invoke-WebRequest -uri $uri -Headers $headers -Method POST -ContentType "application/json" -Body $($template | ConvertTo-Json) | ConvertFrom-Json

echo $($response | ConvertTo-Json)

echo "##vso[task.setvariable variable=cashboxId;isOutput=true]$($response.cashboxid)"
echo "##vso[task.setvariable variable=accessToken;isOutput=true]$($response.accesstoken)"
echo "##vso[task.setvariable variable=url;isOutput=true]$($templateJson.ftQueues[0].Url[0])"