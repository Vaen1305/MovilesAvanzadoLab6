using Unity.Netcode;
using UnityEngine;

public class EnemyAI : NetworkBehaviour
{
    public float speed = 3f;
    private Rigidbody rb;

    public int maxHealth = 100;
    private NetworkVariable<int> health = new NetworkVariable<int>(
        readPerm: NetworkVariableReadPermission.Everyone, 
        writePerm: NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            health.Value = maxHealth;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (!IsServer) return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0) return;

        GameObject closest = null;
        float minDist = float.MaxValue;
        foreach (var p in players)
        {
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = p;
            }
        }

        if (closest != null && rb != null)
        {
            Vector3 dir = (closest.transform.position - transform.position).normalized;
            rb.MovePosition(rb.position + dir * speed * Time.fixedDeltaTime);
        }
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;

        health.Value -= damage;
        if (health.Value <= 0)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!IsServer) return;
        if (other.gameObject.CompareTag("Player"))
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}