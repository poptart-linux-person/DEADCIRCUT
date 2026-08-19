using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace DeadCircuit.Gameplay
{
    public class MonsterSystemPack : NetworkBehaviour
    {
        public readonly SyncVar<float> Rage = new(0f);
        public readonly SyncVar<float> Stagger = new(0f);
        public readonly SyncVar<float> Balance = new(1f);
        public readonly SyncVar<float> Awareness = new(0f);
        public readonly SyncVar<float> InjuredArm = new(0f);
        public readonly SyncVar<float> InjuredLeg = new(0f);
        public readonly SyncVar<bool> Enraged = new(false);
        public readonly SyncVar<bool> KnockedDown = new(false);
        public readonly SyncVar<bool> Recovering = new(false);

        [SerializeField] float rageDecay = 3f;
        [SerializeField] float staggerRecovery = 20f;
        [SerializeField] float balanceRecovery = 0.25f;
        [SerializeField] float enrageThreshold = 75f;

        [ServerCallback]
        void Update()
        {
            Rage.Value = Mathf.Max(0f, Rage.Value - rageDecay * Time.deltaTime);
            Stagger.Value = Mathf.MoveTowards(Stagger.Value, 0f, staggerRecovery * Time.deltaTime);
            Balance.Value = Mathf.MoveTowards(Balance.Value, 1f, balanceRecovery * Time.deltaTime);
            InjuredArm.Value = Mathf.MoveTowards(InjuredArm.Value, 0f, 0.5f * Time.deltaTime);
            InjuredLeg.Value = Mathf.MoveTowards(InjuredLeg.Value, 0f, 0.25f * Time.deltaTime);
            Awareness.Value = Mathf.MoveTowards(Awareness.Value, 0f, 1.5f * Time.deltaTime);
            Enraged.Value = Rage.Value >= enrageThreshold;
            if (KnockedDown.Value && Balance.Value > 0.8f) KnockedDown.Value = false;
            Recovering.Value = Stagger.Value > 0.1f || KnockedDown.Value;
        }

        [Server]
        public void AddRage(float amount) => Rage.Value = Mathf.Clamp(Rage.Value + Mathf.Abs(amount), 0f, 100f);

        [Server]
        public void AddAwareness(float amount) => Awareness.Value = Mathf.Clamp(Awareness.Value + Mathf.Abs(amount), 0f, 100f);

        [Server]
        public void ApplyStagger(float amount)
        {
            Stagger.Value = Mathf.Clamp(Stagger.Value + Mathf.Abs(amount), 0f, 100f);
            Balance.Value = Mathf.Clamp01(Balance.Value - amount * 0.03f);
            if (Balance.Value <= 0.1f) KnockedDown.Value = true;
        }

        [Server]
        public void HitArm(float severity) => InjuredArm.Value = Mathf.Clamp(InjuredArm.Value + Mathf.Abs(severity), 0f, 100f);

        [Server]
        public void HitLeg(float severity)
        {
            InjuredLeg.Value = Mathf.Clamp(InjuredLeg.Value + Mathf.Abs(severity), 0f, 100f);
            Balance.Value = Mathf.Clamp01(Balance.Value - severity * 0.02f);
        }

        public float MoveMultiplier => Mathf.Lerp(0.55f, 1f, 1f - InjuredLeg.Value / 100f) * (Enraged.Value ? 1.12f : 1f);
        public float AttackMultiplier => Mathf.Lerp(1f, 0.65f, InjuredArm.Value / 100f) * (Enraged.Value ? 1.2f : 1f);
    }
}
