using FishNet.Object;
using UnityEngine;
using DeadCircuit.Networking;

namespace DeadCircuit.AI
{
    public class DeadCircuitMonster : NetworkBehaviour
    {
        public enum BrainState { Dormant, Patrol, Investigate, Chase, Search, Stunned }
        [SerializeField] float hearingRadius = 12f;
        [SerializeField] float visionRadius = 22f;
        [SerializeField] float chaseSpeed = 5.8f;
        [SerializeField] float patrolSpeed = 2.2f;
        [SerializeField] float searchDuration = 4f;
        [SerializeField] float attackRange = 1.6f;
        [SerializeField] int damage = 35;
        [SerializeField] LayerMask sightMask = ~0;

        BrainState state = BrainState.Dormant;
        Transform target;
        Vector3 investigatePoint;
        float searchTimer;
        float nextThink;

        [ServerCallback]
        void Update()
        {
            if (!IsServerStarted) return;
            if (Time.time >= nextThink)
            {
                nextThink = Time.time + 0.15f;
                Think();
            }
            MoveBrain();
        }

        [Server]
        void Think()
        {
            DeadCircuitPlayer candidate = FindNearestPlayer();
            if (candidate != null && CanSee(candidate.transform))
            {
                target = candidate.transform;
                state = BrainState.Chase;
                return;
            }
            if (state == BrainState.Chase)
            {
                state = BrainState.Search;
                investigatePoint = transform.position;
                searchTimer = searchDuration;
            }
            else if (state == BrainState.Search)
            {
                searchTimer -= 0.15f;
                if (searchTimer <= 0f) state = BrainState.Patrol;
            }
            else if (state == BrainState.Dormant) state = BrainState.Patrol;
        }

        [Server]
        void MoveBrain()
        {
            Vector3 destination = transform.position;
            float speed = patrolSpeed;
            if (state == BrainState.Chase && target != null)
            {
                destination = target.position;
                speed = chaseSpeed;
                if (Vector3.Distance(transform.position, destination) <= attackRange)
                {
                    var player = target.GetComponent<DeadCircuitPlayer>();
                    if (player != null) player.DealDamageServerRpc(damage);
                }
            }
            else if (state == BrainState.Search) destination = investigatePoint;
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            Vector3 look = destination - transform.position;
            if (look.sqrMagnitude > 0.01f) transform.forward = Vector3.Slerp(transform.forward, look.normalized, 0.12f);
        }

        [Server]
        DeadCircuitPlayer FindNearestPlayer()
        {
            DeadCircuitPlayer best = null;
            float bestDistance = visionRadius;
            foreach (DeadCircuitPlayer player in FindObjectsByType<DeadCircuitPlayer>(FindObjectsSortMode.None))
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < bestDistance && !player.Downed.Value)
                {
                    best = player;
                    bestDistance = distance;
                }
            }
            return best;
        }

        [Server]
        bool CanSee(Transform candidate)
        {
            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 direction = (candidate.position + Vector3.up * 0.9f) - origin;
            if (direction.magnitude > visionRadius) return false;
            if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, visionRadius, sightMask))
                return hit.transform == candidate || hit.transform.IsChildOf(candidate);
            return false;
        }

        [Server]
        public void HearNoise(Vector3 position)
        {
            if (Vector3.Distance(transform.position, position) > hearingRadius) return;
            investigatePoint = position;
            state = BrainState.Investigate;
        }
    }
}
