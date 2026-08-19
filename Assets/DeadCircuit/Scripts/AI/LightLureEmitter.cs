using UnityEngine;

namespace DeadCircuit.AI
{
    public class LightLureEmitter : MonoBehaviour
    {
        [SerializeField] Light source;
        [SerializeField] LightSignalType signalType = LightSignalType.WorldLight;
        [SerializeField] float radius = 16f;
        [SerializeField] float intensityMultiplier = 1f;

        void Update()
        {
            if (source == null || !source.enabled || source.intensity <= 0f) return;
            LightLureSystem.Emit(transform.position, source.intensity * intensityMultiplier, radius, signalType, 0.15f);
        }

        public void EmitLure(float intensity, float duration = 2f)
            => LightLureSystem.Emit(transform.position, intensity, radius, LightSignalType.Lure, duration);
    }
}
