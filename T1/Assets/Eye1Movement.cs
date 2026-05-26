using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Eye1Movement : MonoBehaviour, IFallingBlock
{
    public IndexManager index;
    int leftDiagonalCount = 0;
    float moveSpeed = 1f;

    List<Vector3> leftDiagonalCoordinates = new List<Vector3>();
    List<GameObject> leftChildObject = new List<GameObject>();

    public GameManager gameManager;
    public SwipeInput swipeInput;
    private SphericalGrid sphericalGrid;

    int stop = -1;
    int stopperID = 0;

    void Awake()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (swipeInput == null) swipeInput = FindFirstObjectByType<SwipeInput>();
        if (sphericalGrid == null) sphericalGrid = FindFirstObjectByType<SphericalGrid>();
        if (index == null) index = FindFirstObjectByType<IndexManager>();

        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f)
            leftDiagonalCoordinates.Add(new Vector3(-v, v, 0f));
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
    //  LEFT DIAGONAL — 3 blocks falling together
    // ================================================================

    IEnumerator moveLeftDiognal(Transform child, int childCount)
{
    if (leftChildObject == null || leftChildObject.Count == 0) yield break;
    if (childCount == 3)
    {
        index.indexCountVertical = index.indexCountVertical  -1 ;
        for (; index.indexCountLeft < leftDiagonalCoordinates.Count; index.indexCountLeft++)
        {
            index.indexCountVertical++;
            if (stop == -1)
            {
                bool blocked = false;
                try { blocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[index.indexCountLeft]); } catch { }
                if (blocked) { stop = index.indexCountLeft - 1; stopperID = 1; }
            }
            yield return null;

            if (stop != -1 && index.indexCountLeft > stop)
            {
                if (stopperID == 1)
                {
                    bool stillBlocked = false;
                    try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[index.indexCountLeft]); } catch { stillBlocked = false; }
                    if (stillBlocked)
                    {
                        // LANDING SPOT 1
                        leftflagRadius(index.indexCountLeft - 1);
                        leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        leftChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                        leftChildObject[2].transform.SetParent(gameManager.motherPlatform.transform, true);
                        gameManager.CheckAndDestroyRings();
                        index.indexCountLeft = 2;
                        index.indexCountVertical  = 2;
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
                            // LANDING SPOT 2
                            leftflagRadius(index.indexCountLeft - 1);
                            leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            leftChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                            leftChildObject[2].transform.SetParent(gameManager.motherPlatform.transform, true);
                            gameManager.CheckAndDestroyRings();
                            index.indexCountLeft = 2;
                            index.indexCountVertical  = 2;
                            TryDestroySelf();
                            yield break;
                        }
                        yield return null;
                    }
                }
            }

            leftChildObject[0].transform.position = leftDiagonalCoordinates[index.indexCountLeft];
            leftChildObject[1].transform.position = leftDiagonalCoordinates[index.indexCountLeft - 1];
            leftChildObject[2].transform.position = leftDiagonalCoordinates[index.indexCountLeft - 2];

            try
            {
                if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[index.indexCountLeft + 1]))
                {
                    if (stop == -1) { stop = index.indexCountLeft; stopperID = 1; }
                }
            }
            catch (System.ArgumentOutOfRangeException)
            {
                if (leftChildObject[0].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 1] &&
                    leftChildObject[1].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 2] &&
                    leftChildObject[2].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 3])
                {
                    // LANDING SPOT 3
                    leftflagRadius(index.indexCountLeft);
                    leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                    leftChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                    leftChildObject[2].transform.SetParent(gameManager.motherPlatform.transform, true);
                    gameManager.CheckAndDestroyRings();
                    index.indexCountLeft = 2;
                    index.indexCountVertical  = 2;
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
    //  HELPERS
    // ================================================================

    void countChildren()
    {
        leftDiagonalCount = 0;
        foreach (Transform child in transform)
            if (child.position.x < 0f) { leftDiagonalCount++; leftChildObject.Add(child.gameObject); }
    }

    void CheckChildrenWorldX()
    {
        bool leftStarted = false;
        foreach (Transform child in transform)
        {
            if (child.position.x < 0f && !leftStarted)
            {
                StartCoroutine(moveLeftDiognal(child, leftDiagonalCount));
                leftStarted = true;
            }
        }
    }

    // ================================================================
    //  FLAG RADIUS — 3 blocks at i, i-1, i-2
    // ================================================================

    void leftflagRadius(int i)
    {
        sphericalGrid.PlaceBlockByWorldPosition(
            leftChildObject[0].transform.position, i,
            leftChildObject[0], gameManager.motherPlatform.transform);
        sphericalGrid.PlaceBlockByWorldPosition(
            leftChildObject[1].transform.position, i - 1,
            leftChildObject[1], gameManager.motherPlatform.transform);
        sphericalGrid.PlaceBlockByWorldPosition(
            leftChildObject[2].transform.position, i - 2,
            leftChildObject[2], gameManager.motherPlatform.transform);
    }
}