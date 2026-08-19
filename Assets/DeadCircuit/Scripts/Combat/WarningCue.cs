using UnityEngine;

namespace DeadCircuit.Combat
{
    public class WarningCue : MonoBehaviour
    {
        [SerializeField] XSawCombat saw;
        [SerializeField] AudioSource warningAudio;
        [SerializeField] Light warningLight;
        [SerializeField] Renderer warningRenderer;
        [SerializeField] float maxPulse = 0.15f;
        Vector3 baseScale;

        void Awake()
        {
            if (warningRenderer != null) baseScale = warningRenderer.transform.localScale;
        }

        void Update()
        {
            if (saw == null) return;
            bool active = saw.ShouldShowWarning;
            float intensity = active ? 1f - saw.WarningIntensity : 0f;
            float pulse = active ? 1f + Mathf.Sin(Time.time * 22f) * maxPulse * Mathf.Max(0.2f, intensity) : 1f;
            if (warningRenderer != null) warningRenderer.transform.localScale = baseScale * pulse;
            if (warningLight != null) warningLight.intensity = active ? 1.5f + intensity * 5f : 0f;
            if (warningAudio != null)
            {
                warningAudio.volume = active ? 0.2f + intensity * 0.7f : 0f;
                warningAudio.pitch = 0.9f + intensity * 0.25f;
            }
        }
    }
}
