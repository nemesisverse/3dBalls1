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
            transform.position += Vector3.down * moveDistance;
            if (!CheckValidMove()) {
                transform.position += Vector3.up * moveDistance;
                enabled = false;

                //create new tetris block
                //***
                //PlayField.instance.SpawnNewBlock();
            }
            else
            {
                //update the grid

                //***
                //PlayField.instance.UpdateGrid(this);
            }
        
           
            prevTime = Time.time;
        }
    }

    bool CheckValidMove()
    {
        foreach (Transform child in transform)
        {
            //Vector3 pos = PlayField.instance.Round(child.position);


            Vector3 pos = child.position;
            // ❌ If outside grid boundaries, invalid move
            if (!PlayField.instance.CheckInsideGrid(pos))
            {
                return false;
            }
        }
        //***
        //foreach(Transform child in transform)
        //{
        //    Vector3 pos = child.position;
        //    Transform t = PlayField.instance.GetTransformOnGridPos(pos);
        //    if(t!=null && t.parent != transform)
        //    {
        //        return false;
        //    }
        //}

        // ✅ If all children are inside grid
        return true;
    }
}