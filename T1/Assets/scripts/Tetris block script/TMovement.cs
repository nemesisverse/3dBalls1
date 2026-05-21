using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TMovement : MonoBehaviour, IFallingBlock
{
    // ================================================================
    //  IFallingBlock implementation  ← NEW
    // ================================================================

    // BlockCycler sets this BEFORE Start() so coroutines resume at the
    // correct radius index instead of always starting from index 2.
    public int StartIndex { get; set; } = 2;

    // Highest loop-index reached across all three tracks.
    // BlockCycler reads this on tap to hand off to the next block.
    public int CurrentIndex { get; private set; } = 2;

    // Per-track index snapshots used by StopMovement() to know which
    // radius level to register each frozen child in the grid.
    int _leftI = 2, _rightI = 2, _vertI = 2;

    // ================================================================

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

    private SphericalGrid sphericalGrid;

    int stop = -1;
    int stopperID = 0;

    void Awake()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (swipeInput == null) swipeInput = FindFirstObjectByType<SwipeInput>();
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
        SetInitialPositions();   // ← NEW: snap children to StartIndex before coroutines run
        CheckChildrenWorldX();
    }

    // ================================================================
    //  NEW — snap children to StartIndex on spawn so there is no
    //  one-frame jump when the block is cycled in mid-fall.
    // ================================================================
    void SetInitialPositions()
    {
        int si = Mathf.Clamp(StartIndex, 2, Mathf.Min(
            leftDiagonalCoordinates.Count  - 1,
            Mathf.Min(rightDiagonalCoordinates.Count - 1,
                      verticalCoordinates.Count      - 1)));

        if (leftChildObject.Count > 0)
            leftChildObject[0].transform.position = leftDiagonalCoordinates[si];

        if (rightChildObject.Count > 0)
            rightChildObject[0].transform.position = rightDiagonalCoordinates[si];

        if (verticalChildObject.Count > 0)
            verticalChildObject[0].transform.position = verticalCoordinates[si];

        if (verticalChildObject.Count > 1 && si - 1 >= 0)
            verticalChildObject[1].transform.position = verticalCoordinates[si - 1];
    }

    // ================================================================
    //  NEW — IFallingBlock.StopMovement
    //  Called by BlockCycler on tap.
    //  Stops all coroutines and freezes every still-falling child
    //  onto motherPlatform at its current world position.
    // ================================================================
    public void StopMovement()
    {
        StopAllCoroutines();
        enabled = false;

        // --- left children ---
        foreach (var go in leftChildObject)
        {
            if (go != null && go.transform.parent == transform)
            {
                sphericalGrid.PlaceBlockByWorldPosition(
                    go.transform.position, _leftI,
                    go, gameManager.motherPlatform.transform);
                go.transform.SetParent(gameManager.motherPlatform.transform, true);
            }
        }

        // --- right children ---
        foreach (var go in rightChildObject)
        {
            if (go != null && go.transform.parent == transform)
            {
                sphericalGrid.PlaceBlockByWorldPosition(
                    go.transform.position, _rightI,
                    go, gameManager.motherPlatform.transform);
                go.transform.SetParent(gameManager.motherPlatform.transform, true);
            }
        }

        // --- vertical children (child[0] is at _vertI, child[1] at _vertI-1) ---
        for (int j = 0; j < verticalChildObject.Count; j++)
        {
            var go = verticalChildObject[j];
            if (go != null && go.transform.parent == transform)
            {
                int idx = Mathf.Max(2, _vertI - j);
                sphericalGrid.PlaceBlockByWorldPosition(
                    go.transform.position, idx,
                    go, gameManager.motherPlatform.transform);
                go.transform.SetParent(gameManager.motherPlatform.transform, true);
            }
        }

        gameManager.CheckAndDestroyRings();
        TryDestroySelf();
    }

    void TryDestroySelf()
    {
        if (transform.childCount == 0)
            Destroy(gameObject);
    }

    // ================================================================
    //  LEFT DIAGONAL — 3 landing spots, all now have ring check
    //  CHANGED: loop starts at StartIndex; per-iteration index tracking.
    // ================================================================

    IEnumerator moveLeftDiognal(Transform child, int childCount)
    {
        if (leftChildObject == null || leftChildObject.Count == 0) yield break;
        if (childCount == 1)
        {
            int loopStart = Mathf.Clamp(StartIndex, 2, leftDiagonalCoordinates.Count - 1); // ← NEW
            for (int i = loopStart; i < leftDiagonalCoordinates.Count; i++)
            {
                _leftI = i; if (i > CurrentIndex) CurrentIndex = i;  // ← NEW

                if (stop == -1)
                {
                    bool blocked = false;
                    try { blocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[i]); } catch { }
                    if (blocked) { stop = i - 1; stopperID = 1; }
                }
                yield return null;

                if (stop != -1 && i > stop)
                {
                    if (stopperID == 1)
                    {
                        bool stillBlocked = false;
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[i]); } catch { stillBlocked = false; }
                        if (stillBlocked)
                        {
                            // LANDING SPOT 1
                            leftflagRadius(i - 1);
                            leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            gameManager.CheckAndDestroyRings();  // ← ADDED
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
                                leftflagRadius(i - 1);
                                leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                gameManager.CheckAndDestroyRings();  // ← ADDED
                                TryDestroySelf();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                leftChildObject[0].transform.position = leftDiagonalCoordinates[i];

                try
                {
                    if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[i + 1]))
                    {
                        if (stop == -1) { stop = i; stopperID = 1; }
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    if (leftChildObject[0].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 1])
                    {
                        // LANDING SPOT 3
                        leftflagRadius(i);
                        leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        gameManager.CheckAndDestroyRings();  // ← ADDED
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
    //  RIGHT DIAGONAL — 3 landing spots, all now have ring check
    //  CHANGED: loop starts at StartIndex; per-iteration index tracking.
    // ================================================================

    IEnumerator moveRightDiognal(Transform child, int childCount)
    {
        if (rightChildObject == null || rightChildObject.Count == 0) yield break;
        if (childCount == 1)
        {
            int loopStart = Mathf.Clamp(StartIndex, 2, rightDiagonalCoordinates.Count - 1); // ← NEW
            for (int i = loopStart; i < rightDiagonalCoordinates.Count; i++)
            {
                _rightI = i; if (i > CurrentIndex) CurrentIndex = i;  // ← NEW

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
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[i]); } catch { stillBlocked = false; }
                        if (stillBlocked)
                        {
                            // LANDING SPOT 4
                            rightflagRadius(i - 1);
                            rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            gameManager.CheckAndDestroyRings();  // ← ADDED
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
                                // LANDING SPOT 5
                                rightflagRadius(i - 1);
                                rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                gameManager.CheckAndDestroyRings();  // ← ADDED
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
                        // LANDING SPOT 6
                        rightflagRadius(i);
                        rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        gameManager.CheckAndDestroyRings();  // ← ADDED
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
    //  VERTICAL — already had ring checks (3 landing spots)
    //  CHANGED: loop starts at StartIndex; per-iteration index tracking.
    // ================================================================

    IEnumerator moveVertical(Transform child, int childCount)
    {
        if (verticalChildObject == null || verticalChildObject.Count == 0) yield break;
        if (childCount == 2)
        {
            int loopStart = Mathf.Clamp(StartIndex, 2, verticalCoordinates.Count - 1); // ← NEW
            for (int i = loopStart; i < verticalCoordinates.Count; i++)
            {
                _vertI = i; if (i > CurrentIndex) CurrentIndex = i;  // ← NEW

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
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, verticalCoordinates[i]); } catch { stillBlocked = false; }
                        if (stillBlocked)
                        {
                            // LANDING SPOT 7
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
                                // LANDING SPOT 8
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
                        // LANDING SPOT 9
                        verticalflagRadius(i);
                        verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
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
    //  HELPERS
    // ================================================================

    void countChildren()
    {
        leftDiagonalCount = 0; rightDiagonalCount = 0; verticalCount = 0;
        foreach (Transform child in transform)
        {
            if (child.position.x < 0f) { leftDiagonalCount++; leftChildObject.Add(child.gameObject); }
        }
        foreach (Transform child in transform)
        {
            if (child.position.x > 0f) { rightDiagonalCount++; rightChildObject.Add(child.gameObject); }
        }
        foreach (Transform child in transform)
        {
            if (child.position.x == 0f) { verticalCount++; verticalChildObject.Add(child.gameObject); }
        }
    }

    void CheckChildrenWorldX()
    {
        bool leftStarted = false, rightStarted = false, verticalStarted = false;
        foreach (Transform child in transform)
        {
            float worldX = child.position.x;
            if (worldX < 0f && !leftStarted)
            {
                StartCoroutine(moveLeftDiognal(child, leftDiagonalCount));
                leftStarted = true;
            }
            else if (worldX == 0f && !verticalStarted)
            {
                StartCoroutine(moveVertical(child, verticalCount));
                verticalStarted = true;
            }
            else if (worldX > 0f && !rightStarted)
            {
                StartCoroutine(moveRightDiognal(child, rightDiagonalCount));
                rightStarted = true;
            }
        }
    }

    // ================================================================
    //  FLAG RADIUS — passes block's ACTUAL world position to grid
    //  so it finds the correct plane + slot, not just first empty
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