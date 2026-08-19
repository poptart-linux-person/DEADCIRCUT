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
                var player = hit.GetComponentInParent<DeadCircuit.Networking.DeadCircuitPlayer>();
                if (player == null || player.gameObject == gameObject) continue;
                player.DealDamageServerRpc(damage);
                Rigidbody body = player.GetComponent<Rigidbody>();
                if (body != null) body.AddForce((direction + Vector3.up * 0.2f) * knockback, ForceMode.Impulse);
                break;
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
