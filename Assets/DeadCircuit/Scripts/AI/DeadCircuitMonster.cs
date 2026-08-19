using FishNet.Object;
using UnityEngine;
using DeadCircuit.Networking;

namespace DeadCircuit.AI
{
    public class DeadCircuitMonster : NetworkBehaviour
    {
        public enum BrainState { Dormant, Patrol, Investigate, Chase, Search, Stunned, Executing }

        [Header("Sight")]
        [SerializeField] float visionRadius = 22f;
        [SerializeField, Range(30f, 180f)] float fieldOfView = 105f;
        [SerializeField] float eyeHeight = 1.45f;
        [SerializeField] LayerMask sightMask = ~0;
        [SerializeField] float visibleTargetHold = 0.65f;

        [Header("Hearing")]
        [SerializeField] float hearingRadius = 18f;
        [SerializeField] float investigationAccuracy = 2.5f;
        [SerializeField] float searchDuration = 6f;

        [Header("Movement")]
        [SerializeField] float chaseSpeed = 5.8f;
        [SerializeField] float investigateSpeed = 3.2f;
        [SerializeField] float patrolSpeed = 2.2f;
        [SerializeField] float turnSpeed = 7f;

        [Header("Attack")]
        [SerializeField] float attackRange = 1.6f;
        [SerializeField] float attackCooldown = 0.85f;
        [SerializeField] int damage = 35;
        [SerializeField] int executionHealthThreshold = 18;
        [SerializeField] float executionDuration = 0.75f;
        [SerializeField] float executionThrowUp = 3.5f;
        [SerializeField] float executionThrowForward = 8.5f;

        BrainState state = BrainState.Dormant;
        Transform target;
        Vector3 lastKnownPosition;
        Vector3 investigatePoint;
        float searchTimer;
        float nextThink;
        float nextAttack;
        float targetVisibleUntil;
        float executionTimer;
        bool hasHeardSomething;
        DeadCircuitPlayer executionTarget;

        public override void OnStartServer()
        {
            base.OnStartServer();
            DeadCircuitNoiseDirector.Register(this);
            state = BrainState.Patrol;
            lastKnownPosition = transform.position;
        }

        public override void OnStopServer()
        {
            DeadCircuitNoiseDirector.Unregister(this);
            base.OnStopServer();
        }

        [ServerCallback]
        void Update()
        {
            if (!IsServerStarted) return;

            if (state == BrainState.Executing)
            {
                UpdateExecution();
                return;
            }

            if (Time.time >= nextThink)
            {
                nextThink = Time.time + 0.12f;
                Think();
            }

            MoveBrain();
        }

        [Server]
        void Think()
        {
            DeadCircuitPlayer seen = FindVisiblePlayer();
            if (seen != null)
            {
                target = seen.transform;
                lastKnownPosition = target.position;
                targetVisibleUntil = Time.time + visibleTargetHold;
                state = BrainState.Chase;
                hasHeardSomething = false;
                return;
            }

            if (state == BrainState.Chase && Time.time > targetVisibleUntil)
            {
                state = BrainState.Search;
                investigatePoint = lastKnownPosition;
                searchTimer = searchDuration;
                return;
            }

            if (state == BrainState.Investigate)
            {
                if (Vector3.Distance(transform.position, investigatePoint) <= 0.8f)
                {
                    state = BrainState.Search;
                    searchTimer = searchDuration;
                }
                return;
            }

            if (state == BrainState.Search)
            {
                searchTimer -= 0.12f;
                if (searchTimer <= 0f && !hasHeardSomething)
                {
                    state = BrainState.Patrol;
                    target = null;
                }
                return;
            }

            if (state == BrainState.Dormant) state = BrainState.Patrol;
        }

        [Server]
        void MoveBrain()
        {
            Vector3 destination = transform.position;
            float speed = patrolSpeed;

            switch (state)
            {
                case BrainState.Chase:
                    if (target != null)
                    {
                        destination = target.position;
                        speed = chaseSpeed;
                        if (Vector3.Distance(transform.position, destination) <= attackRange && Time.time >= nextAttack)
                        {
                            var player = target.GetComponent<DeadCircuitPlayer>();
                            if (player != null)
                            {
                                if (player.Health.Value <= executionHealthThreshold)
                                {
                                    BeginExecution(player);
                                    return;
                                }
                                nextAttack = Time.time + attackCooldown;
                                player.DealDamageServerRpc(damage);
                            }
                        }
                    }
                    break;

                case BrainState.Investigate:
                    destination = investigatePoint;
                    speed = investigateSpeed;
                    break;

                case BrainState.Search:
                    destination = investigatePoint;
                    speed = investigateSpeed * 0.8f;
                    break;
            }

            Vector3 move = destination - transform.position;
            if (move.sqrMagnitude > 0.04f)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
                Quaternion desired = Quaternion.LookRotation(move.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desired, turnSpeed * Time.deltaTime);
            }
        }

        [Server]
        void BeginExecution(DeadCircuitPlayer player)
        {
            if (player == null || player.Downed.Value) return;
            executionTarget = player;
            state = BrainState.Executing;
            executionTimer = executionDuration;
            target = null;
        }

        [Server]
        void UpdateExecution()
        {
            if (executionTarget == null)
            {
                state = BrainState.Chase;
                return;
            }

            transform.LookAt(executionTarget.transform.position + Vector3.up * 0.9f);
            executionTimer -= Time.deltaTime;
            if (executionTimer > 0f) return;

            Vector3 away = (executionTarget.transform.position - transform.position).normalized;
            Vector3 impulse = away * executionThrowForward + Vector3.up * executionThrowUp;
            executionTarget.ExecuteAndThrowServer(impulse, executionTarget.transform.position + Vector3.up * 0.8f);
            executionTarget = null;
            state = BrainState.Chase;
            nextAttack = Time.time + 1.25f;
        }

        [Server]
        DeadCircuitPlayer FindVisiblePlayer()
        {
            DeadCircuitPlayer best = null;
            float bestDistance = visionRadius;
            Vector3 eye = transform.position + Vector3.up * eyeHeight;

            foreach (DeadCircuitPlayer player in FindObjectsByType<DeadCircuitPlayer>(FindObjectsSortMode.None))
            {
                if (player == null || player.Downed.Value) continue;

                Vector3 targetPoint = player.transform.position + Vector3.up * 0.9f;
                Vector3 direction = targetPoint - eye;
                float distance = direction.magnitude;
                if (distance > bestDistance) continue;

                float angle = Vector3.Angle(transform.forward, direction);
                if (angle > fieldOfView * 0.5f) continue;

                if (!Physics.Raycast(eye, direction.normalized, out RaycastHit hit, distance, sightMask)) continue;
                if (hit.transform != player.transform && !hit.transform.IsChildOf(player.transform)) continue;

                best = player;
                bestDistance = distance;
            }

            return best;
        }

        [Server]
        public void HearNoise(Vector3 position, float loudness, NoiseType type)
        {
            float distance = Vector3.Distance(transform.position, position);
            float effectiveRadius = hearingRadius * Mathf.Lerp(0.5f, 1.35f, Mathf.Clamp01(loudness));
            if (distance > effectiveRadius) return;

            float precision = investigationAccuracy * Mathf.Lerp(1.5f, 0.45f, Mathf.Clamp01(loudness));
            Vector2 offset = Random.insideUnitCircle * precision;
            investigatePoint = position + new Vector3(offset.x, 0f, offset.y);
            hasHeardSomething = true;
            target = null;
            state = BrainState.Investigate;
            searchTimer = searchDuration;
        }

        [Server]
        public void HearNoise(Vector3 position)
        {
            HearNoise(position, 0.5f, NoiseType.Impact);
        }
    }
}
