using FishNet.Object;
using UnityEngine;

namespace DeadCircuit.Combat
{
    public class DeathRagdoll : NetworkBehaviour
    {
        [SerializeField] Rigidbody[] ragdollBodies;
        [SerializeField] Collider[] ragdollColliders;
        [SerializeField] Animator animator;
        [SerializeField] CharacterController characterController;

        public override void OnStartClient()
        {
            base.OnStartClient();
            SetRagdoll(false);
        }

        [ObserversRpc]
        public void EnableRagdollObserversRpc(Vector3 impulse, Vector3 hitPoint)
        {
            SetRagdoll(true);
            if (characterController != null) characterController.enabled = false;
            Rigidbody nearest = null;
            float best = float.MaxValue;
            foreach (var rb in ragdollBodies)
            {
                float d = Vector3.SqrMagnitude(rb.worldCenterOfMass - hitPoint);
                if (d < best) { best = d; nearest = rb; }
            }
            if (nearest != null) nearest.AddForceAtPosition(impulse, hitPoint, ForceMode.Impulse);
        }

        void SetRagdoll(bool enabled)
        {
            if (animator != null) animator.enabled = !enabled;
            if (characterController != null) characterController.enabled = !enabled;
            foreach (var rb in ragdollBodies) rb.isKinematic = !enabled;
            foreach (var col in ragdollColliders) col.enabled = enabled;
        }
    }
}
