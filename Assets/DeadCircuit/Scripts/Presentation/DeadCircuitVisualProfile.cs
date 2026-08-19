using UnityEngine;

namespace DeadCircuit.Presentation
{
    [DisallowMultipleComponent]
    public class DeadCircuitVisualProfile : MonoBehaviour
    {
        [Header("Atmosphere")]
        [SerializeField] Color fogColor = new Color(0.025f, 0.03f, 0.04f);
        [SerializeField] float fogDensity = 0.012f;
        [SerializeField] Color ambientColor = new Color(0.055f, 0.06f, 0.075f);
        [SerializeField] float ambientIntensity = 0.7f;
        [SerializeField] float reflectionIntensity = 0.65f;
        [SerializeField] float shadowDistance = 70f;

        [Header("Quality")]
        [SerializeField] bool useVSync = false;
        [SerializeField] int targetFrameRate = 90;
        [SerializeField] int antiAliasing = 4;

        void Awake()
        {
            Apply();
        }

        [ContextMenu("Apply Visual Profile")]
        public void Apply()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.ambientIntensity = ambientIntensity;
            RenderSettings.reflectionIntensity = reflectionIntensity;
            QualitySettings.shadowDistance = shadowDistance;
            QualitySettings.antiAliasing = Mathf.Clamp(antiAliasing, 0, 8);
            QualitySettings.vSyncCount = useVSync ? 1 : 0;
            Application.targetFrameRate = Mathf.Max(30, targetFrameRate);
        }
    }
}
