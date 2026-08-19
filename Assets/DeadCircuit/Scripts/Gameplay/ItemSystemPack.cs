using FishNet.Object;
using UnityEngine;

namespace DeadCircuit.Gameplay
{
    public enum DeadCircuitItemType { Medkit, Battery, Flare, NoiseMaker, Armor, Adrenaline, Food, KeyItem }

    public class ItemSystemPack : NetworkBehaviour
    {
        [SerializeField] DeadCircuitItemType itemType;
        [SerializeField] int uses = 1;
        [SerializeField] float throwImpulse = 8f;
        [SerializeField] float impactThreshold = 5f;
        [SerializeField] Rigidbody body;
        [SerializeField] Collider pickupCollider;
        [SerializeField] bool canBeThrown = true;

        public bool IsHeld { get; private set; }
        public DeadCircuitItemType ItemType => itemType;

        [ServerRpc]
        public void PickupServerRpc()
        {
            if (uses <= 0) return;
            IsHeld = true;
            if (body != null) body.isKinematic = true;
        }

        [ServerRpc]
        public void DropServerRpc(Vector3 velocity)
        {
            IsHeld = false;
            if (body != null)
            {
                body.isKinematic = false;
                body.velocity = velocity;
            }
        }

        [ServerRpc]
        public void ThrowServerRpc(Vector3 direction, float strength)
        {
            if (!canBeThrown) return;
            IsHeld = false;
            if (body == null) return;
            body.isKinematic = false;
            body.AddForce(direction.normalized * throwImpulse * Mathf.Clamp01(strength), ForceMode.Impulse);
        }

        [Server]
        public bool Consume()
        {
            if (uses <= 0) return false;
            uses--;
            return true;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!IsServerStarted || body == null || collision.relativeVelocity.magnitude < impactThreshold) return;
            var monster = collision.collider.GetComponentInParent<DeadCircuit.AI.DeadCircuitMonster>();
            if (monster == null) return;
            float force = collision.relativeVelocity.magnitude;
            monster.StunAndKnockback(transform.position, Mathf.RoundToInt(force * 3f), Mathf.Clamp(force * 0.12f, 0.5f, 2.5f), Mathf.Clamp(force * 0.8f, 2f, 10f));
        }
    }
}
