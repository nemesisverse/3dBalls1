using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class T2Movement : MonoBehaviour, IFallingBlock
{
    public IndexManager index;
    int leftDiagonalCount = 0;
    int rightDiagonalCount = 0;
    int verticalCount = 0;
    float moveSpeed = 1f;

    List<Vector3> leftDiagonalCoordinates = new List<Vector3>();
    List<Vector3> rightDiagonalCoordinates = new List<Vector3>();
    List<Vector3> verticalCoordinates = new List<Vector3>();

    List<GameObject> leftChildObject = new List<GameObject>();
    List<GameObject> rightChildObject = new List<GameObject>();
    List<GameObject> verticalChildObject = new List<GameObject>();

    public GameManager gameManager;
    public SwipeInput swipeInput;

    //I added this code
    private SphericalGrid sphericalGrid;
    private BlockTInstantiator blockTInstantiator;

    int stop = -1;
    int stopperID = 0;

    void Awake()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (swipeInput == null) swipeInput = FindFirstObjectByType<SwipeInput>();
        if (sphericalGrid == null) sphericalGrid = FindFirstObjectByType<SphericalGrid>();
        if (index == null) index = FindFirstObjectByType<IndexManager>(); // ← ADD THIS
        if (blockTInstantiator == null) blockTInstantiator = FindFirstObjectByType<BlockTInstantiator>();

        // Populate Coordinates
        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f) leftDiagonalCoordinates.Add(new Vector3(-v, v, 0f));
        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f) rightDiagonalCoordinates.Add(new Vector3(v, v, 0f));
        for (float v = 18.5f; v >= 2.5f; v -= 1f) verticalCoordinates.Add(new Vector3(0f, v, 0f));
    }

    void Start()
    {
        countChildren();
        CheckChildrenWorldX();
    }

    void TryDestroySelf()
    {
        if (transform.childCount == 0)
            Destroy(gameObject);
    }

    // ================================================================
    //  Destroys the BlockTInstantiator whenever a block lands
    // ================================================================

    void DestroyInstantiator()
    {
        if (blockTInstantiator != null)
            Destroy(blockTInstantiator.gameObject);
    }

    // ================================================================
    //  LEFT DIAGONAL — uses index.indexCountLeft throughout
    // ================================================================

    IEnumerator moveLeftDiognal(Transform child, int childCount)
    {
        if (leftChildObject == null || leftChildObject.Count == 0) yield break;
        if (childCount == 1)
        {
            for (; index.indexCountLeft < rightDiagonalCoordinates.Count; index.indexCountLeft++)
            {
                if (stop == -1)
                {
                    bool blocked = false;                                                                               //not i 
                    try { blocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[index.indexCountLeft - 1]); } catch { }
                    if (blocked) { stop = index.indexCountLeft - 1; stopperID = 1; }
                }
                yield return null;

                if (stop != -1 && index.indexCountLeft > stop)
                {
                    if (stopperID == 1)
                    {
                        bool stillBlocked = false;
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[index.indexCountLeft - 1]); } catch { stillBlocked = false; }
                        if (stillBlocked)
                        {
                            leftflagRadius(index.indexCountLeft - 2);
                            leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            DestroyInstantiator();
                            gameManager.CheckAndDestroyRings();
                            index.indexCountLeft = 2;
                            enabled = false;
                            TryDestroySelf();
                            yield break;
                        }
                        else { stop = -1; stopperID = 0; }
                    }
                    else
                    {
                        while (stop != -1 && stopperID != 1)
                        {
                            if (!enabled)
                            {
                                leftflagRadius(index.indexCountLeft - 2);
                                leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                DestroyInstantiator();
                                gameManager.CheckAndDestroyRings();  // ← ADDED
                                index.indexCountLeft = 2;
                                TryDestroySelf();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                leftChildObject[0].transform.position = leftDiagonalCoordinates[index.indexCountLeft - 1];

                //it doesnt need try and catch as its okay
                if (index.indexCountLeft + 1 < leftDiagonalCoordinates.Count)
                {
                    //not i-1 use
                    if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[index.indexCountLeft]))
                    {
                        if (stop == -1) { stop = index.indexCountLeft; stopperID = 1; }
                    }
                }
                else
                {
                    //not -1
                    if (leftChildObject[0].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 2])
                    {
                        //not i
                        leftflagRadius(index.indexCountLeft - 1);
                        leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        DestroyInstantiator();
                        gameManager.CheckAndDestroyRings();  // ← ADDED
                        index.indexCountLeft = 2;
                        enabled = false;
                        TryDestroySelf();
                    }
                    yield break;
                }

                while (gameManager.isRotating)
                    yield return null;  // Pause here until rotation finishes

                // Pause while swap-check is running
                while (blockTInstantiator != null && blockTInstantiator.isCheckingSwap)
                    yield return null;

                yield return new WaitForSeconds(moveSpeed);
            }
        }
    }

    // ================================================================
    //  RIGHT DIAGONAL — uses index.indexCountRight throughout
    // ================================================================

    IEnumerator moveRightDiognal(Transform child, int childCount)
    {
        if (rightChildObject == null || rightChildObject.Count == 0) yield break;
        if (childCount == 1)
        {
            for (; index.indexCountRight < rightDiagonalCoordinates.Count; index.indexCountRight++)
            {
                if (stop == -1)
                {
                    bool blocked = false;                                                                                       //not i
                    try { blocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[index.indexCountRight - 1]); } catch { }
                    if (blocked) { stop = index.indexCountRight - 1; stopperID = 2; }
                }
                yield return null;

                if (stop != -1 && index.indexCountRight > stop)
                {
                    if (stopperID == 2)
                    {
                        bool stillBlocked = false;
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[index.indexCountRight - 1]); } catch { stillBlocked = false; }
                        if (stillBlocked)
                        {
                            //not i-1
                            rightflagRadius(index.indexCountRight - 2);
                            rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            DestroyInstantiator();
                            gameManager.CheckAndDestroyRings();
                            index.indexCountRight = 2;
                            enabled = false;
                            TryDestroySelf();
                            yield break;
                        }
                        else { stop = -1; stopperID = 0; }
                    }
                    else
                    {
                        while (stop != -1 && stopperID != 2)
                        {
                            if (!enabled)
                            {
                                rightflagRadius(index.indexCountRight - 2);
                                rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                DestroyInstantiator();
                                gameManager.CheckAndDestroyRings();  // ← ADDED
                                index.indexCountRight = 2;
                                TryDestroySelf();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                rightChildObject[0].transform.position = rightDiagonalCoordinates[index.indexCountRight - 1];

                if (index.indexCountRight + 1 < rightDiagonalCoordinates.Count)
                {
                    if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[index.indexCountRight]))
                    {
                        if (stop == -1) { stop = index.indexCountRight; stopperID = 2; }
                    }
                }
                else
                {
                    if (rightChildObject[0].transform.position == rightDiagonalCoordinates[rightDiagonalCoordinates.Count - 2])
                    {
                        rightflagRadius(index.indexCountRight - 1);
                        rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        DestroyInstantiator();
                        gameManager.CheckAndDestroyRings();  // ← ADDED
                        index.indexCountRight = 2;
                        enabled = false;
                        TryDestroySelf();
                    }
                    yield break;
                }

                while (gameManager.isRotating)
                    yield return null;  // Pause here until rotation finishes

                // Pause while swap-check is running
                while (blockTInstantiator != null && blockTInstantiator.isCheckingSwap)
                    yield return null;

                yield return new WaitForSeconds(moveSpeed);
            }
        }
    }

    // ================================================================
    //  VERTICAL — uses index.indexCountVertical throughout
    //  (previously used undefined bare variable 'i' — now fixed)
    // ================================================================

    IEnumerator moveVertical(Transform child, int childCount)
    {
        if (verticalChildObject == null || verticalChildObject.Count == 0) yield break;
        if (childCount == 2)
        {
            for (; index.indexCountVertical < verticalCoordinates.Count; index.indexCountVertical++)
            {
                if (stop == -1)
                {
                    bool blocked = false;
                    try { blocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, verticalCoordinates[index.indexCountVertical]); } catch { }
                    if (blocked) { stop = index.indexCountVertical - 1; stopperID = 3; }
                }
                yield return null;

                if (stop != -1 && index.indexCountVertical > stop)
                {
                    if (stopperID == 3)
                    {
                        bool stillBlocked = false;
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, verticalCoordinates[index.indexCountVertical]); } catch { stillBlocked = false; }
                        if (stillBlocked)
                        {
                            verticalflagRadius(index.indexCountVertical - 1);
                            verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                            DestroyInstantiator();
                            //gameManager.CheckAndDestroyRings();
                            index.indexCountVertical = 2;
                            enabled = false;
                            //TryDestroySelf();
                            yield break;
                        }
                        else { stop = -1; stopperID = 0; }
                    }
                    else
                    {
                        while (stop != -1 && stopperID != 3)
                        {
                            if (!enabled)
                            {
                                verticalflagRadius(index.indexCountVertical - 1);
                                verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                                DestroyInstantiator();
                                //gameManager.CheckAndDestroyRings();
                                index.indexCountVertical = 2;
                                //TryDestroySelf();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                verticalChildObject[0].transform.position = verticalCoordinates[index.indexCountVertical];
                verticalChildObject[1].transform.position = verticalCoordinates[index.indexCountVertical - 1];

                try
                {
                    if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, verticalCoordinates[index.indexCountVertical + 1]))
                    {
                        if (stop == -1) { stop = index.indexCountVertical; stopperID = 3; }
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    if (verticalChildObject[0].transform.position == verticalCoordinates[verticalCoordinates.Count - 1] &&
                        verticalChildObject[1].transform.position == verticalCoordinates[verticalCoordinates.Count - 2])
                    {
                        verticalflagRadius(index.indexCountVertical);
                        verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                        DestroyInstantiator();
                        //gameManager.CheckAndDestroyRings();
                        index.indexCountVertical = 2;
                        enabled = false;
                        //TryDestroySelf();
                    }
                    yield break;
                }

                while (gameManager.isRotating)
                    yield return null;  // Pause here until rotation finishes

                // Pause while swap-check is running
                while (blockTInstantiator != null && blockTInstantiator.isCheckingSwap)
                    yield return null;

                yield return new WaitForSeconds(moveSpeed);
            }
        }
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    void countChildren()
    {
        leftDiagonalCount = 0; rightDiagonalCount = 0; verticalCount = 0;
        foreach (Transform child in transform) { if (child.position.x < 0f) { leftDiagonalCount++; leftChildObject.Add(child.gameObject); } }
        Debug.Log(leftDiagonalCount);
        foreach (Transform child in transform) { if (child.position.x > 0f) { rightDiagonalCount++; rightChildObject.Add(child.gameObject); } }
        Debug.Log(rightDiagonalCount);
        foreach (Transform child in transform) { if (child.position.x == 0f) { verticalCount++; verticalChildObject.Add(child.gameObject); } }
        Debug.Log(verticalCount);
    }

    void CheckChildrenWorldX()
    {
        bool rightStarted = false, verticalStarted = false, leftStarted = false;
        foreach (Transform child in transform)
        {
            float worldX = child.position.x;
            //else if (worldX < 0f && !leftStarted) { StartCoroutine(moveLeftDiognal(child, leftDiagonalCount)); leftStarted = true; }

            if (worldX > 0f && !rightStarted) { StartCoroutine(moveRightDiognal(child, rightDiagonalCount)); rightStarted = true; }
            else if (worldX == 0f && !verticalStarted) { StartCoroutine(moveVertical(child, verticalCount)); verticalStarted = true; }
            else if (worldX < 0f && !leftStarted) { StartCoroutine(moveLeftDiognal(child, leftDiagonalCount)); leftStarted = true; }
        }
    }

    // ================================================================
    //  FLAG RADIUS
    // ================================================================

    void leftflagRadius(int i)
    {
        sphericalGrid.PlaceBlockByWorldPosition(
            leftChildObject[0].transform.position, i,
            leftChildObject[0], gameManager.motherPlatform.transform);
    }

    void rightflagRadius(int i)
    {
        sphericalGrid.PlaceBlockByWorldPosition(
            rightChildObject[0].transform.position, i,
            rightChildObject[0], gameManager.motherPlatform.transform);
    }

    void verticalflagRadius(int i)
    {
        sphericalGrid.PlaceVerticalBlockByPosition(
            verticalChildObject[0].transform.position,
            verticalChildObject[1].transform.position,
            i, verticalChildObject[0], verticalChildObject[1],
            gameManager.motherPlatform.transform);
    }
}