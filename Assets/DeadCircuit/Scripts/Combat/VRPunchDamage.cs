using FishNet.Object;
using UnityEngine;

namespace DeadCircuit.Combat
{
    /// <summary>
    /// Call PunchServerRpc when the player's hand physically connects with a target.
    /// Damage scales with measured hand impact speed, then clamps to safe limits.
    /// </summary>
    public class VRPunchDamage : NetworkBehaviour
    {
        [SerializeField] float minimumSpeed = 1.25f;
        [SerializeField] float fullPowerSpeed = 6.5f;
        [SerializeField] float minimumDamage = 6f;
        [SerializeField] float maximumDamage = 45f;
        [SerializeField] float maxKnockback = 4.5f;
        [SerializeField] float hitCooldown = 0.18f;

        float localCooldown;

        public void TryPunch(float handSpeed, Vector3 hitDirection, DeadCircuit.Networking.DeadCircuitPlayer target)
        {
            if (!IsOwner || localCooldown > 0f || target == null) return;
            localCooldown = hitCooldown;

            float normalized = Mathf.InverseLerp(minimumSpeed, fullPowerSpeed, Mathf.Abs(handSpeed));
            float damage = Mathf.Lerp(minimumDamage, maximumDamage, normalized);
            float knockback = Mathf.Lerp(0.35f, maxKnockback, normalized);

            PunchServerRpc(target, damage, hitDirection.sqrMagnitude > 0.001f ? hitDirection.normalized : transform.forward, knockback);
        }

        void Update()
        {
            localCooldown = Mathf.Max(0f, localCooldown - Time.deltaTime);
        }

        [ServerRpc]
        void PunchServerRpc(DeadCircuit.Networking.DeadCircuitPlayer target, float damage, Vector3 direction, float knockback)
        {
            if (target == null || target.Downed.Value) return;
            target.DealImpactServerRpc(Mathf.RoundToInt(Mathf.Clamp(damage, minimumDamage, maximumDamage)), direction, knockback);
        }
    }
}
