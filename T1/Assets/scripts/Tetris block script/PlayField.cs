using UnityEngine;

public class PlayField : MonoBehaviour
{
    public static PlayField instance;

    public int gridSizeX, gridSizeY, gridSizeZ;

    void Awake()
    {
        instance = this;
    }

    public Vector3 Round(Vector3 vec)
    {
        return new Vector3(
            Mathf.RoundToInt(vec.x),
            Mathf.RoundToInt(vec.y),
            Mathf.RoundToInt(vec.z)
        );
    }

    public bool CheckInsideGrid(Vector3 pos)
    {
        return (
            pos.x >= -13 && pos.x < gridSizeX &&
            pos.z >= -13 && pos.z < gridSizeZ &&
            pos.y >= 2
        );
    }
}
