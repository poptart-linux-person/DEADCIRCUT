using FishNet.Object;
using UnityEngine;
using DeadCircuit.AI;

namespace DeadCircuit.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsThrowable : NetworkBehaviour
    {
        [SerializeField] float minImpactSpeed = 5f;
        [SerializeField] float maxImpactSpeed = 18f;
        [SerializeField] float maxStun = 2.5f;
        [SerializeField] float maxKnockback = 3.5f;
        Rigidbody body;
        Vector3 lastVelocity;

        void Awake() => body = GetComponent<Rigidbody>();

        void FixedUpdate() { if (body != null) lastVelocity = body.linearVelocity; }

        void OnCollisionEnter(Collision collision)
        {
            if (!IsServerStarted) return;
            float speed = Mathf.Max(lastVelocity.magnitude, collision.relativeVelocity.magnitude);
            if (speed < minImpactSpeed) return;
            var monster = collision.collider.GetComponentInParent<DeadCircuitMonster>();
            if (monster == null) return;
            float t = Mathf.InverseLerp(minImpactSpeed, maxImpactSpeed, speed);
            monster.StunAndKnockback(transform.position, Mathf.RoundToInt(Mathf.Lerp(15f, 90f, t)), Mathf.Lerp(0.45f, maxStun, t));
            Vector3 away = (monster.transform.position - transform.position).normalized;
            monster.ApplyPhysicsKnockback(away, Mathf.Lerp(0.5f, maxKnockback, t));
        }
    }

    public class ThrowablePickup : NetworkBehaviour
    {
        [SerializeField] Transform holdPoint;
        Rigidbody body;
        FixedJoint joint;

        void Awake() => body = GetComponent<Rigidbody>();

        [Server]
        public void Pickup(NetworkObject holder, Vector3 worldHoldPoint)
        {
            if (body == null || holder == null || joint != null) return;
            body.isKinematic = false;
            transform.position = worldHoldPoint;
            joint = gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = holder.GetComponent<Rigidbody>();
        }

        [Server]
        public void Throw(Vector3 velocity)
        {
            if (joint != null) Destroy(joint);
            joint = null;
            body.isKinematic = false;
            body.linearVelocity = velocity;
        }
    }
}
