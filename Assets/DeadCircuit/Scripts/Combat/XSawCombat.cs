using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace DeadCircuit.Combat
{
    public enum ClashState { Idle, Warning, Window, Success, Fail }

    public class XSawCombat : NetworkBehaviour
    {
        public readonly SyncVar<ClashState> State = new(ClashState.Idle);
        public readonly SyncVar<float> WindowRemaining = new(0f);

        [SerializeField] float warningTime = 0.8f;
        [SerializeField] float reactionWindow = 0.32f;
        [SerializeField] float attackCooldown = 0.65f;
        [SerializeField] int normalDamage = 28;
        [SerializeField] int executionDamage = 100;
        [SerializeField] float clashRange = 2.25f;
        [SerializeField] LayerMask hittableLayers = ~0;

        float cooldown;

        void Update()
        {
            cooldown = Mathf.Max(0f, cooldown - Time.deltaTime);
            if (IsServerStarted && State.Value != ClashState.Idle)
            {
                WindowRemaining.Value = Mathf.Max(0f, WindowRemaining.Value - Time.deltaTime);
                if (State.Value == ClashState.Warning && WindowRemaining.Value <= 0f)
                {
                    State.Value = ClashState.Window;
                    WindowRemaining.Value = reactionWindow;
                }
                else if (State.Value == ClashState.Window && WindowRemaining.Value <= 0f)
                {
                    State.Value = ClashState.Fail;
                    Invoke(nameof(ResetState), 0.35f);
                }
            }
        }

        public void Attack()
        {
            if (!IsOwner || cooldown > 0f || State.Value != ClashState.Idle) return;
            cooldown = attackCooldown;
            RequestAttackServerRpc();
        }

        [ServerRpc]
        void RequestAttackServerRpc()
        {
            if (State.Value != ClashState.Idle) return;
            Vector3 origin = transform.position + Vector3.up * 1.1f;
            if (!Physics.Raycast(origin, transform.forward, out RaycastHit hit, clashRange, hittableLayers)) return;

            var target = hit.collider.GetComponentInParent<XSawCombat>();
            if (target != null && target != this) target.TriggerWarning();

            var health = hit.collider.GetComponentInParent<DeadCircuit.Networking.DeadCircuitPlayer>();
            if (health != null) health.DealDamageServerRpc(normalDamage);
        }

        [Server]
        void TriggerWarning()
        {
            State.Value = ClashState.Warning;
            WindowRemaining.Value = warningTime;
        }

        public void React()
        {
            if (!IsOwner || State.Value != ClashState.Window) return;
            ResolveReactionServerRpc();
        }

        [ServerRpc]
        void ResolveReactionServerRpc()
        {
            if (State.Value != ClashState.Window || WindowRemaining.Value <= 0f) return;
            State.Value = ClashState.Success;
            WindowRemaining.Value = 0f;
            HitExecution();
            Invoke(nameof(ResetState), 0.4f);
        }

        [Server]
        void HitExecution()
        {
            Vector3 origin = transform.position + Vector3.up * 1.1f;
            if (!Physics.Raycast(origin, transform.forward, out RaycastHit hit, clashRange + 0.7f, hittableLayers)) return;
            var health = hit.collider.GetComponentInParent<DeadCircuit.Networking.DeadCircuitPlayer>();
            if (health != null) health.DealDamageServerRpc(executionDamage);
        }

        [Server]
        void ResetState() => State.Value = ClashState.Idle;

        public bool ShouldShowWarning => State.Value == ClashState.Warning || State.Value == ClashState.Window;
        public float WarningIntensity => State.Value == ClashState.Warning ? Mathf.Clamp01(WindowRemaining.Value / warningTime) : 1f;
    }
}
