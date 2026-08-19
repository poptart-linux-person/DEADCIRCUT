using FishNet.Object;
using UnityEngine;
using DeadCircuit.Networking;

namespace DeadCircuit.AI
{
    public enum MonsterCombatState { Ready, Blocking, Parrying, Dropkicking, Recovering }

    public sealed class MonsterCombatAbilities : NetworkBehaviour
    {
        public MonsterCombatState State { get; private set; } = MonsterCombatState.Ready;

        [SerializeField] float blockDuration = 0.9f;
        [SerializeField] float parryWindow = 0.22f;
        [SerializeField] float kickRange = 2.1f;
        [SerializeField] float kickForce = 7f;
        [SerializeField] float kickCooldown = 2.2f;
        [SerializeField] int kickDamage = 18;
        [SerializeField] float recoveryTime = 0.85f;

        float stateUntil;
        float nextKick;
        Transform focusedTarget;

        [ServerCallback]
        void Update()
        {
            if (State != MonsterCombatState.Ready && Time.time >= stateUntil)
                State = MonsterCombatState.Ready;
        }

        [Server]
        public bool TryBlock(float threat)
        {
            if (State != MonsterCombatState.Ready) return false;
            if (threat < 0.35f) return false;
            State = MonsterCombatState.Blocking;
            stateUntil = Time.time + blockDuration;
            return true;
        }

        [Server]
        public bool TryParry(float threat)
        {
            if (State != MonsterCombatState.Ready) return false;
            if (threat < 0.65f) return false;
            State = MonsterCombatState.Parrying;
            stateUntil = Time.time + parryWindow;
            return true;
        }

        [Server]
        public bool TryDropkick(Transform target)
        {
            if (target == null || State != MonsterCombatState.Ready || Time.time < nextKick) return false;
            if (Vector3.Distance(transform.position, target.position) > kickRange) return false;
            focusedTarget = target;
            State = MonsterCombatState.Dropkicking;
            stateUntil = Time.time + 0.42f;
            nextKick = Time.time + kickCooldown;
            Invoke(nameof(ResolveDropkick), 0.25f);
            return true;
        }

        [Server]
        void ResolveDropkick()
        {
            if (State != MonsterCombatState.Dropkicking || focusedTarget == null) return;
            var player = focusedTarget.GetComponent<DeadCircuitPlayer>();
            if (player != null)
                player.DealDamageServerRpc(kickDamage);
            focusedTarget = null;
            State = MonsterCombatState.Recovering;
            stateUntil = Time.time + recoveryTime;
        }

        [Server]
        public bool TryParryIncoming(Transform attacker)
        {
            if (State != MonsterCombatState.Parrying || attacker == null || Time.time > stateUntil) return false;
            Vector3 away = (attacker.position - transform.position).normalized;
            transform.position -= away * 0.45f;
            State = MonsterCombatState.Recovering;
            stateUntil = Time.time + recoveryTime;
            return true;
        }
    }
}
