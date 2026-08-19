using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace DeadCircuit.Networking
{
    public class DeadCircuitPlayer : NetworkBehaviour
    {
        public readonly SyncVar<int> Health = new(140);
        public readonly SyncVar<bool> Downed = new(false);
        public readonly SyncVar<int> Revives = new(1);

        [SerializeField] CharacterController characterController;
        [SerializeField] float reviveDuration = 3f;
        [SerializeField, Range(0f, 0.75f)] float damageReduction = 0.20f;
        [SerializeField] float impactPushScale = 0.18f;
        float reviveTimer;

        public override void OnStartServer()
        {
            base.OnStartServer();
            Health.Value = 140;
            Downed.Value = false;
            Revives.Value = 1;
        }

        [ServerRpc]
        public void DealDamageServerRpc(int amount)
        {
            if (Downed.Value) return;
            int incoming = Mathf.Abs(amount);
            int reduced = Mathf.Max(1, Mathf.RoundToInt(incoming * (1f - damageReduction)));
            Health.Value = Mathf.Max(0, Health.Value - reduced);
            if (Health.Value == 0) Downed.Value = true;
        }

        [ServerRpc]
        public void DealImpactServerRpc(int amount, Vector3 direction, float knockback)
        {
            if (Downed.Value) return;
            int incoming = Mathf.Abs(amount);
            int reduced = Mathf.Max(1, Mathf.RoundToInt(incoming * (1f - damageReduction)));
            Health.Value = Mathf.Max(0, Health.Value - reduced);
            if (characterController != null && direction.sqrMagnitude > 0.001f)
                characterController.Move(direction.normalized * Mathf.Max(0f, knockback) * impactPushScale);
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
            Health.Value = 45;
            Downed.Value = false;
            reviveTimer = 0f;
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
