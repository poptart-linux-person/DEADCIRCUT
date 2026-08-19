using FishNet.Object;
using UnityEngine;

namespace DeadCircuit.Gameplay
{
    public class PowerGrid : NetworkBehaviour
    {
        public readonly SyncVar<bool> Powered = new(true);
        public readonly SyncVar<float> Stability = new(1f);
        [SerializeField] float drainPerSecond = 0.0025f;
        [SerializeField] float outageThreshold = 0.08f;

        [ServerCallback]
        void Update()
        {
            if (!Powered.Value) return;
            Stability.Value = Mathf.Clamp01(Stability.Value - drainPerSecond * Time.deltaTime);
            if (Stability.Value <= outageThreshold)
                Powered.Value = false;
        }

        [Server]
        public void RestorePower(float amount = 0.35f)
        {
            Stability.Value = Mathf.Clamp01(Stability.Value + amount);
            if (Stability.Value > outageThreshold)
                Powered.Value = true;
        }

        public bool CanUsePoweredEquipment() => Powered.Value && Stability.Value > outageThreshold;
    }
}
