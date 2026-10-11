using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Small 2D presentation response to a successfully completed customer order.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class OrderDeliverySound : MonoBehaviour
    {
        private const string ClipResourcePath = "Audio/order-delivery-soft-pop";
        private const float MinimumSpacingSeconds = 0.15f;

        [SerializeField, Range(0f, 1f)] private float volume = 0.2f;
        [SerializeField, Range(0f, 0.1f)] private float pitchVariation = 0.03f;

        private AudioSource audioSource;
        private AudioClip deliveryClip;
        private StreetSimulation simulation;
        private float nextAllowedPlayback;

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
            if (simulation != null) simulation.OrderCompleted -= Play;
            simulation = source;
            if (isActiveAndEnabled && simulation != null) simulation.OrderCompleted += Play;
        }

        private void OnEnable()
        {
            if (simulation != null) simulation.OrderCompleted += Play;
        }

        private void OnDisable()
        {
            if (simulation != null) simulation.OrderCompleted -= Play;
        }

        private void OnDestroy()
        {
            if (simulation != null) simulation.OrderCompleted -= Play;
        }

        private void Play()
        {
            if (deliveryClip == null) deliveryClip = Resources.Load<AudioClip>(ClipResourcePath);
            if (deliveryClip == null || Time.unscaledTime < nextAllowedPlayback) return;

            audioSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
            audioSource.PlayOneShot(deliveryClip, Mathf.Clamp01(volume));
            nextAllowedPlayback = Time.unscaledTime + MinimumSpacingSeconds;
        }
    }
}
