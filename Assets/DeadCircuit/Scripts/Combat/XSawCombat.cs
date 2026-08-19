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
        bool inputArmed;

        void Update()
        {
            cooldown = Mathf.Max(0f, cooldown - Time.deltaTime);
            if (!IsOwner) return;
            if (State.Value == ClashState.Window) WindowRemaining.Value = Mathf.Max(0f, WindowRemaining.Value - Time.deltaTime);
            if (WindowRemaining.Value <= 0f && State.Value == ClashState.Window) FinishWindow(false);
            if (State.Value == ClashState.Warning && Input.GetButtonDown("Fire1")) ArmReaction();
            if (State.Value == ClashState.Window && Input.GetButtonDown("Fire1")) ResolveReaction();
        }

        public void Attack()
        {
            if (!IsOwner || cooldown > 0f || State.Value != ClashState.Idle) return;
            cooldown = attackCooldown;
            if (Physics.Raycast(transform.position + Vector3.up * 1.1f, transform.forward, out RaycastHit hit, clashRange, hittableLayers))
            {
                var target = hit.collider.GetComponentInParent<XSawCombat>();
                if (target != null) target.TriggerWarningServerRpc();
                var health = hit.collider.GetComponentInParent<DeadCircuit.Networking.DeadCircuitPlayer>();
                if (health != null) health.DealDamageServerRpc(normalDamage);
            }
        }

        [ServerRpc]
        void TriggerWarningServerRpc()
        {
            State.Value = ClashState.Warning;
            WindowRemaining.Value = warningTime;
        }

        void ArmReaction()
        {
            if (State.Value != ClashState.Warning) return;
            inputArmed = true;
            ResolveReactionServerRpc(true);
        }

        void ResolveReaction()
        {
            if (!inputArmed) return;
            inputArmed = false;
            ResolveReactionServerRpc(false);
        }

        [ServerRpc]
        void ResolveReactionServerRpc(bool early)
        {
            if (State.Value != ClashState.Warning && State.Value != ClashState.Window) return;
            State.Value = ClashState.Window;
            WindowRemaining.Value = early ? reactionWindow : Mathf.Min(reactionWindow, WindowRemaining.Value);
        }

        void FinishWindow(bool success)
        {
            FinishWindowServerRpc(success);
        }

        [ServerRpc]
        void FinishWindowServerRpc(bool success)
        {
            State.Value = success ? ClashState.Success : ClashState.Fail;
            WindowRemaining.Value = 0f;
            if (success) HitExecution();
            Invoke(nameof(ResetState), 0.4f);
        }

        void HitExecution()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 1.1f, transform.forward, out RaycastHit hit, clashRange + 0.7f, hittableLayers))
            {
                var health = hit.collider.GetComponentInParent<DeadCircuit.Networking.DeadCircuitPlayer>();
                if (health != null) health.DealDamageServerRpc(executionDamage);
            }
        }

        void ResetState() => State.Value = ClashState.Idle;

        public bool ShouldShowWarning => State.Value == ClashState.Warning || State.Value == ClashState.Window;
        public float WarningIntensity => State.Value == ClashState.Warning ? Mathf.Clamp01(WindowRemaining.Value / warningTime) : 1f;
    }
}
