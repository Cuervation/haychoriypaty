using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Small 2D presentation response to each successfully delivered order item.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class OrderDeliverySound : MonoBehaviour
    {
        private const string ClipResourcePath = "Audio/order-delivery-soft-pop";
        [SerializeField, Range(0f, 1f)] private float volume = 0.2f;
        [SerializeField, Range(0f, 0.1f)] private float pitchVariation = 0.03f;

        private AudioSource audioSource;
        private AudioClip deliveryClip;
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
            if (simulation != null) simulation.ProductUnitDelivered -= Play;
            simulation = source;
            if (isActiveAndEnabled && simulation != null) simulation.ProductUnitDelivered += Play;
        }

        private void OnEnable()
        {
            if (simulation != null) simulation.ProductUnitDelivered += Play;
        }

        private void OnDisable()
        {
            if (simulation != null) simulation.ProductUnitDelivered -= Play;
        }

        private void OnDestroy()
        {
            if (simulation != null) simulation.ProductUnitDelivered -= Play;
        }

        private void Play()
        {
            if (deliveryClip == null) deliveryClip = Resources.Load<AudioClip>(ClipResourcePath);
            if (deliveryClip == null) return;

            audioSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
            audioSource.PlayOneShot(deliveryClip, Mathf.Clamp01(volume));
        }
    }
}
