using UnityEngine;
using UnityEngine.SceneManagement;
using DeadCircuit.Gameplay;
using DeadCircuit.Networking;

namespace DeadCircuit.UI
{
    public class DeadCircuitMenu : MonoBehaviour
    {
        [SerializeField] string storyScene = "Story_01";
        [SerializeField] string survivalScene = "Survival_01";
        [SerializeField] MatchDirector matchDirector;

        public void PlayStory()
        {
            if (DeadCircuitGameState.Instance != null) DeadCircuitGameState.Instance.StartStory(1);
            SceneManager.LoadScene(storyScene);
        }

        public void PlaySurvival()
        {
            if (DeadCircuitGameState.Instance != null) DeadCircuitGameState.Instance.StartSurvival();
            SceneManager.LoadScene(survivalScene);
        }

        public void Quit() => Application.Quit();

        public void HostAndPlayStory()
        {
            if (matchDirector != null) matchDirector.StartStory(1);
            PlayStory();
        }

        public void HostAndPlaySurvival()
        {
            if (matchDirector != null) matchDirector.StartSurvival();
            PlaySurvival();
        }
    }
}
