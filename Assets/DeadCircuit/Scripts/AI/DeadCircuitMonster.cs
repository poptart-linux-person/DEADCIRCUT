using FishNet.Object;
using UnityEngine;
using DeadCircuit.Networking;
using DeadCircuit.Combat;

namespace DeadCircuit.AI
{
    public class DeadCircuitMonster : NetworkBehaviour
    {
        public enum BrainState { Dormant, Patrol, Investigate, Chase, Search, Stunned, Executing, Ragdolled }

        [Header("Sight")]
        [SerializeField] float visionRadius = 22f;
        [SerializeField, Range(30f, 180f)] float fieldOfView = 105f;
        [SerializeField] float eyeHeight = 1.45f;
        [SerializeField] LayerMask sightMask = ~0;
        [SerializeField] float visibleTargetHold = 0.65f;

        [Header("Light Detection")]
        [SerializeField] float lightAwarenessRadius = 20f;
        [SerializeField] float lightInvestigationBias = 0.75f;

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
        [SerializeField] DynamicGrabQTE grabQTE;

        BrainState state = BrainState.Dormant;
        Transform target;
        Vector3 lastKnownPosition;
        Vector3 investigatePoint;
        float searchTimer;
        float nextThink;
        float nextAttack;
        float targetVisibleUntil;
        float stunnedUntil;
        bool hasHeardSomething;

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
            if (state == BrainState.Stunned)
            {
                if (Time.time >= stunnedUntil) state = BrainState.Chase;
                return;
            }
            if (state == BrainState.Ragdolled)
            {
                if (Time.time >= stunnedUntil) RecoverFromRagdoll();
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

            if (LightLureSystem.TryGetBestSignal(transform.position, out LightSignal light))
            {
                float distance = Vector3.Distance(transform.position, light.Position);
                if (distance <= lightAwarenessRadius && light.Intensity * lightInvestigationBias > 0.1f)
                {
                    investigatePoint = light.Position;
                    state = BrainState.Investigate;
                    hasHeardSomething = true;
                }
            }

            if (state == BrainState.Investigate && Vector3.Distance(transform.position, investigatePoint) <= 0.8f)
            {
                state = BrainState.Search;
                searchTimer = searchDuration;
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
                                if (grabQTE != null && player.Health.Value <= executionHealthThreshold)
                                {
                                    float intensity = 1f - Mathf.Clamp01(player.Health.Value / (float)Mathf.Max(1, executionHealthThreshold));
                                    if (grabQTE.TryStart(player, intensity))
                                    {
                                        nextAttack = Time.time + 2f;
                                        return;
                                    }
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
        DeadCircuitPlayer FindVisiblePlayer()
        {
            DeadCircuitPlayer best = null;
            float bestDistance = visionRadius;
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            foreach (DeadCircuitPlayer player in FindObjectsByType<DeadCircuitPlayer>(FindObjectsSortMode.None))
            {
                if (player == null || player.Downed.Value) continue;
                Vector3 direction = (player.transform.position + Vector3.up * 0.9f) - eye;
                float distance = direction.magnitude;
                if (distance > bestDistance) continue;
                if (Vector3.Angle(transform.forward, direction) > fieldOfView * 0.5f) continue;
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
        public void HearNoise(Vector3 position) => HearNoise(position, 0.5f, NoiseType.Impact);

        [Server]
        public void StunAndKnockback(Vector3 fromPosition, int damageAmount, float duration, float force)
        {
            target = null;
            state = BrainState.Stunned;
            stunnedUntil = Time.time + Mathf.Max(0.2f, duration);
            Vector3 away = transform.position - fromPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            away.Normalize();
            transform.position += away * Mathf.Min(2.5f, Mathf.Max(0.5f, force * 0.2f));
            transform.rotation = Quaternion.LookRotation(-away, Vector3.up);
        }

        [Server]
        public void StunAndKnockback(Vector3 fromPosition, int damageAmount, float duration) => StunAndKnockback(fromPosition, damageAmount, duration, 4f);

        [Server]
        public void ApplyPhysicsKnockback(Vector3 direction, float force)
        {
            direction.y = Mathf.Clamp(direction.y + 0.35f, 0f, 1.5f);
            direction.Normalize();
            transform.position += direction * Mathf.Min(3f, force * 0.18f);
            if (force >= 2.2f)
            {
                state = BrainState.Ragdolled;
                stunnedUntil = Time.time + Mathf.Lerp(0.9f, 2.4f, Mathf.InverseLerp(2.2f, 3.5f, force));
            }
            else
            {
                state = BrainState.Stunned;
                stunnedUntil = Time.time + 0.65f;
            }
        }

        [Server]
        void RecoverFromRagdoll() => state = BrainState.Search;
    }
}
