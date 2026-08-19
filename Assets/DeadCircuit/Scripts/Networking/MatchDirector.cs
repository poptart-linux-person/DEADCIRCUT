using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using DeadCircuit.Gameplay;

namespace DeadCircuit.Networking
{
    public class MatchDirector : NetworkBehaviour
    {
        public readonly SyncVar<MatchPhase> Phase = new(MatchPhase.Menu);
        public readonly SyncVar<int> Chapter = new(1);
        public readonly SyncVar<int> Night = new(0);
        public readonly SyncVar<float> TimeRemaining = new(0f);

        [SerializeField] float preparationTime = 120f;
        [SerializeField] float nightTime = 300f;

        public override void OnStartServer()
        {
            base.OnStartServer();
            Phase.Value = MatchPhase.Lobby;
            TimeRemaining.Value = 0f;
        }

        [Server]
        public void StartStory(int chapter)
        {
            Chapter.Value = Mathf.Max(1, chapter);
            Phase.Value = MatchPhase.Story;
            TimeRemaining.Value = 0f;
        }

        [Server]
        public void StartSurvival()
        {
            Night.Value = 1;
            Phase.Value = MatchPhase.Preparation;
            TimeRemaining.Value = preparationTime;
        }

        [Server]
        public void StoryComplete()
        {
            Chapter.Value++;
            Phase.Value = MatchPhase.Story;
            TimeRemaining.Value = 0f;
        }

        [ServerCallback]
        void Update()
        {
            if (!IsServerStarted || TimeRemaining.Value <= 0f) return;
            TimeRemaining.Value = Mathf.Max(0f, TimeRemaining.Value - Time.deltaTime);
            if (TimeRemaining.Value > 0f) return;

            if (Phase.Value == MatchPhase.Preparation)
            {
                Phase.Value = MatchPhase.Night;
                TimeRemaining.Value = nightTime;
            }
            else if (Phase.Value == MatchPhase.Night)
            {
                Night.Value++;
                Phase.Value = MatchPhase.Preparation;
                TimeRemaining.Value = preparationTime;
            }
        }
    }
}
