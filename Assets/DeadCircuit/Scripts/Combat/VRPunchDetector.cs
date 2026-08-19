using UnityEngine;

namespace DeadCircuit.Combat
{
    public class VRPunchDetector : MonoBehaviour
    {
        [SerializeField] PunchCombat punchCombat;
        [SerializeField] Transform hand;
        [SerializeField] float punchSpeed = 2.8f;
        [SerializeField] float minimumForwardSpeed = 0.9f;
        [SerializeField] float sampleSmoothing = 0.08f;
        Vector3 lastPosition;
        Vector3 velocity;
        float nextPunch;

        void Awake()
        {
            if (hand == null) hand = transform;
            lastPosition = hand.position;
        }

        void Update()
        {
            Vector3 rawVelocity = (hand.position - lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            velocity = Vector3.Lerp(velocity, rawVelocity, 1f - Mathf.Exp(-Time.deltaTime / sampleSmoothing));
            lastPosition = hand.position;

            if (punchCombat == null || Time.time < nextPunch) return;
            float forwardSpeed = Vector3.Dot(velocity, hand.forward);
            if (velocity.magnitude >= punchSpeed && forwardSpeed >= minimumForwardSpeed)
            {
                nextPunch = Time.time + 0.22f;
                punchCombat.Punch(hand.position, hand.forward);
            }
        }
    }
}
