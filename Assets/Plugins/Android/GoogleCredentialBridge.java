package com.haychoriypaty.auth;

import android.app.Activity;
import android.content.MutableContextWrapper;
import android.os.CancellationSignal;
import android.os.Handler;
import android.os.Looper;

import androidx.annotation.NonNull;
import androidx.credentials.ClearCredentialStateRequest;
import androidx.credentials.Credential;
import androidx.credentials.CredentialManager;
import androidx.credentials.CustomCredential;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.ClearCredentialException;
import androidx.credentials.exceptions.GetCredentialException;

import com.google.android.libraries.identity.googleid.GetSignInWithGoogleOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;
import com.unity3d.player.UnityPlayer;

import java.nio.charset.StandardCharsets;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.Executor;

/** Minimal Android Credential Manager bridge; ID tokens stay out of logs and persistent storage. */
public final class GoogleCredentialBridge {
    private static final Handler MAIN = new Handler(Looper.getMainLooper());
    private static final Executor MAIN_EXECUTOR = command -> MAIN.post(command);
    private static final Map<String, PendingRequest> REQUESTS = new ConcurrentHashMap<>();

    private static final class PendingRequest {
        final String objectName;
        final CancellationSignal cancellation = new CancellationSignal();
        Runnable timeout;

        PendingRequest(String objectName) { this.objectName = objectName; }
    }

    private GoogleCredentialBridge() { }

    public static void signIn(String objectName, String requestId, String webClientId, int timeoutSeconds) {
        Activity activity = UnityPlayer.currentActivity;
        if (activity == null || objectName == null || requestId == null || webClientId == null || webClientId.trim().isEmpty()) {
            sendFailure(objectName, requestId, "INVALID_REQUEST");
            return;
        }
        PendingRequest pending = new PendingRequest(objectName);
        PendingRequest prior = REQUESTS.put(requestId, pending);
        if (prior != null) prior.cancellation.cancel();
        pending.timeout = () -> finishFailure(requestId, "TIMEOUT", true);
        MAIN.postDelayed(pending.timeout, Math.max(1, timeoutSeconds) * 1000L);
        MAIN.post(() -> startSignIn(activity, requestId, webClientId, pending));
    }

    private static void startSignIn(Activity activity, String requestId, String webClientId, PendingRequest pending) {
        if (REQUESTS.get(requestId) != pending) return;
        try {
            GetSignInWithGoogleOption option = new GetSignInWithGoogleOption.Builder(webClientId).build();
            GetCredentialRequest request = new GetCredentialRequest.Builder().addCredentialOption(option).build();
            CredentialManager manager = CredentialManager.create(activity);
            MutableContextWrapper context = new MutableContextWrapper(activity);
            manager.getCredentialAsync(context, request, pending.cancellation, MAIN_EXECUTOR,
                new androidx.credentials.CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                    @Override public void onResult(@NonNull GetCredentialResponse response) {
                        Credential credential = response.getCredential();
                        if (!(credential instanceof CustomCredential) ||
                            !GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL.equals(((CustomCredential) credential).getType())) {
                            finishFailure(requestId, "UNEXPECTED_CREDENTIAL", false);
                            return;
                        }
                        try {
                            GoogleIdTokenCredential google = GoogleIdTokenCredential.createFrom(((CustomCredential) credential).getData());
                            finishSuccess(requestId, google.getIdToken());
                        } catch (RuntimeException error) {
                            finishFailure(requestId, "TOKEN_PARSE", false);
                        }
                    }

                    @Override public void onError(@NonNull GetCredentialException error) {
                        finishFailure(requestId, error.getClass().getSimpleName(), false);
                    }
                });
        } catch (RuntimeException error) {
            finishFailure(requestId, "REQUEST_FAILED", false);
        }
    }

    public static void cancel(String requestId) {
        finishFailure(requestId, "CANCELED", true);
    }

    public static void clearCredentialState() {
        MAIN.post(() -> {
            Activity activity = UnityPlayer.currentActivity;
            if (activity == null) return;
            try {
                CredentialManager.create(activity).clearCredentialStateAsync(
                    new ClearCredentialStateRequest(ClearCredentialStateRequest.TYPE_CLEAR_CREDENTIAL_STATE), null, MAIN_EXECUTOR,
                    new androidx.credentials.CredentialManagerCallback<Void, ClearCredentialException>() {
                        @Override public void onResult(Void ignored) { }
                        @Override public void onError(@NonNull ClearCredentialException error) { }
                    });
            } catch (RuntimeException ignored) { }
        });
    }

    private static void finishSuccess(String requestId, String token) {
        PendingRequest pending = remove(requestId);
        if (pending == null || token == null || token.isEmpty()) return;
        String encoded = android.util.Base64.encodeToString(token.getBytes(StandardCharsets.UTF_8), android.util.Base64.NO_WRAP);
        UnityPlayer.UnitySendMessage(pending.objectName, "OnGoogleCredentialSuccess", requestId + "\n" + encoded);
    }

    private static void finishFailure(String requestId, String code, boolean cancelSignal) {
        PendingRequest pending = remove(requestId);
        if (pending == null) return;
        if (cancelSignal) pending.cancellation.cancel();
        sendFailure(pending.objectName, requestId, safeErrorCode(code));
    }

    private static PendingRequest remove(String requestId) {
        if (requestId == null) return null;
        PendingRequest pending = REQUESTS.remove(requestId);
        if (pending != null && pending.timeout != null) MAIN.removeCallbacks(pending.timeout);
        return pending;
    }

    private static void sendFailure(String objectName, String requestId, String code) {
        if (objectName != null && requestId != null)
            UnityPlayer.UnitySendMessage(objectName, "OnGoogleCredentialFailure", requestId + "\n" + code);
    }

    private static String safeErrorCode(String code) {
        if (code == null || code.isEmpty()) return "UNKNOWN";
        StringBuilder clean = new StringBuilder(48);
        for (int i = 0; i < code.length() && clean.length() < 48; i++) {
            char c = code.charAt(i);
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_') clean.append(c);
        }
        return clean.length() == 0 ? "UNKNOWN" : clean.toString();
    }
}
