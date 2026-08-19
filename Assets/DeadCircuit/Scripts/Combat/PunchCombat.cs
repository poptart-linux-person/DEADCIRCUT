using FishNet.Object;
using UnityEngine;

namespace DeadCircuit.Combat
{
    public class PunchCombat : NetworkBehaviour
    {
        [SerializeField] float range = 1.35f;
        [SerializeField] float radius = 0.28f;
        [SerializeField] int damage = 32;
        [SerializeField] float knockback = 4.5f;
        [SerializeField] float cooldown = 0.28f;
        float nextAttack;

        public void Punch(Vector3 origin, Vector3 direction)
        {
            if (!IsOwner || Time.time < nextAttack) return;
            nextAttack = Time.time + cooldown;
            PunchServerRpc(origin, direction.normalized);
        }

        [ServerRpc]
        void PunchServerRpc(Vector3 origin, Vector3 direction)
        {
            Vector3 center = origin + direction * (range * 0.5f);
            Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider hit in hits)
            {
                var monster = hit.GetComponentInParent<DeadCircuit.AI.DeadCircuitMonster>();
                if (monster != null && monster.gameObject != gameObject)
                {
                    monster.StunAndKnockback(transform.position, damage, 0.75f, knockback);
                    if (knockback >= 2.2f)
                        monster.ApplyPhysicsKnockback(direction, knockback);
                    break;
                }

                var player = hit.GetComponentInParent<DeadCircuit.Networking.DeadCircuitPlayer>();
                if (player != null && player.gameObject != gameObject)
                {
                    player.DealImpactServerRpc(damage, direction, knockback);
                    break;
                }
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position + transform.forward * (range * 0.5f), radius);
        }
#endif
    }
}
