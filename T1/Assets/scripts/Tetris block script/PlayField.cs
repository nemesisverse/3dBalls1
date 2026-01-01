using UnityEngine;

public class PlayField : MonoBehaviour
{
    public static PlayField instance;

    public float gridSizeY;
  
  
    void Awake()
    {
        instance = this;
    }

    // This method checks world position. Make sure this matches your coordinate system.
    public bool CheckInsideGrid(Vector3 pos)
    {
        return (
            pos.y >= gridSizeY
        );
    }
}
