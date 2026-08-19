using FishNet.Object;
using UnityEngine;
using DeadCircuit.Networking;

namespace DeadCircuit.Combat
{
    public class GrabQTE : NetworkBehaviour
    {
        public enum GrabState { None, Grabbed, ReleaseWindow, Escaped, Thrown }
        public readonly SyncVar<GrabState> State = new(GrabState.None);
        public readonly SyncVar<float> WindowRemaining = new(0f);

        [SerializeField] float escapeDistance = 4.0f;
        [SerializeField] float releaseWindow = 1.0f;
        [SerializeField] float throwDamage = 24f;
        [SerializeField] float stunDuration = 1.6f;

        Transform holder;
        float lastDistance;

        public bool CanBreakFree => State.Value == GrabState.Grabbed || State.Value == GrabState.ReleaseWindow;
        public bool CanRunAway => State.Value == GrabState.ReleaseWindow;

        [Server]
        public void BeginGrab(Transform monster)
        {
            if (monster == null || State.Value != GrabState.None) return;
            holder = monster;
            State.Value = GrabState.Grabbed;
            WindowRemaining.Value = releaseWindow;
            lastDistance = Vector3.Distance(transform.position, monster.position);
        }

        [ServerCallback]
        void Update()
        {
            if (State.Value == GrabState.None || holder == null) return;

            float distance = Vector3.Distance(transform.position, holder.position);
            if (State.Value == GrabState.Grabbed && distance > escapeDistance)
            {
                EscapeServer();
                return;
            }

            if (State.Value == GrabState.ReleaseWindow)
            {
                WindowRemaining.Value = Mathf.Max(0f, WindowRemaining.Value - Time.deltaTime);
                if (distance > escapeDistance)
                {
                    EscapeServer();
                    return;
                }
                if (WindowRemaining.Value <= 0f)
                {
                    State.Value = GrabState.Thrown;
                    WindowRemaining.Value = 0f;
                }
            }

            lastDistance = distance;
        }

        [Server]
        public void OpenReleaseWindow()
        {
            if (State.Value != GrabState.Grabbed) return;
            State.Value = GrabState.ReleaseWindow;
            WindowRemaining.Value = releaseWindow;
        }

        [ServerRpc]
        public void BreakFreeServerRpc()
        {
            if (!CanBreakFree) return;
            EscapeServer();
        }

        [ServerRpc]
        public void RunAwayServerRpc()
        {
            if (!CanRunAway) return;
            if (holder == null || Vector3.Distance(transform.position, holder.position) > escapeDistance)
                EscapeServer();
        }

        [ServerRpc]
        public void CounterAttackServerRpc(int damage)
        {
            if (!CanBreakFree || holder == null) return;
            EscapeServer();
            var monster = holder.GetComponent<DeadCircuit.AI.DeadCircuitMonster>();
            if (monster != null) monster.StunAndKnockback(transform.position, Mathf.Max(1, damage), stunDuration);
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
