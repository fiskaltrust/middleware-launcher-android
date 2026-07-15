using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using fiskaltrust.AndroidLauncher.Extensions;
using fiskaltrust.AndroidLauncher.Services;
using fiskaltrust.Api.PosSystem.Core.Models;

namespace fiskaltrust.AndroidLauncher.Activitites
{
    /// <summary>
    /// Activity that handles Intent-based POS System API calls.
    /// Routes /sign and /echo to local middleware, other endpoints to cloud PosSystemAPI.
    /// </summary>
    [Activity(
        Label = "PosSystemAPI",
        Name = "eu.fiskaltrust.androidlauncher.PosSystemAPI",
        Enabled = true,
        Exported = true)]
    public class PosSystemAPIActivity : Activity
    {
        private const string TAG = "PosSystemAPI";
        private readonly PosSystemApiRequestHandler _requestHandler = new PosSystemApiRequestHandler();

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Log.Info(TAG, "PosSystemAPI Activity started");

            // Process the intent asynchronously
            Task.Run(ProcessIntentAsync);
        }

        private async Task ProcessIntentAsync()
        {
            PosSystemApiResponse response;
            try
            {
                var intent = Intent;
                if (intent == null)
                {
                    Log.Error(TAG, "Intent is null");
                    response = PosSystemApiResponse.Error(500, "Intent is null");
                    FinishWithResponse(response);
                    return;
                }

                // Parse the intent into a DTO
                PosSystemApiRequest request;

                try
                {
                    request = PosSystemApiRequestExtensions.FromIntent(intent);
                    Log.Info(TAG, $"Processing request: {request.Method} {request.Path}");
                }
                catch (ArgumentException ex)
                {
                    Log.Error(TAG, $"Invalid request: {ex.Message}");
                    response = PosSystemApiResponse.Error(400, ex.Message);
                    FinishWithResponse(response);
                    return;
                }

                response = await _requestHandler.HandleAsync(request).ConfigureAwait(false);
                FinishWithResponse(response);
            }
            catch (InvalidOperationException ex)
            {
                Log.Error(TAG, $"Request processing failed: {ex}");
                response = PosSystemApiResponse.Error(500, $"Internal error: {ex.Message}");
                FinishWithResponse(response);
            }
            catch (TimeoutException ex)
            {
                Log.Error(TAG, $"Request processing timeout: {ex}");
                response = PosSystemApiResponse.Error(500, $"Internal error: {ex.Message}");
                FinishWithResponse(response);
            }
        }

        /// <summary>
        /// Finishes the activity with a structured response using the DTO
        /// </summary>
        /// <param name="response">The PosSystemApiResponse to return</param>
        private void FinishWithResponse(PosSystemApiResponse response)
        {
            RunOnUiThread(() =>
            {
                try
                {
                    var intentData = response.ToIntentData();
                    var resultIntent = new Intent();

                    resultIntent.PutExtra(PosSystemAPIActivityIntentStatics.EXTRA_STATUS_CODE, intentData.StatusCode);
                    resultIntent.PutExtra(PosSystemAPIActivityIntentStatics.EXTRA_CONTENT_BASE64URL, intentData.ContentBase64Url);
                    resultIntent.PutExtra(PosSystemAPIActivityIntentStatics.EXTRA_CONTENT_TYPE_BASE64URL, intentData.ContentTypeBase64Url);

                    if (!string.IsNullOrEmpty(intentData.HeadersBase64Url))
                    {
                        resultIntent.PutExtra(PosSystemAPIActivityIntentStatics.EXTRA_RESPONSE_HEADER_JSON_BASE64URL, intentData.HeadersBase64Url);
                    }

                    SetResult(Result.Ok, resultIntent);
                    var contentForLog = response.Content is ResponseBody.Text t ? t.Value : "(binary)";
                    Log.Info(TAG, $"Finishing with response: {response.StatusCode} - {(response.IsSuccess ? "Success" : "Error")} - {contentForLog}");
                }
                catch (InvalidOperationException ex)
                {
                    Log.Error(TAG, $"Failed to set response result: {ex}");
                }
                catch (ArgumentException ex)
                {
                    Log.Error(TAG, $"Failed to set response result: {ex}");
                }
                finally
                {
                    Finish();
                }
            });
        }

    }
}
