using UnityEngine;

namespace DeadCircuit.AI
{
    public enum NoiseType { Footstep, Voice, Impact, Weapon }

    public readonly struct NoiseEvent
    {
        public readonly Vector3 Position;
        public readonly float Loudness;
        public readonly NoiseType Type;

        public NoiseEvent(Vector3 position, float loudness, NoiseType type)
        {
            Position = position;
            Loudness = Mathf.Clamp01(loudness);
            Type = type;
        }
    }
}
