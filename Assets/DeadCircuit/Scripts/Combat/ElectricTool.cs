using FishNet.Object;
using UnityEngine;

namespace DeadCircuit.Combat
{
    public class ElectricTool : NetworkBehaviour
    {
        [SerializeField] float maxCharge = 100f;
        [SerializeField] float drainPerSecond = 18f;
        [SerializeField] int stunDamage = 18;
        [SerializeField] float stunDuration = 1.25f;
        float charge;

        public void SetPowered(bool powered)
        {
            if (!powered) charge = 0f;
        }

        public void Recharge(float amount)
        {
            charge = Mathf.Clamp(charge + amount, 0f, maxCharge);
        }

        public void Shock()
        {
            if (!IsOwner || charge <= 1f) return;
            charge -= drainPerSecond;
            RequestShockServerRpc();
        }

        [ServerRpc]
        void RequestShockServerRpc()
        {
            if (!Physics.Raycast(transform.position, transform.forward, out var hit, 2f)) return;
            var monster = hit.collider.GetComponentInParent<DeadCircuit.AI.DeadCircuitMonster>();
            if (monster != null) monster.StunAndKnockback(transform.position, stunDamage, stunDuration, 3.2f);
        }
    }
}
