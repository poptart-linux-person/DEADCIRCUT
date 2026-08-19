using UnityEngine;

namespace DeadCircuit.Gameplay
{
    public enum GameMode { Story, Survival }
    public enum MatchPhase { Menu, Lobby, Story, Preparation, Night, Complete, GameOver }

    public class DeadCircuitGameState : MonoBehaviour
    {
        public static DeadCircuitGameState Instance { get; private set; }
        public GameMode Mode { get; private set; }
        public MatchPhase Phase { get; private set; } = MatchPhase.Menu;
        public int Chapter { get; private set; } = 1;
        public int Night { get; private set; } = 0;
        public float PhaseTimeRemaining { get; private set; }

        [SerializeField] float preparationSeconds = 120f;
        [SerializeField] float nightSeconds = 300f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartStory(int chapter = 1)
        {
            Mode = GameMode.Story;
            Chapter = Mathf.Max(1, chapter);
            Night = 0;
            SetPhase(MatchPhase.Story, 0f);
        }

        public void StartSurvival()
        {
            Mode = GameMode.Survival;
            Chapter = 0;
            Night = 1;
            SetPhase(MatchPhase.Preparation, preparationSeconds);
        }

        public void SetPhase(MatchPhase phase, float seconds)
        {
            Phase = phase;
            PhaseTimeRemaining = Mathf.Max(0f, seconds);
        }

        public void AdvanceSurvivalNight()
        {
            Night++;
            SetPhase(MatchPhase.Preparation, preparationSeconds);
        }

        public void Tick(float deltaTime)
        {
            if (PhaseTimeRemaining <= 0f) return;
            PhaseTimeRemaining = Mathf.Max(0f, PhaseTimeRemaining - deltaTime);
            if (PhaseTimeRemaining > 0f) return;
            if (Mode != GameMode.Survival) return;

            if (Phase == MatchPhase.Preparation) SetPhase(MatchPhase.Night, nightSeconds);
            else if (Phase == MatchPhase.Night) AdvanceSurvivalNight();
        }

        public void ReturnToMenu() => SetPhase(MatchPhase.Menu, 0f);
    }
}
