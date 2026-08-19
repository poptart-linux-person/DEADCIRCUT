using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using DeadCircuit.AI;

namespace DeadCircuit.Gameplay
{
    public enum GeneratorType { Diesel, Gas, Solar, Wind, HandCrank, BatteryBank, Hydro, Fusion, Steam, Biofuel, Emergency, Portable }

    public class GeneratorSystem : NetworkBehaviour
    {
        [SerializeField] GeneratorType type = GeneratorType.Diesel;
        [SerializeField, Range(0f, 1f)] float fuel = 1f;
        [SerializeField] float basePower = 100f;
        [SerializeField] float repairTime = 4f;
        [SerializeField] float noiseLevel = 0.35f;
        [SerializeField] float failureChancePerMinute = 0.03f;
        public readonly SyncVar<bool> Running = new(false);
        public readonly SyncVar<float> Health = new(1f);
        public readonly SyncVar<float> PowerOutput = new(0f);
        public readonly SyncVar<float> Fuel = new(1f);

        float repairTimer;
        float nextFailureCheck;
        float nextNoisePulse;

        public GeneratorType Type => type;
        public float CurrentNoise => Running.Value ? noiseLevel : 0f;

        public override void OnStartServer()
        {
            base.OnStartServer();
            Fuel.Value = fuel;
            Health.Value = 1f;
        }

        [ServerRpc]
        public void ToggleServerRpc()
        {
            if (Health.Value <= 0f) return;
            Running.Value = !Running.Value;
            RecalculateOutput();
        }

        [ServerRpc]
        public void RepairServerRpc()
        {
            RepairInternal(0.25f);
        }

        [Server]
        public void RepairInternal(float stepSeconds = 0.25f)
        {
            if (Health.Value >= 1f) return;
            repairTimer += stepSeconds;
            if (repairTimer >= repairTime)
            {
                repairTimer = 0f;
                Health.Value = 1f;
                RecalculateOutput();
            }
        }

        [ServerCallback]
        void Update()
        {
            if (!IsServerStarted) return;
            if (Running.Value && type != GeneratorType.Solar && type != GeneratorType.Wind && type != GeneratorType.Hydro)
                Fuel.Value = Mathf.Max(0f, Fuel.Value - Time.deltaTime * 0.00008f);
            if (Fuel.Value <= 0f && type != GeneratorType.Solar && type != GeneratorType.Wind && type != GeneratorType.Hydro)
                Running.Value = false;

            if (Running.Value && Time.time >= nextFailureCheck)
            {
                nextFailureCheck = Time.time + 60f;
                if (Random.value < failureChancePerMinute)
                {
                    Health.Value = Mathf.Clamp01(Health.Value - Random.Range(0.2f, 0.55f));
                    if (Health.Value <= 0f) Running.Value = false;
                }
            }
            RecalculateOutput();

            if (Running.Value && noiseLevel > 0f && Time.time >= nextNoisePulse)
            {
                nextNoisePulse = Time.time + Mathf.Lerp(2.5f, 0.8f, noiseLevel);
                DeadCircuitNoiseDirector.Emit(new NoiseEvent(transform.position, noiseLevel, NoiseType.Generator));
            }
        }

        [Server]
        void RecalculateOutput()
        {
            if (!Running.Value || Health.Value <= 0f)
            {
                PowerOutput.Value = 0f;
                return;
            }
            float modifier = type switch
            {
                GeneratorType.HandCrank => 0.35f,
                GeneratorType.Portable => 0.55f,
                GeneratorType.Solar => 0.7f,
                GeneratorType.Wind => 0.8f,
                GeneratorType.BatteryBank => 0.9f,
                GeneratorType.Emergency => 0.65f,
                GeneratorType.Biofuel => 1.0f,
                GeneratorType.Diesel => 1.15f,
                GeneratorType.Gas => 1.2f,
                GeneratorType.Steam => 1.25f,
                GeneratorType.Hydro => 1.35f,
                GeneratorType.Fusion => 1.8f,
                _ => 1f
            };
            PowerOutput.Value = basePower * modifier * Health.Value;
        }
    }
}
