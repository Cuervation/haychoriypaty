using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HayChoriYPaty.Cloud
{
    /// <summary>Android Credential Manager bridge that returns only an in-memory Google ID token.</summary>
    [DisallowMultipleComponent]
    public sealed class GoogleCredentialProvider : MonoBehaviour
    {
        private const int TimeoutSeconds = 60;
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, TaskCompletionSource<string>> Pending = new Dictionary<string, TaskCompletionSource<string>>();
        private string activeRequestId;

        public Task<string> SignInAsync(string webClientId)
        {
#if UNITY_EDITOR || !UNITY_ANDROID
            throw new PlatformNotSupportedException("Google Credential Manager is available only in the Android player.");
#else
            if (string.IsNullOrWhiteSpace(webClientId)) throw new ArgumentException("A web OAuth client ID is required.", nameof(webClientId));
            Cancel();
            string requestId = Guid.NewGuid().ToString("N");
            var completion = new TaskCompletionSource<string>();
            lock (Sync) Pending.Add(requestId, completion);
            activeRequestId = requestId;
            try
            {
                using (var bridge = new AndroidJavaClass("com.haychoriypaty.auth.GoogleCredentialBridge"))
                    bridge.CallStatic("signIn", gameObject.name, requestId, webClientId, TimeoutSeconds);
            }
            catch (Exception error)
            {
                RemovePending(requestId);
                completion.TrySetException(new InvalidOperationException("google_credential_bridge_unavailable", error));
            }
            return completion.Task;
#endif
        }

        /// <summary>Stops the in-flight picker and completes its task as canceled.</summary>
        public void Cancel()
        {
            string requestId = activeRequestId;
            if (string.IsNullOrEmpty(requestId)) return;
            activeRequestId = null;
            TaskCompletionSource<string> completion = RemovePending(requestId);
            completion?.TrySetCanceled();
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var bridge = new AndroidJavaClass("com.haychoriypaty.auth.GoogleCredentialBridge"))
                    bridge.CallStatic("cancel", requestId);
            }
            catch { /* Cancellation is best-effort; never log credential or account data. */ }
#endif
        }

        /// <summary>Clears Credential Manager's remembered app credential state; it does not remove the Google account.</summary>
        public void SignOut()
        {
            Cancel();
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var bridge = new AndroidJavaClass("com.haychoriypaty.auth.GoogleCredentialBridge"))
                    bridge.CallStatic("clearCredentialState");
            }
            catch { /* Sign-out state cleanup is best-effort and contains no user-visible token data. */ }
#endif
        }

        // Called through UnitySendMessage. Payload format: requestId + newline + Base64(UTF-8 token).
        [UnityEngine.Scripting.Preserve]
        public void OnGoogleCredentialSuccess(string payload)
        {
            if (!TrySplit(payload, out string requestId, out string encoded)) return;
            TaskCompletionSource<string> completion = RemovePending(requestId);
            if (completion == null) return; // Ignore stale or canceled native callbacks.
            if (activeRequestId == requestId) activeRequestId = null;
            try { completion.TrySetResult(Encoding.UTF8.GetString(Convert.FromBase64String(encoded))); }
            catch { completion.TrySetException(new InvalidOperationException("google_credential_token_encoding_invalid")); }
        }

        // Called through UnitySendMessage. Error payloads contain only a fixed category code.
        [UnityEngine.Scripting.Preserve]
        public void OnGoogleCredentialFailure(string payload)
        {
            if (!TrySplit(payload, out string requestId, out string code)) return;
            TaskCompletionSource<string> completion = RemovePending(requestId);
            if (completion == null) return;
            if (activeRequestId == requestId) activeRequestId = null;
            if (code == "CANCELED") completion.TrySetCanceled();
            else if (code == "TIMEOUT") completion.TrySetException(new TimeoutException("Google sign-in timed out."));
            else completion.TrySetException(new InvalidOperationException("google_sign_in_failed:" + SafeCode(code)));
        }

        private static TaskCompletionSource<string> RemovePending(string requestId)
        {
            lock (Sync)
            {
                if (!Pending.TryGetValue(requestId, out TaskCompletionSource<string> completion)) return null;
                Pending.Remove(requestId);
                return completion;
            }
        }

        private static bool TrySplit(string payload, out string requestId, out string value)
        {
            requestId = value = null;
            if (string.IsNullOrEmpty(payload)) return false;
            int separator = payload.IndexOf('\n');
            if (separator <= 0 || separator == payload.Length - 1) return false;
            requestId = payload.Substring(0, separator);
            value = payload.Substring(separator + 1);
            return true;
        }

        private static string SafeCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return "UNKNOWN";
            var clean = new StringBuilder(Math.Min(code.Length, 48));
            for (int i = 0; i < code.Length && clean.Length < 48; i++)
            {
                char c = code[i];
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_') clean.Append(c);
            }
            return clean.Length == 0 ? "UNKNOWN" : clean.ToString();
        }

        private void OnDestroy() => Cancel();
    }
}
