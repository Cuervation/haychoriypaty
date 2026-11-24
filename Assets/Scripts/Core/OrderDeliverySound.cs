using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>
    /// Small 2D presentation responses to a delivery. Default levels: the soft pop plays on every delivered order item.
    /// San Lorenzo (2026-11-23, a pedido, for testing): the soft pop plays when a worker hands something over (every delivered
    /// item) and the classic coin ping plays when the whole order is completed and the customer leaves.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class OrderDeliverySound : MonoBehaviour
    {
        private const string ClipResourcePath = "Audio/order-delivery-soft-pop";
        private const string CoinClipResourcePath = "Audio/coin-appear-classic";
        private const int SanLorenzoLevelIndex = 6;
        [SerializeField, Range(0f, 1f)] private float volume = 0.2f;
        [SerializeField, Range(0f, 1f)] private float coinVolume = 0.22f;
        [SerializeField, Range(0f, 0.1f)] private float pitchVariation = 0.03f;

        private AudioSource audioSource;
        private AudioClip deliveryClip, coinClip;
        private StreetSimulation simulation;

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

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
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

        private bool CoinRingsOnTheCompletedOrder => simulation != null && simulation.LevelIndex == SanLorenzoLevelIndex;

        private void OnUnitDelivered()
        {
            Play(); // the soft pop, on every item a worker hands over
        }

        private void OnOrderCompleted()
        {
            if (!CoinRingsOnTheCompletedOrder) return;
            if (coinClip == null) coinClip = Resources.Load<AudioClip>(CoinClipResourcePath);
            if (coinClip == null || audioSource == null) return;
            audioSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
            audioSource.PlayOneShot(coinClip, Mathf.Clamp01(coinVolume));
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
