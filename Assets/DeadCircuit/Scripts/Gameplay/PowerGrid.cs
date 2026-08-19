using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace DeadCircuit.Gameplay
{
    public class PowerGrid : NetworkBehaviour
    {
        public readonly SyncVar<bool> Powered = new(true);
        public readonly SyncVar<float> Stability = new(1f);
        public readonly SyncVar<float> AvailablePower = new(0f);

        [SerializeField] float baseLoad = 35f;
        [SerializeField] float outageThreshold = 0.08f;
        [SerializeField] float stabilityDrainAtLowPower = 0.12f;
        [SerializeField] float stabilityRecoveryAtSurplus = 0.035f;

        [ServerCallback]
        void Update()
        {
            float generation = 0f;
            foreach (GeneratorSystem generator in FindObjectsByType<GeneratorSystem>(FindObjectsSortMode.None))
            {
                if (generator != null && generator.Running.Value)
                    generation += generator.PowerOutput.Value;
            }

            AvailablePower.Value = generation;
            float ratio = baseLoad <= 0f ? 1f : generation / baseLoad;

            if (ratio < 1f)
                Stability.Value = Mathf.Clamp01(Stability.Value - stabilityDrainAtLowPower * (1f - ratio) * Time.deltaTime);
            else
                Stability.Value = Mathf.Clamp01(Stability.Value + stabilityRecoveryAtSurplus * Mathf.Min(1.5f, ratio - 1f) * Time.deltaTime);

            if (Stability.Value <= outageThreshold)
                Powered.Value = false;
            else if (ratio >= 1f)
                Powered.Value = true;
        }

        [Server]
        public void RestorePower(float amount = 0.35f)
        {
            Stability.Value = Mathf.Clamp01(Stability.Value + amount);
            if (Stability.Value > outageThreshold)
                Powered.Value = true;
        }

        public bool CanUsePoweredEquipment() => Powered.Value && Stability.Value > outageThreshold && AvailablePower.Value > 0f;

        [Server]
        public void ForceOutage()
        {
            Powered.Value = false;
            Stability.Value = 0f;
        }
    }
}
