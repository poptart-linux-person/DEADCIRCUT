using FishNet;
using UnityEngine;
using UnityEngine.UI;

namespace DeadCircuit.Networking
{
    public class LobbyController : MonoBehaviour
    {
        [SerializeField] InputField addressInput;
        [SerializeField] Button hostButton;
        [SerializeField] Button joinButton;
        [SerializeField] string defaultAddress = "127.0.0.1";

        void Awake()
        {
            if (hostButton != null) hostButton.onClick.AddListener(Host);
            if (joinButton != null) joinButton.onClick.AddListener(Join);
        }

        public void Host()
        {
            if (!InstanceFinder.ServerManager.Started)
                InstanceFinder.ServerManager.StartConnection();
            if (!InstanceFinder.ClientManager.Started)
                InstanceFinder.ClientManager.StartConnection();
        }

        public void Join()
        {
            string address = addressInput != null && !string.IsNullOrWhiteSpace(addressInput.text)
                ? addressInput.text.Trim()
                : defaultAddress;
            InstanceFinder.ClientManager.StartConnection(address);
        }
    }
}
