using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class player2 : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> accountID = new();
    public NetworkVariable<int> health = new();
    public NetworkVariable<int> attack = new();


    public void SetData(PlayerData playerData)
    {
        accountID.Value = playerData.accountID;
        health.Value = playerData.health;
        attack.Value = playerData.attack;
        transform.position = playerData.position;
    }
    public override void OnNetworkDespawn()
    {
        gameManager2.Instance.playerStatesByAccountID[accountID.Value.ToString()] = new PlayerData(accountID.Value.ToString(), transform.position, health.Value, attack.Value);


        print("me desconecte " + NetworkManager.Singleton.LocalClientId + " y se guardo la data de " + accountID.Value);
    }
}

public class PlayerData
{
    public string accountID;
    public Vector3 position;
    public int health;
    public int attack;

    public PlayerData(string ID, Vector3 pos, int hp, int atk)
    {
        accountID = ID;
        position = pos;
        health = hp;
        attack = atk;
        
    }
}
