using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using DeadCircuit.Networking;

namespace DeadCircuit.Combat
{
    public class GrabQTE : NetworkBehaviour
    {
        public enum GrabState { None, Grabbed, ReleaseWindow, Escaped, Thrown }
        public readonly SyncVar<GrabState> State = new(GrabState.None);
        public readonly SyncVar<float> WindowRemaining = new(0f);

        [SerializeField] float baseEscapeDistance = 4f;
        [SerializeField] float baseReleaseWindow = 1f;
        Transform holder;

        public bool CanBreakFree => State.Value == GrabState.Grabbed || State.Value == GrabState.ReleaseWindow;
        public bool CanRunAway => State.Value == GrabState.ReleaseWindow;

        [Server]
        public bool TryStart(DeadCircuitPlayer player, float intensity)
        {
            if (player == null || State.Value != GrabState.None) return false;
            holder = GetComponentInParent<DeadCircuit.AI.DeadCircuitMonster>()?.transform;
            if (holder == null) return false;
            float clamped = Mathf.Clamp01(intensity);
            State.Value = GrabState.Grabbed;
            WindowRemaining.Value = Mathf.Lerp(baseReleaseWindow + 0.25f, baseReleaseWindow - 0.25f, clamped);
            return true;
        }

        [Server]
        public void BeginGrab(Transform monster)
        {
            if (monster == null || State.Value != GrabState.None) return;
            holder = monster;
            State.Value = GrabState.Grabbed;
            WindowRemaining.Value = baseReleaseWindow;
        }

        [ServerCallback]
        void Update()
        {
            if (State.Value == GrabState.None) return;
            if (holder == null) { EscapeServer(); return; }

            if (State.Value == GrabState.Grabbed || State.Value == GrabState.ReleaseWindow)
            {
                if (Vector3.Distance(transform.position, holder.position) > baseEscapeDistance)
                {
                    EscapeServer();
                    return;
                }
            }

            if (State.Value == GrabState.Grabbed)
            {
                WindowRemaining.Value = Mathf.Max(0f, WindowRemaining.Value - Time.deltaTime);
                if (WindowRemaining.Value <= 0f) OpenReleaseWindow();
            }
            else if (State.Value == GrabState.ReleaseWindow)
            {
                WindowRemaining.Value = Mathf.Max(0f, WindowRemaining.Value - Time.deltaTime);
                if (WindowRemaining.Value <= 0f) State.Value = GrabState.Thrown;
            }
        }

        [Server]
        public void OpenReleaseWindow()
        {
            State.Value = GrabState.ReleaseWindow;
            WindowRemaining.Value = baseReleaseWindow;
        }

        [ServerRpc]
        public void BreakFreeServerRpc()
        {
            if (CanBreakFree) EscapeServer();
        }

        [ServerRpc]
        public void RunAwayServerRpc(Vector3 playerPosition)
        {
            if (!CanRunAway || holder == null) return;
            if (Vector3.Distance(playerPosition, holder.position) > baseEscapeDistance) EscapeServer();
        }

        [ServerRpc]
        public void CounterAttackServerRpc(int damage)
        {
            if (!CanBreakFree || holder == null) return;
            var monster = holder.GetComponent<DeadCircuit.AI.DeadCircuitMonster>();
            EscapeServer();
            if (monster != null) monster.StunAndKnockback(transform.position, Mathf.Max(1, damage), 1.6f);
        }

        [Server]
        void EscapeServer()
        {
            State.Value = GrabState.Escaped;
            WindowRemaining.Value = 0f;
            holder = null;
            Invoke(nameof(ResetState), 0.25f);
        }

        [Server]
        void ResetState() => State.Value = GrabState.None;
    }
}
