using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>
    /// Shared delivery/order cues, plus a coin when a normal level reaches a product quota.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class OrderDeliverySound : MonoBehaviour
    {
        private const string ClipResourcePath = "Audio/order-delivery-soft-pop";
        private const string CompleteClipResourcePath = "Audio/order-complete-bubble";
        private const string ProductGoalClipResourcePath = "Audio/coin-appear-classic";
        [SerializeField, Range(0f, 1f)] private float volume = 0.2f;
        [SerializeField, Range(0f, 1f)] private float completeVolume = 0.25f;
        [SerializeField, Range(0f, 1f)] private float productGoalVolume = 0.22f;
        [SerializeField, Range(0f, 0.1f)] private float pitchVariation = 0.03f;

        private AudioSource audioSource;
        private AudioClip deliveryClip, completeClip, productGoalClip;
        private StreetSimulation simulation;
        private TowerDefenseGameMode towerDefense;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            deliveryClip = Resources.Load<AudioClip>(ClipResourcePath);
        }

        public void Initialize(StreetSimulation source)
        {
            if (simulation == source) return;
            Unsubscribe();
            simulation = source;
            if (isActiveAndEnabled) Subscribe();
        }

        public void BindTowerDefense(TowerDefenseGameMode mode)
        {
            if (towerDefense == mode) return;
            UnsubscribeTowerDefense();
            towerDefense = mode;
            if (isActiveAndEnabled) SubscribeTowerDefense();
        }

        private void OnEnable()
        {
            Subscribe();
            SubscribeTowerDefense();
        }

        private void OnDisable()
        {
            Unsubscribe();
            UnsubscribeTowerDefense();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            UnsubscribeTowerDefense();
        }

        private void Subscribe()
        {
            if (simulation == null) return;
            simulation.ProductUnitDelivered += OnUnitDelivered;
            simulation.OrderCompleted += OnOrderCompleted;
            simulation.ProductGoalCompleted += OnProductGoalCompleted;
        }

        private void Unsubscribe()
        {
            if (simulation == null) return;
            simulation.ProductUnitDelivered -= OnUnitDelivered;
            simulation.OrderCompleted -= OnOrderCompleted;
            simulation.ProductGoalCompleted -= OnProductGoalCompleted;
        }

        private void SubscribeTowerDefense()
        {
            if (towerDefense != null) towerDefense.DeliveryCompleted += OnTowerDefenseDelivery;
        }

        private void UnsubscribeTowerDefense()
        {
            if (towerDefense != null) towerDefense.DeliveryCompleted -= OnTowerDefenseDelivery;
        }

        private void OnTowerDefenseDelivery(TowerDefenseDelivery delivery)
        {
            OnUnitDelivered();
            if (delivery.CompletesOrder) OnOrderCompleted();
        }

        private void OnUnitDelivered()
        {
            if (deliveryClip == null) deliveryClip = Resources.Load<AudioClip>(ClipResourcePath);
            PlayCue(deliveryClip, volume);
        }

        private void OnOrderCompleted()
        {
            if (completeClip == null) completeClip = Resources.Load<AudioClip>(CompleteClipResourcePath);
            PlayCue(completeClip, completeVolume);
        }

        private void OnProductGoalCompleted(int product)
        {
            if (productGoalClip == null) productGoalClip = Resources.Load<AudioClip>(ProductGoalClipResourcePath);
            PlayCue(productGoalClip, productGoalVolume);
        }

        private void PlayCue(AudioClip clip, float cueVolume)
        {
            if (clip == null || audioSource == null) return;
            audioSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
            audioSource.PlayOneShot(clip, Mathf.Clamp01(cueVolume));
        }
    }
}
