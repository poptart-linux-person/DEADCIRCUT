using UnityEngine;

namespace DeadCircuit.Presentation
{
    public class DeadCircuitPerformanceGovernor : MonoBehaviour
    {
        [SerializeField] int desktopTargetFps = 120;
        [SerializeField] int vrTargetFps = 90;
        [SerializeField] int lowFrameThreshold = 72;
        [SerializeField] int recoveryFrameThreshold = 88;
        [SerializeField] int sampleFrames = 45;
        [SerializeField] bool adaptiveShadows = true;

        int frameCounter;
        int lowSamples;
        int goodSamples;
        float frameStart;

        void Awake()
        {
            Application.targetFrameRate = Mathf.Max(30, vrTargetFps);
            QualitySettings.vSyncCount = 0;
            frameStart = Time.realtimeSinceStartup;
        }

        void Update()
        {
            frameCounter++;
            if (frameCounter < sampleFrames) return;

            float elapsed = Mathf.Max(0.001f, Time.realtimeSinceStartup - frameStart);
            float fps = frameCounter / elapsed;
            frameCounter = 0;
            frameStart = Time.realtimeSinceStartup;

            if (fps < lowFrameThreshold) { lowSamples++; goodSamples = 0; }
            else if (fps >= recoveryFrameThreshold) { goodSamples++; lowSamples = 0; }

            if (adaptiveShadows && lowSamples >= 2)
            {
                QualitySettings.shadowDistance = Mathf.Max(35f, QualitySettings.shadowDistance * 0.8f);
                QualitySettings.shadowCascades = 2;
                lowSamples = 0;
            }
            else if (adaptiveShadows && goodSamples >= 4)
            {
                QualitySettings.shadowDistance = Mathf.Min(70f, QualitySettings.shadowDistance * 1.05f);
                QualitySettings.shadowCascades = 4;
                goodSamples = 0;
            }
        }
    }
}
