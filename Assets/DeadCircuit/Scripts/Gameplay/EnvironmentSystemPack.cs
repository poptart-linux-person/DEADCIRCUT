using UnityEngine;

namespace DeadCircuit.Gameplay
{
    public class EnvironmentSystemPack : MonoBehaviour
    {
        [Header("Doors")]
        [SerializeField] bool lockable;
        [SerializeField] bool barred;
        [SerializeField] bool breakable;
        [SerializeField] float breakHealth = 100f;
        float currentBreakHealth;

        [Header("Hiding")]
        [SerializeField] bool hideable;
        [SerializeField] Transform hidePoint;

        [Header("Trap")]
        [SerializeField] bool trapped;
        [SerializeField] float trapCooldown = 4f;
        float nextTrap;

        public bool IsLocked { get; private set; }
        public bool IsBroken { get; private set; }
        public bool IsOpen { get; private set; }

        void Awake() => currentBreakHealth = breakHealth;

        public bool TryUnlock() { if (!lockable || IsBroken) return false; IsLocked = false; return true; }
        public bool TryOpen()
        {
            if (IsBroken || IsOpen || IsLocked || barred) return false;
            IsOpen = true;
            return true;
        }
        public bool TryClose() { if (!IsOpen) return false; IsOpen = false; return true; }
        public void SetBarred(bool value) => barred = value;

        public void HoldAgainstPressure(float amount)
        {
            if (!breakable || IsBroken) return;
            currentBreakHealth -= Mathf.Abs(amount);
            if (currentBreakHealth <= 0f) Break();
        }

        public void DamageStructure(float amount)
        {
            if (!breakable || IsBroken) return;
            currentBreakHealth -= Mathf.Abs(amount);
            if (currentBreakHealth <= 0f) Break();
        }

        void Break()
        {
            IsBroken = true;
            IsOpen = true;
            IsLocked = false;
            barred = false;
        }

        public bool CanHide => hideable && !IsBroken && hidePoint != null;

        public bool TryTriggerTrap()
        {
            if (!trapped || Time.time < nextTrap) return false;
            nextTrap = Time.time + trapCooldown;
            return true;
        }
    }
}
