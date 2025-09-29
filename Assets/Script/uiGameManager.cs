using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class uiGameManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private GameObject connectionPanel;
    [SerializeField] private string lobbySceneName = "Lobby";

    void Start()
    {
        hostButton.onClick.AddListener(() => {
            StartConnection(true);
        });

        clientButton.onClick.AddListener(() => {
            StartConnection(false);
        });
    }

    private void StartConnection(bool isHost)
    {
        string playerName = nameInputField.text;
        if (string.IsNullOrEmpty(playerName))
        {
            return;
        }

        gameManager2.Instance.SetLocalPlayerName(playerName);
        connectionPanel.SetActive(false);

        if (isHost)
        {
            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
            NetworkManager.Singleton.StartHost();
        }
        else
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    private void HandleServerStarted()
    {        
        NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;

        NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}