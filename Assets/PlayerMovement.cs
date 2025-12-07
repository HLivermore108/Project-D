using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float laneWidth = 3f;
    private int lane = 0; // -1 = left, 0 = middle, 1 = right

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A) && lane > -1)
            lane--;
        if (Input.GetKeyDown(KeyCode.D) && lane < 1)
            lane++;

        Vector3 target = new Vector3(lane * laneWidth, transform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
    }
}
