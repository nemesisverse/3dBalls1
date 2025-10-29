using UnityEngine;

public class TetrisBlock : MonoBehaviour
{
    private float prevTime;
    public float fallTime = 2f; // Make it public so you can tweak in Inspector
    public float moveDistance = 0.44f;

    void Update()
    {
        if (Time.time - prevTime > fallTime)
        {
            transform.position += Vector3.down * moveDistance; // ✅ 'Vector3' must be capitalized
            prevTime = Time.time;
        }
    }

    bool checkValidMove()
    {

        return true;
    }
}