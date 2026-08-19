using FishNet.Object;
using UnityEngine;

namespace DeadCircuit.Combat
{
    public class VRChargeAttack : NetworkBehaviour
    {
        [SerializeField] float chargeMinSpeed = 2.8f;
        [SerializeField] float chargeMaxSpeed = 7.5f;
        [SerializeField] float maxChargeDuration = 1.35f;
        [SerializeField] float hitRange = 1.8f;
        [SerializeField] float damageMin = 18f;
        [SerializeField] float damageMax = 65f;
        [SerializeField] float knockbackMin = 2.5f;
        [SerializeField] float knockbackMax = 9f;
        [SerializeField] float cooldown = 1.1f;
        [SerializeField] LayerMask hittableLayers = ~0;

        Vector3 startPosition;
        float chargeTimer;
        float nextCharge;
        bool charging;

        public bool IsCharging => charging;
        public float Charge01 => Mathf.Clamp01(chargeTimer / maxChargeDuration);

        void Update()
        {
            if (!IsOwner || !charging) return;
            chargeTimer += Time.deltaTime;
            if (chargeTimer >= maxChargeDuration) ReleaseCharge();
        }

        public void BeginCharge()
        {
            if (!IsOwner || Time.time < nextCharge || charging) return;
            charging = true;
            chargeTimer = 0f;
            startPosition = transform.position;
        }

        public void ReleaseCharge()
        {
            if (!IsOwner || !charging) return;
            charging = false;
            nextCharge = Time.time + cooldown;
            float speed = Vector3.Distance(transform.position, startPosition) / Mathf.Max(0.05f, chargeTimer);
            float intensity = Mathf.InverseLerp(chargeMinSpeed, chargeMaxSpeed, speed);
            RequestChargeServerRpc(Mathf.Clamp01(intensity), transform.forward);
        }

        [ServerRpc]
        void RequestChargeServerRpc(float intensity, Vector3 forward)
        {
            if (intensity <= 0f) return;
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            if (!Physics.SphereCast(origin, 0.45f, forward.normalized, out RaycastHit hit, hitRange, hittableLayers)) return;
            var monster = hit.collider.GetComponentInParent<DeadCircuit.AI.DeadCircuitMonster>();
            if (monster == null) return;
            int damage = Mathf.RoundToInt(Mathf.Lerp(damageMin, damageMax, intensity));
            float force = Mathf.Lerp(knockbackMin, knockbackMax, intensity);
            monster.StunAndKnockback(transform.position, damage, 1.1f + intensity * 1.4f, force);
            if (intensity > 0.8f) monster.ApplyPhysicsKnockback(forward.normalized, force);
        }
    }
}
