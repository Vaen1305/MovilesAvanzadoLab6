using UnityEngine;
using Unity.Netcode;

public class CameraFollow : MonoBehaviour
{
    private Transform target;
    public Vector3 offset = new Vector3(0, 5, -10);
    void LateUpdate()
    {
        if (target == null)
        {
            // --- CORRECCIÓN AQUÍ ---
            foreach (var player in FindObjectsOfType<SimplePlayerController>())
            {
                if (player.IsOwner)
                {
                    target = player.transform;
                    break;
                }
            }
        }

        if (target != null)
        {
            transform.position = target.position + offset;
            transform.LookAt(target);
        }
    }
}