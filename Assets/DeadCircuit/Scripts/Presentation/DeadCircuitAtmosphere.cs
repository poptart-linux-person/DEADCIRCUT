using UnityEngine;

namespace DeadCircuit.Presentation
{
    public class DeadCircuitAtmosphere : MonoBehaviour
    {
        [SerializeField] Light[] adaptiveLights;
        [SerializeField] float minIntensity = 0.18f;
        [SerializeField] float maxIntensity = 1.0f;
        [SerializeField] float flickerSpeed = 3.5f;
        [SerializeField] float flickerAmount = 0.08f;
        [SerializeField] float heartbeatPulse = 0.025f;

        float seed;

        void Awake() => seed = Random.value * 100f;

        void Update()
        {
            if (adaptiveLights == null || adaptiveLights.Length == 0) return;
            float t = Time.time * flickerSpeed + seed;
            for (int i = 0; i < adaptiveLights.Length; i++)
            {
                Light light = adaptiveLights[i];
                if (light == null) continue;
                float noise = Mathf.PerlinNoise(t + i * 0.71f, i * 0.19f);
                float pulse = Mathf.Sin(Time.time * 1.7f + i) * heartbeatPulse;
                light.intensity = Mathf.Clamp(light.intensity * (1f - flickerAmount) + noise * flickerAmount + pulse, minIntensity, maxIntensity);
            }
        }
    }
}
