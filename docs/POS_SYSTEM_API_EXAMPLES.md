# POS System API Intent Integration Examples

This directory contains examples for integrating with the fiskaltrust.Middleware PosSystemAPI using Android Intents.

## Overview

The PosSystemAPI Activity (`eu.fiskaltrust.androidlauncher.PosSystemAPI`) allows Android applications to communicate with the fiskaltrust.Middleware through Android Intents. This enables offline fiscalization capabilities without requiring direct HTTP communication.

## Basic Integration

### 1. Kotlin Example

```kotlin
import android.content.Intent
import android.util.Base64
import android.util.Log
import androidx.appcompat.app.AppCompatActivity
import org.json.JSONObject
import java.util.UUID

class PosIntegrationActivity : AppCompatActivity() {
    
    companion object {
        private const val REQUEST_CODE_POS_API = 1001
        private const val TAG = "PosIntegration"
    }

    /**
     * Helper function to call the PosSystemAPI
     */
    private fun callPosSystemAPI(
        method: String,
        path: String,
        headers: Map<String, String>,
        body: String? = null
    ) {
        try {
            // Create headers JSON
            val headersJson = JSONObject(headers).toString()
            
            // Base64URL encode (no padding, replace + with -, / with _)
            val headerB64 = headersJson.toByteArray(Charsets.UTF_8)
                .let { Base64.encodeToString(it, Base64.URL_SAFE or Base64.NO_PADDING or Base64.NO_WRAP) }
            
            val bodyB64 = body?.toByteArray(Charsets.UTF_8)
                ?.let { Base64.encodeToString(it, Base64.URL_SAFE or Base64.NO_PADDING or Base64.NO_WRAP) }
            
            // Create intent
            val intent = Intent()
            intent.setClassName(
                "eu.fiskaltrust.androidlauncher",
                "eu.fiskaltrust.androidlauncher.PosSystemAPI"
            )
            intent.putExtra("Method", method)
            intent.putExtra("Path", path)
            intent.putExtra("HeaderJsonObjectBase64Url", headerB64)
            if (bodyB64 != null) {
                intent.putExtra("BodyBase64Url", bodyB64)
            }
            
            startActivityForResult(intent, REQUEST_CODE_POS_API)
        } catch (e: Exception) {
            Log.e(TAG, "Error creating intent", e)
        }
    }

    /**
     * Example: Echo endpoint
     */
    fun exampleEcho() {
        val headers = mapOf(
            "Accept" to "application/json",
            "Content-Type" to "application/json",
            "x-cashbox-id" to "de12c75f-5587-48b8-8ac5-64b7c81a05ec",
            "x-cashbox-accesstoken" to "your-access-token",
            "x-operation-id" to UUID.randomUUID().toString()
        )
        val body = """{"Message": "Hello, World!"}"""
        
        callPosSystemAPI("POST", "/echo", headers, body)
    }

    override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
        super.onActivityResult(requestCode, resultCode, data)
        
        if (requestCode == REQUEST_CODE_POS_API && data != null) {
            val statusCode = data.getStringExtra("StatusCode") ?: "500"
            val contentB64 = data.getStringExtra("ContentBase64Url") ?: ""
            val contentTypeB64 = data.getStringExtra("ContentTypeBase64Url") ?: ""
            
            try {
                // Decode Base64URL
                val content = Base64.decode(contentB64, Base64.URL_SAFE or Base64.NO_PADDING or Base64.NO_WRAP)
                    .toString(Charsets.UTF_8)
                val contentType = Base64.decode(contentTypeB64, Base64.URL_SAFE or Base64.NO_PADDING or Base64.NO_WRAP)
                    .toString(Charsets.UTF_8)
                
                Log.i(TAG, "Status: $statusCode, Type: $contentType")
                Log.i(TAG, "Response: $content")
                
                // Parse JSON response
                val response = JSONObject(content)
                
                when (statusCode.toIntOrNull()) {
                    200, 201 -> {
                        // Success
                        Log.i(TAG, "Operation successful")
                    }
                    400 -> {
                        // Bad request
                        val detail = response.optString("detail", "Unknown error")
                        Log.e(TAG, "Bad request: $detail")
                    }
                    401 -> {
                        // Unauthorized
                        Log.e(TAG, "Unauthorized - check credentials")
                    }
                    500 -> {
                        // Internal server error
                        val detail = response.optString("detail", "Unknown error")
                        Log.e(TAG, "Server error: $detail")
                    }
                }
            } catch (e: Exception) {
                Log.e(TAG, "Error decoding response", e)
            }
        }
    }
}
```

## Additional Resources

- [fiskaltrust PosSystemAPI Documentation](https://docs.fiskaltrust.eu/apis/pos-system-api)
- [fiskaltrust SwaggerHub API Spec](https://app.swaggerhub.com/apis/fiskaltrust/fiskaltrust-possystem-api/2.1)
- [fiskaltrust Portal](https://portal.fiskaltrust.eu)
- [Middleware Documentation](https://docs.fiskaltrust.cloud)
