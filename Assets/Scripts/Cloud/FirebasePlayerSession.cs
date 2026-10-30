using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

namespace HayChoriYPaty.Cloud
{
    public enum PlayerSessionState { Initializing, LoginRequired, Authenticating, Recovering, LegacyConsent, Ready, Conflict, Error }
    [Serializable] public sealed class PlayerFirebaseConfig { public string projectId, appId, apiKey, webClientId; }

    /// <summary>Auth is a hard lifecycle gate, not just a hidden play button. No guest path exists.</summary>
    public sealed class FirebasePlayerSession : MonoBehaviour
    {
        public const string ProjectId = "haychoriypaty";
        public static FirebasePlayerSession Instance { get; private set; }
        public PlayerSessionState State { get; private set; } = PlayerSessionState.Initializing;
        public bool AccountMenuOpen => accountOpen;
        public bool BlocksGameInput
        {
            get
            {
#if UNITY_EDITOR
                if (StreetGame.AllowEditorTestInitialization) return false;
#endif
                return State != PlayerSessionState.Ready || accountOpen;
            }
        }
        public string ProgressSyncStatus
        {
            get
            {
                if (State != PlayerSessionState.Ready) return "Esperando sesión de Google";
                if (offline) return "Sin conexión · progreso pendiente";
                if (syncing || local == null || local.State.pending.Count > 0 || local.State.acknowledgedRevision < 1)
                    return "Pendiente de sincronizar con Firebase";
                return "Sincronizado con Firebase";
            }
        }
        private string message = "Inicializando…", root, legacyJson, legacyHash;
        private PlayerFirebaseConfig config;
        private FirebaseApp app;
        private FirebaseAuth auth;
        private FirebaseFirestore firestore;
        private GoogleCredentialProvider google;
        private PlayerLocalRepository local;
        private FirestorePlayerRepository remote;
        private RemotePlayerState conflictRemote;
        private StreetGame game;
        private Texture2D googleButton;
        private int generation;
        private bool syncing, offline, accountOpen, initialSyncWaiting;
        private float retryAt, retryDelay = 2;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
#if UNITY_EDITOR
            if (StreetGame.AllowEditorTestInitialization) return;
#endif
            if (Instance != null) return;
            var host = new GameObject("FirebasePlayerSession"); DontDestroyOnLoad(host); host.AddComponent<FirebasePlayerSession>();
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            root = Path.Combine(Application.persistentDataPath, "PlayerCloud");
            google = gameObject.AddComponent<GoogleCredentialProvider>();
            googleButton = Resources.Load<Texture2D>("Google/google-g");
        }
        private async void Start() { await BootstrapAsync(); }
        private void OnDestroy()
        {
            generation++;
            if (auth != null) auth.StateChanged -= OnAuthChanged;
            if (Instance == this) Instance = null;
        }
        private void OnAuthChanged(object sender, EventArgs args)
        {
            if (local != null && (auth.CurrentUser == null || auth.CurrentUser.UserId != local.State.uid))
            {
                generation++; game?.SuspendPlayer();
                State = PlayerSessionState.LoginRequired; message = "Volvé a iniciar sesión con Google.";
                accountOpen = true;
                FindAnyObjectByType<StreetView>()?.ReturnToHubAfterAuthenticationRequired();
            }
        }
        private async Task BootstrapAsync()
        {
            int ticket = ++generation;
            initialSyncWaiting = false;
            State = PlayerSessionState.Initializing; message = "Inicializando…";
            try
            {
                if (game != null && !game.SuspendPlayer()) throw new IOException("Previous account snapshot not durable");
                // Legacy bytes are backed up before the game can initialize or repair anything.
                legacyJson = PlayerPrefs.HasKey(StreetGame.ProgressKey) ? PlayerPrefs.GetString(StreetGame.ProgressKey) : null;
                legacyHash = PlayerLocalRepository.BackupLegacy(root, legacyJson);
                var asset = Resources.Load<TextAsset>("player-firebase-config");
                if (asset == null) throw new InvalidOperationException("Firebase configuration is not installed");
                config = JsonUtility.FromJson<PlayerFirebaseConfig>(asset.text);
                if (config == null || config.projectId != ProjectId || string.IsNullOrEmpty(config.appId) ||
                    string.IsNullOrEmpty(config.apiKey) || string.IsNullOrEmpty(config.webClientId))
                    throw new InvalidOperationException("Invalid Firebase target/OAuth configuration");
                var dependency = await Within(FirebaseApp.CheckAndFixDependenciesAsync());
                if (ticket != generation || this == null) return;
                if (dependency != DependencyStatus.Available) throw new InvalidOperationException("Firebase dependencies unavailable");
                if (app == null) app = FirebaseApp.GetInstance("HayChoriYPatyPlayer") ?? FirebaseApp.Create(
                    new AppOptions { ProjectId = config.projectId, AppId = config.appId, ApiKey = config.apiKey }, "HayChoriYPatyPlayer");
                if (app.Options.ProjectId != ProjectId) throw new InvalidOperationException("Firebase project mismatch");
                if (auth == null) { auth = FirebaseAuth.GetAuth(app); auth.StateChanged += OnAuthChanged; }
                firestore = FirebaseFirestore.GetInstance(app);
                if (IsGoogleUser(auth.CurrentUser)) await ResolvePlayerAsync(ticket);
                else { State = PlayerSessionState.LoginRequired; message = ""; }
            }
            catch (Exception) { if (ticket == generation) Fail("No pudimos preparar el acceso. Tu progreso está protegido. Reintentá."); }
        }
        private static bool IsGoogleUser(FirebaseUser user) => user != null && !user.IsAnonymous && user.ProviderData.Any(p => p.ProviderId == "google.com");
        public async void SignIn()
        {
            if (auth == null || State == PlayerSessionState.Authenticating) return;
            int ticket = ++generation; State = PlayerSessionState.Authenticating; message = "Iniciando sesión con Google…";
            try
            {
                string idToken = await Within(google.SignInAsync(config.webClientId), 65);
                if (ticket != generation) return;
                await Within(auth.SignInWithCredentialAsync(GoogleAuthProvider.GetCredential(idToken, null)));
                idToken = null;
                if (ticket != generation) return;
                if (!IsGoogleUser(auth.CurrentUser)) throw new InvalidOperationException("Google provider required");
                await ResolvePlayerAsync(ticket);
                if (ticket == generation && IsGoogleUser(auth.CurrentUser)) ContinueToGameAfterGoogleSignIn();
            }
            catch (Exception) { google.Cancel(); if (ticket == generation) { State = PlayerSessionState.LoginRequired; message = "No se pudo iniciar sesión. Podés volver a intentar."; } }
        }
        private async Task ResolvePlayerAsync(int ticket)
        {
            State = PlayerSessionState.Recovering; message = "Recuperando tu progreso…";
            string uid = auth.CurrentUser.UserId;
            local = new PlayerLocalRepository(root, uid); remote = new FirestorePlayerRepository(firestore, uid);
            RemotePlayerState cloud;
            try { cloud = await Within(remote.ReadAsync()); }
            catch (Exception error)
            {
                // Only a previously owned durable profile is available offline, never an unowned legacy save.
                if (ticket == generation && local.State.snapshot != null && IsTransient(error))
                { Activate(local.State.snapshot); offline = true; return; }
                throw;
            }
            if (ticket != generation || !IsGoogleUser(auth.CurrentUser) || auth.CurrentUser.UserId != uid) return;
            if (local.State.snapshot != null)
            {
                if (local.State.pending.Count > 0)
                {
                    if (cloud != null && cloud.revision != local.State.acknowledgedRevision &&
                        !(cloud.revision == local.State.pending[0].expectedRevision + 1 && cloud.operationId == local.State.pending[0].id))
                    { ShowConflict(cloud); return; }
                    Activate(local.State.snapshot); retryAt = 0; SyncPending(); return;
                }
                if (cloud == null) throw new InvalidOperationException("Previously acknowledged cloud profile missing");
                if (cloud.revision < local.State.acknowledgedRevision ||
                    (cloud.revision == local.State.acknowledgedRevision && cloud.snapshot != local.State.snapshot))
                { ShowConflict(cloud); return; }
                local.AdoptRemote(cloud.snapshot, cloud.revision, false); Activate(local.State.snapshot); return;
            }
            bool unownedLegacy = legacyJson != null && PlayerLocalRepository.CanClaimLegacy(root, uid);
            if (cloud != null)
            {
                if (unownedLegacy) { conflictRemote = cloud; State = PlayerSessionState.Conflict; message = "Esta cuenta y este teléfono tienen progresos distintos. No los mezclamos. Podés usar la nube conservando una copia de este teléfono."; return; }
                local.AdoptRemote(cloud.snapshot, cloud.revision, false); Activate(local.State.snapshot); return;
            }
            if (unownedLegacy)
            { State = PlayerSessionState.LegacyConsent; message = "¿Vincular el progreso de este teléfono a la cuenta Google elegida? Conservaremos una copia de recuperación."; return; }
            Activate(null); // New profile only after a real authenticated server read proves cloud is empty.
            SyncPending();
        }
        private void Activate(string snapshot)
        {
            game = FindAnyObjectByType<StreetGame>();
            var ownerRepository = local; // A stale callback may only ever write its original UID's local envelope.
            if (game == null || !game.ActivatePlayer(snapshot, (json, match) => ownerRepository.Commit(json, match)))
                throw new InvalidOperationException("Cannot safely activate player data");
            initialSyncWaiting = ownerRepository.State.acknowledgedRevision == 0;
            State = initialSyncWaiting ? PlayerSessionState.Recovering : PlayerSessionState.Ready;
            message = initialSyncWaiting ? "Confirmando la primera sincronización de tu progreso…" : "";
            offline = false; retryDelay = 2; retryAt = 0;
        }
        private void ContinueToGameAfterGoogleSignIn()
        {
            if (game == null || !game.IsPlayerActive ||
                (State != PlayerSessionState.Ready && State != PlayerSessionState.Recovering)) return;
            accountOpen = false;
            FindAnyObjectByType<StreetView>()?.ContinueToGameAfterGoogleSignIn();
        }

        public void ConfirmLegacyImport()
        {
            if (State != PlayerSessionState.LegacyConsent || !IsGoogleUser(auth.CurrentUser)) return;
            try
            {
                PlayerLocalRepository.ValidateSnapshot(legacyJson);
                PlayerLocalRepository.ClaimLegacy(root, auth.CurrentUser.UserId);
                local.SetLegacySource(legacyHash); Activate(legacyJson); SyncPending(); ContinueToGameAfterGoogleSignIn();
            }
            catch (Exception) { Fail("No pudimos vincular la partida. La copia original sigue protegida."); }
        }
        public void UseRemotePreservingLocal()
        {
            if (State != PlayerSessionState.Conflict || conflictRemote == null) return;
            try
            {
                if (game != null && game.IsPlayerActive && !game.SuspendPlayer()) throw new IOException();
                if (legacyJson != null && PlayerLocalRepository.CanClaimLegacy(root, local.State.uid))
                    PlayerLocalRepository.ClaimLegacy(root, local.State.uid);
                local.AdoptRemote(conflictRemote.snapshot, conflictRemote.revision, true); Activate(local.State.snapshot);
                conflictRemote = null; ContinueToGameAfterGoogleSignIn();
            }
            catch (Exception) { Fail("No pudimos cambiar de progreso con seguridad. Conservamos ambas copias."); }
        }
        private void ShowConflict(RemotePlayerState cloud)
        {
            game?.SuspendPlayer(); conflictRemote = cloud;
            State = PlayerSessionState.Conflict;
            message = "Se jugó en otro dispositivo. Guardamos tu progreso de este teléfono por separado; no sumamos monedas ni mercadería. Podés cargar el progreso de la nube conservando esta copia.";
        }
        private void Update()
        {
            if ((State == PlayerSessionState.Ready || (initialSyncWaiting && State == PlayerSessionState.Recovering)) &&
                local != null && local.State.pending.Count > 0 && Time.unscaledTime >= retryAt)
                SyncPending();
        }
        private async void SyncPending()
        {
            if (syncing || (State != PlayerSessionState.Ready && !(initialSyncWaiting && State == PlayerSessionState.Recovering)) ||
                !IsGoogleUser(auth?.CurrentUser)) return;
            syncing = true; int ticket = generation;
            var repository = local; var server = remote;
            try
            {
                for (int i = 0; i < 8 && repository.State.pending.Count > 0; i++)
                {
                    var op = repository.State.pending[0];
                    long revision = await Within(server.CommitAsync(op));
                    if (ticket != generation || this == null) return;
                    repository.Acknowledge(op.id, revision); offline = false; retryDelay = 2;
                    if (initialSyncWaiting && repository.State.acknowledgedRevision > 0)
                    { initialSyncWaiting = false; State = PlayerSessionState.Ready; message = ""; }
                }
                retryAt = Time.unscaledTime + 1;
            }
            catch (Exception error)
            {
                if (ticket != generation || this == null) return;
                if (ContainsConflict(error))
                {
                    try { var cloud = await Within(server.ReadAsync()); if (ticket == generation) ShowConflict(cloud); }
                    catch (Exception) { if (ticket == generation) Fail("No pudimos reconciliar el progreso. Tus datos locales están protegidos."); }
                }
                else if (!IsTransient(error)) { game?.SuspendPlayer(); Fail("No pudimos sincronizar con seguridad. Tus datos locales están protegidos."); }
                else
                {
                    offline = true; retryAt = Time.unscaledTime + retryDelay; retryDelay = Mathf.Min(120, retryDelay * 2);
                    if (initialSyncWaiting) message = "Tu progreso local está protegido. Necesitás conexión para confirmar la primera sincronización antes de jugar.";
                }
            }
            finally { syncing = false; }
        }
        private static bool ContainsConflict(Exception error) => error is CloudRevisionConflict ||
            (error.InnerException != null && ContainsConflict(error.InnerException));
        private static bool IsTransient(Exception error)
        {
            if (error is TimeoutException) return true;
            if (error is FirebaseException firebaseError)
            {
                var code = (FirestoreError)firebaseError.ErrorCode;
                return code == FirestoreError.Unavailable || code == FirestoreError.DeadlineExceeded ||
                    code == FirestoreError.Cancelled || code == FirestoreError.Aborted;
            }
            return error.InnerException != null && IsTransient(error.InnerException);
        }
        public void OpenAccountMenu()
        {
            // Login, recovery and conflict resolution all live inside the main hub account panel.
            accountOpen = true;
        }
        public void SignOut()
        {
            if (game != null && !game.SuspendPlayer()) { Fail("No pudimos guardar. No cerramos tu sesión para proteger la partida."); return; }
            generation++; google.Cancel(); auth?.SignOut(); google.SignOut(); local = null; remote = null; conflictRemote = null;
            accountOpen = true; initialSyncWaiting = false; State = PlayerSessionState.LoginRequired; message = "Iniciá sesión con Google para continuar.";
            FindAnyObjectByType<StreetView>()?.ReturnToHubAfterAuthenticationRequired();
        }
        private void Fail(string text) { State = PlayerSessionState.Error; message = text; }
        private static async Task<T> Within<T>(Task<T> task, int seconds = 20)
        {
            if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(seconds))) != task)
            { _ = task.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted); throw new TimeoutException(); }
            return await task;
        }
        private void AccountButton(Rect rect, string caption, Action action)
        {
            var view = FindAnyObjectByType<StreetView>();
            if (view != null) view.DrawAccountButton(rect, caption, action);
            else if (GUI.Button(rect, caption)) action();
        }
        private void OnGUI()
        {
#if UNITY_EDITOR
            if (StreetGame.AllowEditorTestInitialization) return;
#endif
            var view = FindAnyObjectByType<StreetView>();
            if (State == PlayerSessionState.Ready && !accountOpen)
            {
                if (view != null && view.AccountMenuVisible && !view.HubAccountActionRendered)
                    AccountButton(HayChoriYPaty.StreetView.HubAccountButtonScreenBounds(
                        new Vector2(Screen.width, Screen.height)), "CUENTA", OpenAccountMenu);
                return;
            }

            // Never put a second full-screen Firebase cover in front of the three-phrase
            // startup presentation or the hub. The account panel is opened from the hub hotspot.
            if (!accountOpen || (view != null && !view.AccountMenuVisible)) return;

            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            int oldDepth = GUI.depth;
            GUI.depth = -1000;
            GUI.color = new Color(0f, 0f, 0f, .48f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float scale = Mathf.Min(Screen.width / 540f, Screen.height / 960f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 540f * scale) * .5f,
                (Screen.height - 960f * scale) * .5f), Quaternion.identity, Vector3.one * scale);

            Rect card = new Rect(34, 245, 472, 470);
            GUI.color = new Color(.025f, .16f, .31f, .98f);
            GUI.DrawTexture(card, Texture2D.whiteTexture);
            GUI.color = new Color(.16f, .77f, 1f, 1f);
            GUI.DrawTexture(new Rect(card.x, card.y, card.width, 4), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(card.x, card.yMax - 4, card.width, 4), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(card.x, card.y, 4, card.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(card.xMax - 4, card.y, 4, card.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var title = new GUIStyle(GUI.skin.label) { fontSize = 29, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            title.normal.textColor = Color.white;
            GUI.Label(new Rect(card.x + 20, card.y + 20, card.width - 40, 48), "CUENTA", title);

            string shown;
            if (State == PlayerSessionState.Ready)
                shown = offline ? "Sesión de Google guardada. Progreso local pendiente de sincronizar."
                    : "Tu progreso está vinculado con Google.";
            else if (State == PlayerSessionState.LoginRequired && string.IsNullOrWhiteSpace(message))
                shown = "Iniciá sesión con Google para jugar y guardar tu progreso.";
            else
                shown = string.IsNullOrWhiteSpace(message) ? "Preparando tu cuenta…" : message;
            var text = new GUIStyle(GUI.skin.label) { fontSize = 21, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            text.normal.textColor = Color.white;
            GUI.Label(new Rect(card.x + 36, card.y + 82, card.width - 72, 132), shown, text);

            float buttonY = card.y + 278;
            if (State == PlayerSessionState.Ready)
            {
                AccountButton(new Rect(74, buttonY, 392, 52), "CERRAR SESIÓN", SignOut);
                buttonY += 60;
            }
            else if (State == PlayerSessionState.LoginRequired)
            {
                if (view != null) view.DrawGoogleAccountButton(new Rect(70, buttonY, 400, 54),
                    "INICIAR SESIÓN CON GOOGLE", googleButton, SignIn);
                else if (GUI.Button(new Rect(70, buttonY, 400, 54), "INICIAR SESIÓN CON GOOGLE")) SignIn();
                buttonY += 60;
            }
            else if (State == PlayerSessionState.LegacyConsent)
            {
                AccountButton(new Rect(58, buttonY, 424, 54), "VINCULAR PROGRESO DE ESTE TELÉFONO", ConfirmLegacyImport);
                buttonY += 60;
            }
            else if (State == PlayerSessionState.Conflict && conflictRemote != null)
            {
                AccountButton(new Rect(58, buttonY, 424, 54), "USAR NUBE Y CONSERVAR COPIA LOCAL", UseRemotePreservingLocal);
                buttonY += 60;
            }
            else if (State == PlayerSessionState.Error)
            {
                AccountButton(new Rect(74, buttonY, 392, 52), "REINTENTAR", () => { _ = BootstrapAsync(); });
                buttonY += 60;
            }
            else if (initialSyncWaiting && offline)
            {
                AccountButton(new Rect(58, buttonY, 424, 54), "REINTENTAR SINCRONIZACIÓN", () => { retryAt = 0; SyncPending(); });
                buttonY += 60;
            }

            if (auth?.CurrentUser != null && State != PlayerSessionState.Ready && State != PlayerSessionState.Authenticating)
            {
                AccountButton(new Rect(74, buttonY, 392, 52), "CAMBIAR CUENTA GOOGLE", SignOut);
                buttonY += 60;
            }
            AccountButton(new Rect(74, buttonY, 392, 52), "VOLVER", () => accountOpen = false);

            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
            GUI.depth = oldDepth;
        }
    }
}
