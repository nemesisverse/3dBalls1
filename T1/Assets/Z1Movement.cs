using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Z1Movement : MonoBehaviour, IFallingBlock
{
    int leftDiagonalCount = 0;
    int verticalCount     = 0;
    float moveSpeed = 1f;

    List<Vector3> leftDiagonalCoordinates = new List<Vector3>();
    List<Vector3> verticalCoordinates     = new List<Vector3>();

    List<GameObject> leftChildObject     = new List<GameObject>();
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
        if (gameManager   == null) gameManager   = FindFirstObjectByType<GameManager>();
        if (swipeInput    == null) swipeInput    = FindFirstObjectByType<SwipeInput>();
        if (sphericalGrid == null) sphericalGrid = FindFirstObjectByType<SphericalGrid>();

        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f)
            leftDiagonalCoordinates.Add(new Vector3(-v, v, 0f));
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
    //  2 blocks: block[0] leads at [i-1], block[1] trails at [i-2].
    //  Mirrors ZMovement's left diagonal exactly — only difference is
    //  moving/placing the second block alongside the first.
    // ================================================================

    IEnumerator moveLeftDiagonal(Transform child, int childCount)
    {
        if (leftChildObject == null || leftChildObject.Count == 0) yield break;
        if (childCount == 2)
        {
            for (int i = 2; i < leftDiagonalCoordinates.Count; i++)
            {
                // Pre-move look-ahead: lead block's upcoming position is [i-1].
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
                        // FIX: check [i-1] (same position that triggered the stop), not [i]
                        bool stillBlocked = false;
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[i - 1]); }
                        catch { stillBlocked = false; }

                        if (stillBlocked)
                        {
                            // LANDING SPOT 1 — block[0] rests at [i-2], block[1] at [i-3]
                            leftflagRadius(i - 2);
                            leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            leftChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
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
                                // LANDING SPOT 2
                                leftflagRadius(i - 2);
                                leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                leftChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                                gameManager.CheckAndDestroyRings();
                                TryDestroySelf();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                // Move both blocks: lead to [i-1], trail to [i-2]
                leftChildObject[0].transform.position = leftDiagonalCoordinates[i - 1];
                leftChildObject[1].transform.position = leftDiagonalCoordinates[i - 2];

                // FIX: post-move look-ahead checks [i] — the lead block's NEXT position.
                // Guard with i+1 < Count so the else branch handles the end-of-track case.
                if (i + 1 < leftDiagonalCoordinates.Count)
                {
                    if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[i]))
                    {
                        if (stop == -1) { stop = i; stopperID = 1; }
                    }
                }
                else
                {
                    // End of track: block[0] at [Count-2], block[1] at [Count-3]
                    if (leftChildObject[0].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 2] &&
                        leftChildObject[1].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 3])
                    {
                        // LANDING SPOT 3
                        leftflagRadius(i - 1);
                        leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        leftChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
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
    //  VERTICAL — identical to ZMovement
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
                            // LANDING SPOT 4
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
                                // LANDING SPOT 5
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
                        // LANDING SPOT 6
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
    //  HELPERS
    // ================================================================

    void countChildren()
    {
        leftDiagonalCount = 0; verticalCount = 0;
        foreach (Transform child in transform)
            if (child.position.x < 0f)  { leftDiagonalCount++;  leftChildObject.Add(child.gameObject); }
        foreach (Transform child in transform)
            if (child.position.x == 0f) { verticalCount++;      verticalChildObject.Add(child.gameObject); }
    }

    void CheckChildrenWorldX()
    {
        bool leftStarted = false, verticalStarted = false;
        foreach (Transform child in transform)
        {
            float worldX = child.position.x;
            if      (worldX < 0f  && !leftStarted)     { StartCoroutine(moveLeftDiagonal(child, leftDiagonalCount)); leftStarted     = true; }
            else if (worldX == 0f && !verticalStarted) { StartCoroutine(moveVertical(child,    verticalCount));      verticalStarted = true; }
        }
    }

    // Places block[0] at index i, block[1] at index i-1
    void leftflagRadius(int i)
    {
        sphericalGrid.PlaceBlockByWorldPosition(
            leftChildObject[0].transform.position, i,
            leftChildObject[0], gameManager.motherPlatform.transform);
        sphericalGrid.PlaceBlockByWorldPosition(
            leftChildObject[1].transform.position, i - 1,
            leftChildObject[1], gameManager.motherPlatform.transform);
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














