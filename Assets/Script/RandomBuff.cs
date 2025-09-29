using Unity.Netcode;
using UnityEngine;

public class RandomBuff : NetworkBehaviour
{
    [SerializeField] private int buffAmount = 1;
    private bool applied = false;
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && !applied)
        {
            var no = other.GetComponent<NetworkObject>();
            if (no != null)
            {
                int chosen = Random.Range(1, 4);
                AddBuffPlayerServerRpc(no.OwnerClientId, chosen);
                applied = true;
                print("Hemos chocao con owner " + no.OwnerClientId + " amount " + chosen);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddBuffPlayerServerRpc(ulong playerID, int amount)
    {
        if (!IsServer) return;
        print("aplicar buf en servidor a " + playerID + " amount " + amount);

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerID, out var client))
        {
            var playerObject = client.PlayerObject;
            if (playerObject != null)
            {
                var controller = playerObject.GetComponent<SimplePlayerController>();
                if (controller != null)
                {
                    controller.attack.Value += amount;
                    print($"Nuevo attack de {playerID} = {controller.attack.Value}");
                }
            }
        }

        var netObj = GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Despawn(true);
        }
    }
}