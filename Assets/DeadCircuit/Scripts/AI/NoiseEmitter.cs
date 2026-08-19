using FishNet.Object;
using UnityEngine;

namespace DeadCircuit.AI
{
    public class NoiseEmitter : NetworkBehaviour
    {
        [SerializeField] float footstepDistance = 0.45f;
        [SerializeField] float footstepMinSpeed = 1.2f;
        [SerializeField] float footstepMaxSpeed = 5.5f;
        [SerializeField] float footstepCooldown = 0.32f;
        [SerializeField] float voicePollInterval = 0.12f;
        [SerializeField] float voiceThreshold = 0.035f;
        [SerializeField] float voiceLoudnessMultiplier = 2.4f;
        [SerializeField] float voiceCooldown = 0.25f;

        Vector3 lastPosition;
        float footstepDistanceAccum;
        float nextFootstep;
        float nextVoicePoll;
        float nextVoiceNoise;
        AudioClip microphoneClip;
        string microphoneDevice;

        void Start()
        {
            lastPosition = transform.position;
            if (IsOwner && Microphone.devices.Length > 0)
            {
                microphoneDevice = Microphone.devices[0];
                microphoneClip = Microphone.Start(microphoneDevice, true, 1, 16000);
            }
        }

        void Update()
        {
            if (!IsOwner) return;
            float moved = Vector3.Distance(transform.position, lastPosition);
            lastPosition = transform.position;
            footstepDistanceAccum += moved;

            float speed = Time.deltaTime > 0f ? moved / Time.deltaTime : 0f;
            float speed01 = Mathf.InverseLerp(footstepMinSpeed, footstepMaxSpeed, speed);
            if (footstepDistanceAccum >= footstepDistance && Time.time >= nextFootstep && speed >= footstepMinSpeed)
            {
                footstepDistanceAccum = 0f;
                nextFootstep = Time.time + Mathf.Lerp(footstepCooldown, footstepCooldown * 0.55f, speed01);
                EmitNoiseServerRpc(Mathf.Lerp(0.22f, 0.52f, speed01), NoiseType.Footstep);
            }

            if (Time.time >= nextVoicePoll && microphoneClip != null)
            {
                nextVoicePoll = Time.time + voicePollInterval;
                int sampleRate = microphoneClip.frequency;
                int micPos = Microphone.GetPosition(microphoneDevice);
                if (micPos < 0) return;
                int sampleCount = Mathf.Min(256, micPos);
                if (sampleCount <= 0) return;
                float[] samples = new float[sampleCount];
                microphoneClip.GetData(samples, micPos - sampleCount);
                float sum = 0f;
                for (int i = 0; i < samples.Length; i++) sum += samples[i] * samples[i];
                float rms = Mathf.Sqrt(sum / Mathf.Max(1, samples.Length));
                if (rms >= voiceThreshold && Time.time >= nextVoiceNoise)
                {
                    nextVoiceNoise = Time.time + voiceCooldown;
                    float loudness = Mathf.Clamp01((rms - voiceThreshold) * voiceLoudnessMultiplier);
                    EmitNoiseServerRpc(Mathf.Clamp(loudness, 0.1f, 0.8f), NoiseType.Voice);
                }
            }
        }

        [ServerRpc]
        void EmitNoiseServerRpc(float loudness, NoiseType type)
        {
            DeadCircuitNoiseDirector.Emit(new NoiseEvent(transform.position, loudness, type));
        }
    }
}
