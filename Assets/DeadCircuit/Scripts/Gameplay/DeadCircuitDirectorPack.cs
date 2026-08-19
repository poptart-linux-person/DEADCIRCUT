using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace DeadCircuit.Gameplay
{
    public class DeadCircuitDirectorPack : NetworkBehaviour
    {
        public readonly SyncVar<float> Threat = new(0f);
        public readonly SyncVar<int> Escalation = new(0);
        public readonly SyncVar<int> SurvivorCount = new(0);
        public readonly SyncVar<bool> PowerOut = new(false);
        public readonly SyncVar<bool> Blackout = new(false);
        public readonly SyncVar<bool> HuntActive = new(false);
        public readonly SyncVar<float> EventTimer = new(0f);

        [SerializeField] float threatDecay = 1.5f;
        [SerializeField] float eventCooldown = 30f;
        [SerializeField] PowerGrid powerGrid;

        [ServerCallback]
        void Update()
        {
            Threat.Value = Mathf.Max(0f, Threat.Value - threatDecay * Time.deltaTime);
            EventTimer.Value = Mathf.Max(0f, EventTimer.Value - Time.deltaTime);
            if (powerGrid == null) powerGrid = Object.FindObjectOfType<PowerGrid>();
            if (powerGrid != null)
            {
                PowerOut.Value = !powerGrid.Powered.Value;
                Blackout.Value = PowerOut.Value;
            }
            if (EventTimer.Value <= 0f && Threat.Value > 60f) StartDynamicHunt();
        }

        [Server]
        public void AddThreat(float amount)
        {
            Threat.Value = Mathf.Clamp(Threat.Value + Mathf.Abs(amount), 0f, 100f);
            if (Threat.Value >= 85f) Escalation.Value++;
        }

        [Server]
        public void TriggerPowerOutage()
        {
            PowerOut.Value = true;
            Blackout.Value = true;
            if (powerGrid != null) powerGrid.ForceOutage();
            EventTimer.Value = eventCooldown;
        }

        [Server]
        public void RestorePower()
        {
            PowerOut.Value = false;
            Blackout.Value = false;
            if (powerGrid != null) powerGrid.RestorePower(0.5f);
        }

        [Server]
        void StartDynamicHunt()
        {
            HuntActive.Value = true;
            EventTimer.Value = eventCooldown;
            AddThreat(12f);
        }

        [Server]
        public void EndHunt() => HuntActive.Value = false;

        [Server]
        public void ReportSurvivors(int count) => SurvivorCount.Value = Mathf.Max(0, count);
    }
}
