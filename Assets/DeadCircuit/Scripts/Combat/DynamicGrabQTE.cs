using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using DeadCircuit.Networking;

namespace DeadCircuit.Combat
{
    public enum GrabQTEState { Idle, Warning, Grabbed, Throwing, Dodged, Failed }

    public class DynamicGrabQTE : NetworkBehaviour
    {
        public readonly SyncVar<GrabQTEState> State = new(GrabQTEState.Idle);
        public readonly SyncVar<float> WarningRemaining = new(0f);

        [SerializeField] float baseWarning = 0.55f;
        [SerializeField] float reactionWindow = 0.28f;
        [SerializeField] float grabDistance = 1.7f;
        [SerializeField] float throwForward = 8f;
        [SerializeField] float throwUp = 4.5f;

        float difficulty;
        float nextAttempt;
        DeadCircuitPlayer targetPlayer;

        [Server]
        public bool TryStart(DeadCircuitPlayer player, float attackIntensity)
        {
            if (!player || State.Value != GrabQTEState.Idle || Time.time < nextAttempt) return false;
            if (Vector3.Distance(transform.position, player.transform.position) > grabDistance) return false;
            targetPlayer = player;
            difficulty = Mathf.Clamp01(attackIntensity);
            State.Value = GrabQTEState.Warning;
            WarningRemaining.Value = Mathf.Lerp(baseWarning, reactionWindow * 1.6f, difficulty);
            nextAttempt = Time.time + 2f;
            return true;
        }

        [ServerCallback]
        void Update()
        {
            if (!IsServerStarted) return;
            if (State.Value == GrabQTEState.Warning)
            {
                WarningRemaining.Value = Mathf.Max(0f, WarningRemaining.Value - Time.deltaTime);
                if (WarningRemaining.Value <= 0f)
                {
                    State.Value = GrabQTEState.Grabbed;
                    WarningRemaining.Value = Mathf.Lerp(reactionWindow, 0.16f, difficulty);
                }
                return;
            }

            if (State.Value == GrabQTEState.Grabbed)
            {
                WarningRemaining.Value = Mathf.Max(0f, WarningRemaining.Value - Time.deltaTime);
                if (WarningRemaining.Value <= 0f) ThrowNow();
            }
        }

        [Server]
        public void ResolveDodge(DeadCircuitPlayer player)
        {
            if (player == null || player != targetPlayer) return;
            if (State.Value != GrabQTEState.Grabbed || WarningRemaining.Value <= 0f) return;
            State.Value = GrabQTEState.Dodged;
            WarningRemaining.Value = 0f;
            Invoke(nameof(ResetState), 0.25f);
        }

        [Server]
        void ThrowNow()
        {
            if (!targetPlayer) { ResetState(); return; }
            State.Value = GrabQTEState.Throwing;
            Vector3 away = (targetPlayer.transform.position - transform.position).normalized;
            Vector3 impulse = away * throwForward + Vector3.up * throwUp;
            targetPlayer.EnterRagdollServer(away, throwForward, throwUp);
            targetPlayer.DealDamageServerRpc(Mathf.RoundToInt(Mathf.Lerp(18f, 42f, difficulty)));
            Invoke(nameof(ResetState), 1f);
        }

        [Server]
        void ResetState()
        {
            State.Value = GrabQTEState.Idle;
            WarningRemaining.Value = 0f;
            targetPlayer = null;
        }

        public bool IsWarning => State.Value == GrabQTEState.Warning || State.Value == GrabQTEState.Grabbed;
        public float WarningIntensity => IsWarning ? 1f - Mathf.Clamp01(WarningRemaining.Value / Mathf.Max(0.01f, baseWarning)) : 0f;
    }
}
