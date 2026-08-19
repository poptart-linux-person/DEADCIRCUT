using FishNet.Object;
using UnityEngine;

namespace DeadCircuit.Gameplay
{
    public class CoopSystemPack : NetworkBehaviour
    {
        [SerializeField] float assistRadius = 3f;
        [SerializeField] float reviveSpeedMultiplier = 1f;

        public bool IsNear(Vector3 position) => Vector3.Distance(transform.position, position) <= assistRadius;

        [Server]
        public void AssistRevive(DeadCircuit.Networking.DeadCircuitPlayer target)
        {
            if (target == null || !IsNear(target.transform.position)) return;
            target.ReviveServerRpc(target);
        }

        [Server]
        public float CombineReviveProgress(int helpers) => Mathf.Clamp(1f + Mathf.Max(0, helpers - 1) * 0.55f, 1f, 2.2f) * reviveSpeedMultiplier;

        [Server]
        public void ShareAdrenaline(DeadCircuit.Networking.DeadCircuitPlayer ally, PlayerSystemPack allySystems, float amount)
        {
            if (ally == null || allySystems == null || !IsNear(ally.transform.position)) return;
            allySystems.AddAdrenaline(amount);
        }

        [Server]
        public void CreateDistraction(Vector3 position, float loudness)
        {
            DeadCircuit.AI.DeadCircuitNoiseDirector.Emit(
                new DeadCircuit.AI.NoiseEvent(position, loudness, DeadCircuit.AI.NoiseType.Voice));
        }
    }
}
