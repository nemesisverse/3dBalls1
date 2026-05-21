using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZMovement : MonoBehaviour, IFallingBlock
{
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
            for (int i = 2; i < leftDiagonalCoordinates.Count; i++)
            {
                // Pre-move look-ahead: Z block will land at [i-1], so check [i-1].
                if (stop == -1)
                {
                    bool blocked = false;
                    try { blocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[i - 1]); } catch { }
                    if (blocked) { stop = i - 1; stopperID = 1; }
                }
                yield return null;

                if (stop != -1 && i > stop)
                {
                    if (stopperID == 1)
                    {
                        bool stillBlocked = false;
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[i - 1]); }
                        catch { stillBlocked = false; }

                        if (stillBlocked)
                        {
                            // Block is at [i-2] — land there.
                            leftflagRadius(i - 2);
                            leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            gameManager.CheckAndDestroyRings();
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
                                leftflagRadius(i - 2);
                                leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                gameManager.CheckAndDestroyRings();
                                TryDestroySelf();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                // Move to Z position: one step higher than T (i-1 instead of i).
                leftChildObject[0].transform.position = leftDiagonalCoordinates[i - 1];

                // Look one step ahead: the next position the Z block would occupy is [i].
                if (i + 1 < leftDiagonalCoordinates.Count)
                {
                    if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[i]))
                    {
                        if (stop == -1) { stop = i; stopperID = 1; }
                    }
                }
                else
                {
                    // Reached the innermost Z position (Count-2).
                    if (leftChildObject[0].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 2])
                    {
                        leftflagRadius(i - 1);
                        leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        gameManager.CheckAndDestroyRings();
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
            for (int i = 2; i < rightDiagonalCoordinates.Count; i++)
            {
                if (stop == -1)
                {
                    bool blocked = false;
                    try { blocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[i]); } catch { }
                    if (blocked) { stop = i - 1; stopperID = 2; }
                }
                yield return null;

                if (stop != -1 && i > stop)
                {
                    if (stopperID == 2)
                    {
                        bool stillBlocked = false;
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[i]); }
                        catch { stillBlocked = false; }

                        if (stillBlocked)
                        {
                            rightflagRadius(i - 1);
                            rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            gameManager.CheckAndDestroyRings();
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
                                rightflagRadius(i - 1);
                                rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                gameManager.CheckAndDestroyRings();
                                TryDestroySelf();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                rightChildObject[0].transform.position = rightDiagonalCoordinates[i];

                try
                {
                    if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[i + 1]))
                    {
                        if (stop == -1) { stop = i; stopperID = 2; }
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    if (rightChildObject[0].transform.position == rightDiagonalCoordinates[rightDiagonalCoordinates.Count - 1])
                    {
                        rightflagRadius(i);
                        rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        //gameManager.CheckAndDestroyRings();
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
    //  VERTICAL — identical to TMovement
    // ================================================================

    IEnumerator moveVertical(Transform child, int childCount)
    {
        if (verticalChildObject == null || verticalChildObject.Count == 0) yield break;
        if (childCount == 2)
        {
            for (int i = 2; i < verticalCoordinates.Count; i++)
            {
                if (stop == -1)
                {
                    bool blocked = false;
                    try { blocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, verticalCoordinates[i]); } catch { }
                    if (blocked) { stop = i - 1; stopperID = 3; }
                }
                yield return null;

                if (stop != -1 && i > stop)
                {
                    if (stopperID == 3)
                    {
                        bool stillBlocked = false;
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, verticalCoordinates[i]); }
                        catch { stillBlocked = false; }

                        if (stillBlocked)
                        {
                            verticalflagRadius(i - 1);
                            verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                            gameManager.CheckAndDestroyRings();
                            enabled = false;
                            TryDestroySelf();
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
                                verticalflagRadius(i - 1);
                                verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                                gameManager.CheckAndDestroyRings();
                                TryDestroySelf();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                verticalChildObject[0].transform.position = verticalCoordinates[i];
                verticalChildObject[1].transform.position = verticalCoordinates[i - 1];

                try
                {
                    if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, verticalCoordinates[i + 1]))
                    {
                        if (stop == -1) { stop = i; stopperID = 3; }
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    if (verticalChildObject[0].transform.position == verticalCoordinates[verticalCoordinates.Count - 1] &&
                        verticalChildObject[1].transform.position == verticalCoordinates[verticalCoordinates.Count - 2])
                    {
                        verticalflagRadius(i);
                        verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                        //gameManager.CheckAndDestroyRings();
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