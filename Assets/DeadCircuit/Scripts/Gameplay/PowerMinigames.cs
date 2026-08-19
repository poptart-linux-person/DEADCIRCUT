using FishNet.Object;
using UnityEngine;

namespace DeadCircuit.Gameplay
{
    public enum PowerMinigameType { FuseMatch, WireRoute, FrequencyTune, PressureBalance, CrankTiming, BatterySequence, SwitchOrder, RotorBalance, ValveTiming, RelaySync, CapacitorCharge, CoolantFlow }

    public class PowerMinigame : NetworkBehaviour
    {
        [SerializeField] PowerMinigameType type;
        [SerializeField, Range(0.1f, 0.95f)] float targetTolerance = 0.22f;
        [SerializeField] float completionAmount = 0.28f;
        [SerializeField] GeneratorSystem generator;
        [SerializeField] PowerGrid grid;
        float progress;

        void Awake()
        {
            if (generator == null) generator = GetComponentInParent<GeneratorSystem>();
            if (grid == null) grid = Object.FindObjectOfType<PowerGrid>();
        }

        [ServerRpc]
        public void SubmitStepServerRpc(float value)
        {
            float target = 0.5f + Mathf.Sin((int)type * 1.37f) * 0.22f;
            if (Mathf.Abs(value - target) <= targetTolerance)
                progress = Mathf.Clamp01(progress + completionAmount);
            else
                progress = Mathf.Clamp01(progress - 0.08f);

            if (progress < 1f) return;
            progress = 0f;
            if (generator != null) generator.RepairInternal();
            if (grid != null) grid.RestorePower(0.25f);
        }

        public bool Complete => progress >= 1f;
        public float Progress => progress;
    }
}
