using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [Header("Lobby UI")]
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerCardPrefab;

    [Header("Customization UI")]
    [SerializeField] private CanvasGroup customizationPanel; 
    [SerializeField] private Button nextMainBodyButton, prevMainBodyButton;
    [SerializeField] private Button nextBodyPartButton, prevBodyPartButton;
    [SerializeField] private Button nextEyeButton, prevEyeButton;
    [SerializeField] private Button nextGloveButton, prevGloveButton;
    [SerializeField] private Button nextHeadPartButton, prevHeadPartButton;
    [SerializeField] private Button nextMouthAndNoseButton, prevMouthAndNoseButton;
    [SerializeField] private Button nextTailButton, prevTailButton;

    private int currentMainBodyIndex = 0, currentBodyPartIndex = -1, currentEyeIndex = 0, currentGloveIndex = -1, currentHeadPartIndex = -1, currentMouthAndNoseIndex = 0, currentTailIndex = -1;

    private SimplePlayerController localPlayerController;

    void Start()
    {
        if (gameManager2.Instance != null)
        {
            gameManager2.Instance.lobbyPlayers.OnListChanged += UpdatePlayerList;
        }

        if (customizationPanel != null)
        {
            customizationPanel.interactable = false;
            customizationPanel.alpha = 0.5f;
        }

        readyButton.onClick.AddListener(() => gameManager2.Instance.SetPlayerReadyServerRpc());
        startButton.onClick.AddListener(() => gameManager2.Instance.StartGameServerRpc());

        nextMainBodyButton.onClick.AddListener(OnNextMainBody);
        prevMainBodyButton.onClick.AddListener(OnPrevMainBody);
        nextBodyPartButton.onClick.AddListener(OnNextBodyPart);
        prevBodyPartButton.onClick.AddListener(OnPrevBodyPart);
        nextEyeButton.onClick.AddListener(OnNextEye);
        prevEyeButton.onClick.AddListener(OnPrevEye);
        nextGloveButton.onClick.AddListener(OnNextGlove);
        prevGloveButton.onClick.AddListener(OnPrevGlove);
        nextHeadPartButton.onClick.AddListener(OnNextHeadPart);
        prevHeadPartButton.onClick.AddListener(OnPrevHeadPart);
        nextMouthAndNoseButton.onClick.AddListener(OnNextMouthAndNose);
        prevMouthAndNoseButton.onClick.AddListener(OnPrevMouthAndNose);
        nextTailButton.onClick.AddListener(OnNextTail);
        prevTailButton.onClick.AddListener(OnPrevTail);

        UpdatePlayerList();
    }

    void Update()
    {
        if (localPlayerController == null)
        {
            if (NetworkManager.Singleton?.LocalClient?.PlayerObject != null)
            {
                localPlayerController = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<SimplePlayerController>();
                
                if (localPlayerController != null && customizationPanel != null)
                {
                    customizationPanel.interactable = true;
                    customizationPanel.alpha = 1f;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (gameManager2.Instance != null)
        {
            gameManager2.Instance.lobbyPlayers.OnListChanged -= UpdatePlayerList;
        }
    }

    private int GetNextIndex(int currentIndex, int totalItems, bool allowNone)
    {
        currentIndex++;
        int limit = totalItems;
        int resetValue = allowNone ? -1 : 0;
        if (currentIndex >= limit) currentIndex = resetValue;
        return currentIndex;
    }

    private int GetPrevIndex(int currentIndex, int totalItems, bool allowNone)
    {
        currentIndex--;
        int lowerBound = allowNone ? -1 : 0;
        if (currentIndex < lowerBound) currentIndex = totalItems - 1;
        return currentIndex;
    }

    private void HandleChange(ref int currentIndex, int totalItems, bool allowNone, System.Action<SimplePlayerController, int> rpcCall, bool isNext = true)
    {
        if (isNext) currentIndex = GetNextIndex(currentIndex, totalItems, allowNone);
        else currentIndex = GetPrevIndex(currentIndex, totalItems, allowNone);
        
        rpcCall(localPlayerController, currentIndex);
    }

    private void OnNextMainBody() => HandleChange(ref currentMainBodyIndex, 6, false, (p, i) => p.ChangeMainBodyServerRpc(i));
    private void OnPrevMainBody() => HandleChange(ref currentMainBodyIndex, 6, false, (p, i) => p.ChangeMainBodyServerRpc(i), false);
    private void OnNextBodyPart() => HandleChange(ref currentBodyPartIndex, 10, true, (p, i) => p.ChangeBodyPartServerRpc(i));
    private void OnPrevBodyPart() => HandleChange(ref currentBodyPartIndex, 10, true, (p, i) => p.ChangeBodyPartServerRpc(i), false);
    private void OnNextEye() => HandleChange(ref currentEyeIndex, 11, false, (p, i) => p.ChangeEyeServerRpc(i));
    private void OnPrevEye() => HandleChange(ref currentEyeIndex, 11, false, (p, i) => p.ChangeEyeServerRpc(i), false);
    private void OnNextGlove() => HandleChange(ref currentGloveIndex, 10, true, (p, i) => p.ChangeGloveServerRpc(i));
    private void OnPrevGlove() => HandleChange(ref currentGloveIndex, 10, true, (p, i) => p.ChangeGloveServerRpc(i), false);
    private void OnNextHeadPart() => HandleChange(ref currentHeadPartIndex, 4, true, (p, i) => p.ChangeHeadPartServerRpc(i));
    private void OnPrevHeadPart() => HandleChange(ref currentHeadPartIndex, 4, true, (p, i) => p.ChangeHeadPartServerRpc(i), false);
    private void OnNextMouthAndNose() => HandleChange(ref currentMouthAndNoseIndex, 14, false, (p, i) => p.ChangeMouthAndNoseServerRpc(i));
    private void OnPrevMouthAndNose() => HandleChange(ref currentMouthAndNoseIndex, 14, false, (p, i) => p.ChangeMouthAndNoseServerRpc(i), false);
    private void OnNextTail() => HandleChange(ref currentTailIndex, 8, true, (p, i) => p.ChangeTailServerRpc(i));
    private void OnPrevTail() => HandleChange(ref currentTailIndex, 8, true, (p, i) => p.ChangeTailServerRpc(i), false);

    private void UpdatePlayerList(NetworkListEvent<LobbyPlayerData> changeEvent = default)
    {
        foreach (Transform child in playerListContent) Destroy(child.gameObject);
        if (gameManager2.Instance == null) return;
        foreach (var playerData in gameManager2.Instance.lobbyPlayers)
        {
            GameObject card = Instantiate(playerCardPrefab, playerListContent);
            card.GetComponentInChildren<TMP_Text>().text = $"{playerData.PlayerName} - {(playerData.IsReady ? "<color=green>Listo</color>" : "<color=red>No Listo</color>")}";
        }
        if (NetworkManager.Singleton.IsHost)
        {
            startButton.gameObject.SetActive(true);
            bool allReady = true;         
            startButton.interactable = allReady;
        }
        else
        {
            startButton.gameObject.SetActive(false);
        }
    }
}