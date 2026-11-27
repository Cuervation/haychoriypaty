using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>
    /// Shared campaign audio: a soft pop per delivered item and a rising bubble when that item completes the order.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class OrderDeliverySound : MonoBehaviour
    {
        private const string ClipResourcePath = "Audio/order-delivery-soft-pop";
        private const string CompleteClipResourcePath = "Audio/order-complete-bubble";
        [SerializeField, Range(0f, 1f)] private float volume = 0.2f;
        [SerializeField, Range(0f, 1f)] private float completeVolume = 0.25f;
        [SerializeField, Range(0f, 0.1f)] private float pitchVariation = 0.03f;

        private AudioSource audioSource;
        private AudioClip deliveryClip, completeClip;
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
        }

        private void Unsubscribe()
        {
            if (simulation == null) return;
            simulation.ProductUnitDelivered -= OnUnitDelivered;
            simulation.OrderCompleted -= OnOrderCompleted;
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
            Play(); // the soft pop, on every item a worker hands over
        }

        private void OnOrderCompleted()
        {
            if (completeClip == null) completeClip = Resources.Load<AudioClip>(CompleteClipResourcePath);
            if (completeClip == null || audioSource == null) return;
            audioSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
            audioSource.PlayOneShot(completeClip, Mathf.Clamp01(completeVolume));
        }

        private void Play()
        {
            if (deliveryClip == null) deliveryClip = Resources.Load<AudioClip>(ClipResourcePath);
            if (deliveryClip == null || audioSource == null) return;

            audioSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
            audioSource.PlayOneShot(deliveryClip, Mathf.Clamp01(volume));
        }
    }
}
