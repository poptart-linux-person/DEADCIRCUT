using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace DeadCircuit.Gameplay
{
    public class PlayerSystemPack : NetworkBehaviour
    {
        [Header("Core")]
        public readonly SyncVar<float> Stamina = new(100f);
        public readonly SyncVar<float> Adrenaline = new(0f);
        public readonly SyncVar<float> Fear = new(0f);
        public readonly SyncVar<float> Bleed = new(0f);
        public readonly SyncVar<float> Balance = new(1f);
        public readonly SyncVar<bool> Exhausted = new(false);
        public readonly SyncVar<bool> IsCrouched = new(false);
        public readonly SyncVar<bool> IsHiding = new(false);
        public readonly SyncVar<bool> HasArmor = new(false);
        public readonly SyncVar<int> ArmorDurability = new(0);

        [SerializeField] float staminaMax = 100f;
        [SerializeField] float staminaDrain = 24f;
        [SerializeField] float staminaRegen = 18f;
        [SerializeField] float adrenalineDecay = 7f;
        [SerializeField] float fearDecay = 4f;
        [SerializeField] float bleedDamagePerSecond = 3f;
        [SerializeField] float lowStaminaThreshold = 15f;

        [ServerCallback]
        void Update()
        {
            Stamina.Value = Mathf.Clamp(Stamina.Value + staminaRegen * Time.deltaTime, 0f, staminaMax);
            Adrenaline.Value = Mathf.Max(0f, Adrenaline.Value - adrenalineDecay * Time.deltaTime);
            Fear.Value = Mathf.Max(0f, Fear.Value - fearDecay * Time.deltaTime);
            if (Bleed.Value > 0f)
                Bleed.Value = Mathf.Max(0f, Bleed.Value - 0.05f * Time.deltaTime);
            Exhausted.Value = Stamina.Value <= lowStaminaThreshold;
            Balance.Value = Mathf.MoveTowards(Balance.Value, 1f, Time.deltaTime * 0.35f);
        }

        public bool TrySpendStamina(float amount)
        {
            if (!IsOwner || Exhausted.Value) return false;
            RequestSpendStaminaServerRpc(Mathf.Max(0f, amount));
            return true;
        }

        [ServerRpc]
        void RequestSpendStaminaServerRpc(float amount)
        {
            if (Stamina.Value < amount) return;
            Stamina.Value -= amount;
        }

        [Server]
        public void AddAdrenaline(float amount) => Adrenaline.Value = Mathf.Clamp(Adrenaline.Value + amount, 0f, 100f);

        [Server]
        public void AddFear(float amount) => Fear.Value = Mathf.Clamp(Fear.Value + amount, 0f, 100f);

        [Server]
        public void AddBleed(float amount) => Bleed.Value = Mathf.Clamp(Bleed.Value + amount, 0f, 100f);

        [Server]
        public void Stagger(float amount) => Balance.Value = Mathf.Clamp01(Balance.Value - Mathf.Abs(amount));

        [Server]
        public void EquipArmor(int durability)
        {
            HasArmor.Value = durability > 0;
            ArmorDurability.Value = Mathf.Max(0, durability);
        }

        [Server]
        public bool AbsorbWithArmor(int damage)
        {
            if (!HasArmor.Value || ArmorDurability.Value <= 0) return false;
            int absorbed = Mathf.Min(ArmorDurability.Value, Mathf.Max(1, damage / 2));
            ArmorDurability.Value -= absorbed;
            if (ArmorDurability.Value <= 0) HasArmor.Value = false;
            return true;
        }

        [ServerRpc]
        public void SetCrouchedServerRpc(bool crouched) => IsCrouched.Value = crouched;

        [ServerRpc]
        public void SetHidingServerRpc(bool hiding) => IsHiding.Value = hiding;

        [Server]
        public void Trip(float severity)
        {
            Balance.Value = Mathf.Clamp01(Balance.Value - severity);
            AddFear(severity * 15f);
        }
    }
}
