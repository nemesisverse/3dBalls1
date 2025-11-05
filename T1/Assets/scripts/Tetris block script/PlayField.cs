using UnityEngine;

public class PlayField : MonoBehaviour
{
    public static PlayField instance;

    public float gridSizeX, gridSizeY, gridSizeZ;

    [Header("Blocks List")]
    public GameObject[] blocks;


    //***
    //public Transform[,,] theGrid;

    //for moving block to check which point is valid to fit
    public Transform[,,] leftGrid;
    public Transform[,,] middleGrid;
    public Transform[,,] rightGrid;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        //***
        //theGrid = new Transform[gridSizeX, gridSizeY, gridSizeZ];
    }


    // This method checks world position. Make sure this matches your coordinate system.
    public bool CheckInsideGrid(Vector3 pos)
    {
        return (
            pos.x >= -13f && pos.x < gridSizeX &&
            pos.z >= -13f && pos.z < gridSizeZ &&
            pos.y >= gridSizeY
        );
    }


    //***
    // Update grid cells that belong to 'block'
    //public void UpdateGrid(TetrisBlock block)
    //{
    //    // 1) Remove any children of this block from the grid
    //    for (int x = 0; x < gridSizeX; x++)
    //    {
    //        for (int y = 0; y < gridSizeY; y++)
    //        {
    //            for (int z = 0; z < gridSizeZ; z++)
    //            {
    //                  if(theGrid[x,y,z] != null){
    //                     if (theGrid[x, y, z] != null && theGrid[x, y, z].parent == block.transform)
    //                     {
    //                         theGrid[x, y, z] = null; // assignment, not comparison
    //                     }
    //                  }
    //                // null-check before accessing .parent

    //            }
    //        }
    //    }
    //
    //    // 2) Fill grid with current children of the block
    //    foreach (Transform child in block.transform)
    //    {
    //        Vector3Int pos = Round(child.position);
    //
    //        // Bounds-check before indexing (prevents OutOfRange exceptions)
    //        if (pos.x >= 0 && pos.x < gridSizeX &&
    //            pos.y >= 0 && pos.y < gridSizeY &&
    //            pos.z >= 0 && pos.z < gridSizeZ)
    //        {
    //            theGrid[pos.x, pos.y, pos.z] = child;
    //        }
    //        // else: child is outside array bounds — handle if needed
    //    }
    //}
    //
         // to check if that position is taken or not
    //public Transform GetTransformOnGridPos(Vector3 pos)
    //{
    //    if(pos.y > gridSizeY - 1)
    //    {
              //outside the grid
    //        return null;
    //    }
    //    else
    //    {
                //inside grid
    //        return theGrid[(int)pos.x, (int)pos.y, (int)pos.z];
    //    }
    //}

    //public void SpawnNewBlock()
    //{
    //    Vector3 spawnPoint = new Vector3(transform.position.x + gridSizeX/2 , 
    //                                     transform.position.y + gridSizeY,
    //                                     transform.position.z + gridSizeZ/2);
    //
    //    int randomIndex = randomIndex.range(0,blocks.length);
    //    //spawn the block
    //    GameObject newBlock = Instantiate(blocks[randomIndex], spawnPoint, Quaternion.identity) as GameObject;
    //    //ghost
    //
    //    //inputs
    //}
}
