using FishNet.Object;
using UnityEngine;
using DeadCircuit.AI;

namespace DeadCircuit.Physics
{
    public class SuperhumanPhysics : NetworkBehaviour
    {
        [SerializeField] float maxPickupMass = 250f;
        [SerializeField] float baseLiftTime = 0.35f;
        [SerializeField] float heavyLiftTime = 2.0f;
        [SerializeField] float throwForce = 18f;

        Rigidbody heldBody;
        float liftTimer;
        float requiredLiftTime;
        bool lifting;

        public bool IsLifting => lifting;
        public float LiftProgress => requiredLiftTime <= 0f ? 0f : Mathf.Clamp01(liftTimer / requiredLiftTime);

        public void TryPickup(Rigidbody body)
        {
            if (!IsOwner || body == null || lifting || body.mass > maxPickupMass) return;
            RequestPickupServerRpc(body.GetComponent<NetworkObject>());
        }

        [ServerRpc]
        void RequestPickupServerRpc(NetworkObject target)
        {
            if (target == null) return;
            Rigidbody body = target.GetComponent<Rigidbody>();
            if (body == null || body.mass > maxPickupMass || lifting) return;
            heldBody = body;
            requiredLiftTime = Mathf.Lerp(baseLiftTime, heavyLiftTime, Mathf.InverseLerp(5f, maxPickupMass, body.mass));
            liftTimer = 0f;
            lifting = true;
            body.isKinematic = false;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        void Update()
        {
            if (!IsOwner || !lifting || heldBody == null) return;
            liftTimer += Time.deltaTime;
            if (liftTimer >= requiredLiftTime)
            {
                heldBody.velocity = Vector3.zero;
                heldBody.angularVelocity = Vector3.zero;
            }
        }

        public void ThrowHeld(Vector3 direction)
        {
            if (!IsOwner || !lifting || heldBody == null || LiftProgress < 0.8f) return;
            RequestThrowServerRpc(direction.normalized, LiftProgress);
        }

        [ServerRpc]
        void RequestThrowServerRpc(Vector3 direction, float progress)
        {
            if (!lifting || heldBody == null) return;
            Rigidbody body = heldBody;
            heldBody = null;
            lifting = false;
            body.AddForce(direction * throwForce * Mathf.Lerp(0.55f, 1.5f, Mathf.Clamp01(progress)), ForceMode.Impulse);
        }

        public void DropHeld()
        {
            heldBody = null;
            lifting = false;
        }
    }
}
