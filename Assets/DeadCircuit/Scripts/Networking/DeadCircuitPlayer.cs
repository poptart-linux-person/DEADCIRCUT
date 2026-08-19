using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace DeadCircuit.Networking
{
    public class DeadCircuitPlayer : NetworkBehaviour
    {
        public readonly SyncVar<int> Health = new(100);
        public readonly SyncVar<bool> Downed = new(false);
        public readonly SyncVar<int> Revives = new(1);

        [SerializeField] CharacterController characterController;
        [SerializeField] float reviveDuration = 3f;

        float reviveTimer;

        public override void OnStartServer()
        {
            base.OnStartServer();
            Health.Value = 100;
            Downed.Value = false;
        }

        [ServerRpc]
        public void DealDamageServerRpc(int amount)
        {
            if (Downed.Value) return;
            Health.Value = Mathf.Max(0, Health.Value - Mathf.Abs(amount));
            if (Health.Value == 0) Downed.Value = true;
        }

        [ServerRpc]
        public void ReviveServerRpc(DeadCircuitPlayer target)
        {
            if (target == null || !target.Downed.Value || target.Revives.Value <= 0) return;
            target.ReviveInternal();
        }

        void ReviveInternal()
        {
            Revives.Value--;
            Health.Value = 35;
            Downed.Value = false;
        }

        [ServerRpc]
        public void RecoverServerRpc()
        {
            if (!Downed.Value) return;
            reviveTimer += Time.deltaTime;
            if (reviveTimer >= reviveDuration) ReviveInternal();
        }
    }
}
