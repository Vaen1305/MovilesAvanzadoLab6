using Unity.Netcode;
using UnityEngine;

public class Projectil : NetworkBehaviour
{
    public int baseDamage = 1;
    // damage that will actually be applied (calculated on server)
    private int damage = 1;
    private ulong shooterClientId; // ID del jugador que disparó

    void Start()
    {
        if (IsServer)
        {
            Invoke("SimpleDespawn", 5f);
        }
    }

    // Método para establecer quién disparó el proyectil
    public void SetShooter(ulong clientId)
    {
        shooterClientId = clientId;
        // If running on the server, initialize directly. If running on client, request server initialization.
        if (IsServer)
        {
            // Default to baseDamage
            damage = baseDamage;
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                var playerObj = client.PlayerObject;
                if (playerObj != null)
                {
                    var controller = playerObj.GetComponent<SimplePlayerController>();
                    if (controller != null)
                    {
                        damage = baseDamage + controller.attack.Value;
                    }
                }
            }
        }
        else
        {
            // Request the server to initialize this projectile (calculate damage using shooter's attack)
            InitializeOnServerServerRpc(clientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void InitializeOnServerServerRpc(ulong clientId)
    {
        if (!IsServer) return;
        shooterClientId = clientId;

        // Default to baseDamage
        damage = baseDamage;

        // Try to read the shooter's attack from their player object
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            var playerObj = client.PlayerObject;
            if (playerObj != null)
            {
                var controller = playerObj.GetComponent<SimplePlayerController>();
                if (controller != null)
                {
                    // Combine baseDamage with player's attack NetworkVariable
                    damage = baseDamage + controller.attack.Value;
                }
            }
        }
    }

    public void SimpleDespawn()
    {
        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            // Obtener el NetworkObject del jugador para verificar si es el mismo que disparó
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
            if (playerNetObj != null && playerNetObj.OwnerClientId != shooterClientId)
            {
                // Es un jugador diferente al que disparó, aplicar daño
                SimplePlayerController player = other.GetComponent<SimplePlayerController>();
                if (player != null)
                {
                    player.TakeDamageServerRpc(damage);
                }
                SimpleDespawn();
            }
            // Si es el mismo jugador que disparó, no hacer nada (no auto-daño)
            return;
        }

        if (other.CompareTag("Enemy"))
        {
            EnemyAI enemy = other.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            SimpleDespawn();
        }

        // Se destruye al chocar con cualquier otra cosa
        SimpleDespawn();
    }
}