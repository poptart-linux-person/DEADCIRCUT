using FishNet.Object;
using UnityEngine;
using DeadCircuit.Gameplay;

namespace DeadCircuit.Combat
{
    public class ElectricTool : NetworkBehaviour
    {
        [SerializeField] float maxCharge = 100f;
        [SerializeField] float drainPerSecond = 18f;
        [SerializeField] int stunDamage = 18;
        [SerializeField] float stunDuration = 1.25f;
        [SerializeField] PowerGrid powerGrid;
        float charge;
        bool powered = true;

        public bool IsPowered => powered && (powerGrid == null || powerGrid.CanUsePoweredEquipment());

        public void SetPowered(bool value)
        {
            powered = value;
            if (!powered) charge = 0f;
        }

        public void Recharge(float amount)
        {
            if (!IsPowered) return;
            charge = Mathf.Clamp(charge + amount, 0f, maxCharge);
        }

        public void Shock()
        {
            if (!IsOwner || !IsPowered || charge <= 1f) return;
            RequestShockServerRpc();
        }

        [ServerRpc]
        void RequestShockServerRpc()
        {
            if (!IsPowered || charge <= 1f) return;
            charge = Mathf.Max(0f, charge - drainPerSecond);
            if (!Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 2f)) return;
            var monster = hit.collider.GetComponentInParent<DeadCircuit.AI.DeadCircuitMonster>();
            if (monster != null) monster.StunAndKnockback(transform.position, stunDamage, stunDuration, 3.2f);
        }
    }
}
