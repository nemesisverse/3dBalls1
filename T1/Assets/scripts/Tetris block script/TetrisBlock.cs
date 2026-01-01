using UnityEngine;
public class TetrisBlock : MonoBehaviour
{
    private float prevTime;
    public float fallTime = 2f; // Make it public so you can tweak in Inspector
    public float moveDistance = 1f;
    void Update()
    {
        if (Time.time - prevTime > fallTime)
        {
            //transform.position += Vector3.down * moveDistance;
            //if (!CheckValidMove()) {
            //    transform.position += Vector3.up * moveDistance;
            //    enabled = false; // this will stop the Update function from being called further on the obejct which is applied
            //}
            prevTime = Time.time;
        }
    }

    bool CheckValidMove()
    {
        foreach (Transform child in transform)
        {
            //Vector3 pos = PlayField.instance.Round(child.position);
            Vector3 pos = child.position;
            // If outside grid boundaries, invalid move
            if (!PlayField.instance.CheckInsideGrid(pos))
            {
                return false;
            }
        }
        return true;
    }
}