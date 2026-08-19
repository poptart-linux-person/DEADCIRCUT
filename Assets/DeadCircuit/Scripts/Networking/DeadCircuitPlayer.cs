using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using DeadCircuit.Combat;

namespace DeadCircuit.Networking
{
    public class DeadCircuitPlayer : NetworkBehaviour
    {
        public readonly SyncVar<int> Health = new(120);
        public readonly SyncVar<bool> Downed = new(false);
        public readonly SyncVar<int> Revives = new(1);

        [SerializeField] CharacterController characterController;
        [SerializeField] float reviveDuration = 3f;
        [SerializeField, Range(0f, 0.75f)] float damageReduction = 0.22f;
        [SerializeField, Range(0.5f, 1.25f)] float combatPowerScale = 0.90f;
        [SerializeField] DeathRagdoll deathRagdoll;
        float reviveTimer;

        public float CombatPowerScale => combatPowerScale;

        public override void OnStartServer()
        {
            base.OnStartServer();
            Health.Value = 120;
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
            if (Health.Value == 0) DieServer();
        }

        [Server]
        void DieServer()
        {
            if (Downed.Value) return;
            Downed.Value = true;
            if (deathRagdoll != null)
                deathRagdoll.EnableRagdollObserversRpc(Vector3.zero, transform.position);
        }

        [Server]
        public void ExecuteAndThrowServer(Vector3 impulse, Vector3 hitPoint)
        {
            if (Downed.Value) return;
            Downed.Value = true;
            Health.Value = 0;
            if (deathRagdoll != null)
                deathRagdoll.EnableRagdollObserversRpc(impulse, hitPoint);
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
            Health.Value = 42;
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
