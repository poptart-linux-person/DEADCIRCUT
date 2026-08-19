using UnityEngine;
using DeadCircuit.Combat;

namespace DeadCircuit.Gameplay
{
    /// <summary>
    /// Disables electric tools and powered lights when the shared grid is down.
    /// Attach to a scene object alongside PowerGrid.
    /// </summary>
    public class PoweredEquipment : MonoBehaviour
    {
        [SerializeField] PowerGrid grid;
        [SerializeField] ElectricTool[] electricTools;
        [SerializeField] Light[] poweredLights;
        [SerializeField] Behaviour[] poweredBehaviours;

        bool lastPowered = true;

        void Awake()
        {
            if (grid == null) grid = Object.FindObjectOfType<PowerGrid>();
        }

        void Update()
        {
            bool powered = grid == null || grid.CanUsePoweredEquipment();
            if (powered == lastPowered) return;
            lastPowered = powered;

            if (electricTools != null)
            {
                foreach (ElectricTool tool in electricTools)
                {
                    if (tool != null) tool.SetPowered(powered);
                }
            }

            if (poweredLights != null)
            {
                foreach (Light light in poweredLights)
                {
                    if (light != null) light.enabled = powered;
                }
            }

            if (poweredBehaviours != null)
            {
                foreach (Behaviour behaviour in poweredBehaviours)
                {
                    if (behaviour != null) behaviour.enabled = powered;
                }
            }
        }
    }
}
