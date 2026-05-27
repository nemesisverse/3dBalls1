using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZMovement : MonoBehaviour, IFallingBlock
{
    public IndexManager index;
    int leftDiagonalCount  = 0;
    int rightDiagonalCount = 0;
    int verticalCount      = 0;
    float moveSpeed = 1f;

    List<Vector3> leftDiagonalCoordinates  = new List<Vector3>();
    List<Vector3> rightDiagonalCoordinates = new List<Vector3>();
    List<Vector3> verticalCoordinates      = new List<Vector3>();

    List<GameObject> leftChildObject     = new List<GameObject>();
    List<GameObject> rightChildObject    = new List<GameObject>();
    List<GameObject> verticalChildObject = new List<GameObject>();

    public GameManager gameManager;
    public SwipeInput  swipeInput;

    private SphericalGrid sphericalGrid;

    int stop      = -1;
    int stopperID =  0;

    // ================================================================
    //  AWAKE / START
    // ================================================================

    void Awake()
    {
        if (gameManager  == null) gameManager  = FindFirstObjectByType<GameManager>();
        if (swipeInput   == null) swipeInput   = FindFirstObjectByType<SwipeInput>();
        if (sphericalGrid == null) sphericalGrid = FindFirstObjectByType<SphericalGrid>();
        if (index == null) index = FindFirstObjectByType<IndexManager>();

        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f)
            leftDiagonalCoordinates.Add(new Vector3(-v, v, 0f));
        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f)
            rightDiagonalCoordinates.Add(new Vector3(v, v, 0f));
        for (float v = 18.5f; v >= 2.5f; v -= 1f)
            verticalCoordinates.Add(new Vector3(0f, v, 0f));
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
    //  LEFT DIAGONAL
    //  Z orientation: block lives at index (i-1) — one unit higher than T.
    //  Everything else mirrors TMovement exactly.
    // ================================================================

    IEnumerator moveLeftDiognal(Transform child, int childCount)
    {
        if (leftChildObject == null || leftChildObject.Count == 0) yield break;
        if (childCount == 1)
        {
            for (; index.indexCountLeft < leftDiagonalCoordinates.Count; index.indexCountLeft++)
            {
                // Pre-move look-ahead: Z block will land at [i-1], so check [i-1].
                if (stop == -1)
                {
                    bool blocked = false;
                    try { blocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[index.indexCountLeft - 1]); } catch { }
                    if (blocked) { stop = index.indexCountLeft - 1; stopperID = 1; }
                }
                yield return null;

                if (stop != -1 && index.indexCountLeft > stop)
                {
                    if (stopperID == 1)
                    {
                        bool stillBlocked = false;
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[index.indexCountLeft - 1]); }
                        catch { stillBlocked = false; }

                        if (stillBlocked)
                        {
                            // Block is at [i-2] — land there.
                            leftflagRadius(index.indexCountLeft - 2);
                            leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
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
                                gameManager.CheckAndDestroyRings();
                                index.indexCountLeft = 2;
                                TryDestroySelf();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                // Move to Z position: one step higher than T (i-1 instead of i).
                leftChildObject[0].transform.position = leftDiagonalCoordinates[index.indexCountLeft - 1];

                // Look one step ahead: the next position the Z block would occupy is [i].
                if (index.indexCountLeft + 1 < leftDiagonalCoordinates.Count)
                {
                    if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[index.indexCountLeft]))
                    {
                        if (stop == -1) { stop = index.indexCountLeft; stopperID = 1; }
                    }
                }
                else
                {
                    // Reached the innermost Z position (Count-2).
                    if (leftChildObject[0].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 2])
                    {
                        leftflagRadius(index.indexCountLeft - 1);
                        leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        gameManager.CheckAndDestroyRings();
                        index.indexCountLeft = 2;
                        enabled = false;
                        TryDestroySelf();
                    }
                    yield break;
                }

                while (gameManager.isRotating)
                    yield return null;

                yield return new WaitForSeconds(moveSpeed);
            }
        }
    }

    // ================================================================
    //  RIGHT DIAGONAL — identical to TMovement
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
                    bool blocked = false;
                    try { blocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[index.indexCountRight]); } catch { }
                    if (blocked) { stop = index.indexCountRight - 1; stopperID = 2; }
                }
                yield return null;

                if (stop != -1 && index.indexCountRight > stop)
                {
                    if (stopperID == 2)
                    {
                        bool stillBlocked = false;
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[index.indexCountRight]); }
                        catch { stillBlocked = false; }

                        if (stillBlocked)
                        {
                            rightflagRadius(index.indexCountRight - 1);
                            rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            //gameManager.CheckAndDestroyRings();
                            enabled = false;
                            index.indexCountRight = 2;
                            //TryDestroySelf();
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
                                rightflagRadius(index.indexCountRight - 1);
                                rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                //gameManager.CheckAndDestroyRings();
                                //TryDestroySelf();
                                index.indexCountRight = 2;
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                rightChildObject[0].transform.position = rightDiagonalCoordinates[index.indexCountRight];

                try
                {
                    if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[index.indexCountRight + 1]))
                    {
                        if (stop == -1) { stop = index.indexCountRight; stopperID = 2; }
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    if (rightChildObject[0].transform.position == rightDiagonalCoordinates[rightDiagonalCoordinates.Count - 1])
                    {
                        rightflagRadius(index.indexCountRight);
                        rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        //gameManager.CheckAndDestroyRings();
                        enabled = false;
                        index.indexCountRight = 2;
                        //TryDestroySelf();
                    }
                    yield break;
                }

                while (gameManager.isRotating)
                    yield return null;

                yield return new WaitForSeconds(moveSpeed);
            }
        }
    }

    // ================================================================
    //  VERTICAL — identical to TMovement
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
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, verticalCoordinates[index.indexCountVertical]); }
                        catch { stillBlocked = false; }

                        if (stillBlocked)
                        {
                            verticalflagRadius(index.indexCountVertical - 1);
                            verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                            //gameManager.CheckAndDestroyRings();
                            enabled = false;
                            index.indexCountVertical = 2;
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
                        //gameManager.CheckAndDestroyRings();
                        index.indexCountVertical = 2;
                        enabled = false;
                        //TryDestroySelf();
                    }
                    yield break;
                }

                while (gameManager.isRotating)
                    yield return null;

                yield return new WaitForSeconds(moveSpeed);
            }
        }
    }

    // ================================================================
    //  HELPERS — identical to TMovement
    // ================================================================

    void countChildren()
    {
        leftDiagonalCount = 0; rightDiagonalCount = 0; verticalCount = 0;
        foreach (Transform child in transform)
        {
            if (child.position.x < 0f)  { leftDiagonalCount++;  leftChildObject.Add(child.gameObject); }
        }
        foreach (Transform child in transform)
        {
            if (child.position.x > 0f)  { rightDiagonalCount++; rightChildObject.Add(child.gameObject); }
        }
        foreach (Transform child in transform)
        {
            if (child.position.x == 0f) { verticalCount++;      verticalChildObject.Add(child.gameObject); }
        }
    }

    void CheckChildrenWorldX()
    {
        bool leftStarted = false, rightStarted = false, verticalStarted = false;
        foreach (Transform child in transform)
        {
            float worldX = child.position.x;
            if      (worldX < 0f  && !leftStarted)     { StartCoroutine(moveLeftDiognal(child,  leftDiagonalCount));  leftStarted     = true; }
            else if (worldX == 0f && !verticalStarted) { StartCoroutine(moveVertical(child,     verticalCount));      verticalStarted = true; }
            else if (worldX > 0f  && !rightStarted)    { StartCoroutine(moveRightDiognal(child, rightDiagonalCount)); rightStarted    = true; }
        }
    }

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