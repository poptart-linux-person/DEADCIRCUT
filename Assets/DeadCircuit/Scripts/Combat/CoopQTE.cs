using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using DeadCircuit.Networking;

namespace DeadCircuit.Combat
{
    public class CoopQTE : NetworkBehaviour
    {
        public readonly SyncVar<bool> Active = new(false);
        public readonly SyncVar<int> Helpers = new(0);
        public readonly SyncVar<float> Stability = new(1f);

        [SerializeField] float helpRange = 3.5f;
        [SerializeField] float helperGain = 0.22f;
        [SerializeField] float attackGain = 0.32f;
        [SerializeField] float runBreakDistance = 5f;

        DeadCircuitPlayer victim;
        DeadCircuit.AI.DeadCircuitMonster monster;

        [Server]
        public void Begin(DeadCircuitPlayer player, DeadCircuit.AI.DeadCircuitMonster source)
        {
            if (player == null || source == null) return;
            victim = player;
            monster = source;
            Active.Value = true;
            Helpers.Value = 0;
            Stability.Value = 1f;
        }

        [ServerCallback]
        void Update()
        {
            if (!Active.Value || victim == null || monster == null) return;
            if (Vector3.Distance(victim.transform.position, monster.transform.position) > runBreakDistance)
            {
                Complete(false);
            }
        }

        [ServerRpc]
        public void AssistServerRpc(DeadCircuitPlayer helper)
        {
            if (!Active.Value || helper == null || victim == helper || monster == null) return;
            if (Vector3.Distance(helper.transform.position, victim.transform.position) > helpRange) return;
            Helpers.Value = Mathf.Min(4, Helpers.Value + 1);
            Stability.Value = Mathf.Max(0f, Stability.Value - helperGain);
            if (Stability.Value <= 0f) Complete(true);
            else monster.StunAndKnockback(helper.transform.position, Mathf.RoundToInt(35f), 0.8f);
        }

        [ServerRpc]
        public void StrikeServerRpc(DeadCircuitPlayer helper, int damage)
        {
            if (!Active.Value || helper == null || monster == null) return;
            if (Vector3.Distance(helper.transform.position, victim.transform.position) > helpRange + 1f) return;
            Stability.Value = Mathf.Max(0f, Stability.Value - helperGain - Mathf.Clamp01(damage / 100f) * attackGain);
            monster.StunAndKnockback(helper.transform.position, Mathf.Max(1, damage), 1.1f);
            if (Stability.Value <= 0f) Complete(true);
        }

        [Server]
        void Complete(bool rescued)
        {
            Active.Value = false;
            if (victim != null)
            {
                if (rescued)
                    victim.ReleaseFromQTEServer();
                else
                    victim.EnterRagdollServer((victim.transform.position - monster.transform.position).normalized, 6f, 3f);
            }
            victim = null;
            monster = null;
        }
    }
}
