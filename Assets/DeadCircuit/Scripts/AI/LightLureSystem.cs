using System.Collections.Generic;
using UnityEngine;

namespace DeadCircuit.AI
{
    public enum LightSignalType { Flashlight, WorldLight, Lure }

    public struct LightSignal
    {
        public Vector3 Position;
        public float Intensity;
        public float Radius;
        public LightSignalType Type;
        public float ExpiresAt;
    }

    public static class LightLureSystem
    {
        static readonly List<LightSignal> Signals = new();

        public static void Emit(Vector3 position, float intensity, float radius, LightSignalType type, float duration = 0.3f)
        {
            Signals.Add(new LightSignal { Position = position, Intensity = Mathf.Max(0f, intensity), Radius = Mathf.Max(0.1f, radius), Type = type, ExpiresAt = Time.time + duration });
        }

        public static IReadOnlyList<LightSignal> ActiveSignals()
        {
            Signals.RemoveAll(s => s.ExpiresAt < Time.time);
            return Signals;
        }

        public static bool TryGetBestSignal(Vector3 observer, out LightSignal best)
        {
            best = default;
            float score = 0f;
            foreach (var signal in ActiveSignals())
            {
                float distance = Vector3.Distance(observer, signal.Position);
                if (distance > signal.Radius) continue;
                float candidate = signal.Intensity * (1f - distance / signal.Radius);
                if (candidate > score) { score = candidate; best = signal; }
            }
            return score > 0f;
        }
    }
}
