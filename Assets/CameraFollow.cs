using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 5, -10);

    void LateUpdate()
    {
        if (player != null)
            transform.position = player.position + offset;
    }
}
