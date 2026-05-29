using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TMovement : MonoBehaviour, IFallingBlock
{
    public IndexManager index;
    int leftDiagonalCount = 0;
    int rightDiagonalCount = 0;
    int verticalCount = 0;
    float moveSpeed     = 1.5f;   // ← was 1f
    float fastMoveSpeed = 0.25f;   // ← NEW: hold-to-fast-fall speed

    List<Vector3> leftDiagonalCoordinates = new List<Vector3>();
    List<Vector3> rightDiagonalCoordinates = new List<Vector3>();
    List<Vector3> verticalCoordinates = new List<Vector3>();

    List<GameObject> leftChildObject = new List<GameObject>();
    List<GameObject> rightChildObject = new List<GameObject>();
    List<GameObject> verticalChildObject = new List<GameObject>();

    public GameManager gameManager;
    public SwipeInput swipeInput;

    private SphericalGrid sphericalGrid;
    private BlockTInstantiator blockTInstantiator;

    // ================================================================
    //  Block Guide / Indicator system
    //  blockGuide is resolved at runtime via Find because TMovement is a
    //  prefab instantiated during gameplay — a scene object cannot be
    //  pre-assigned in the prefab's Inspector field.
    //  tMovementIndicators — the child names that belong to this block shape.
    // ================================================================
    private GameObject blockGuide;
    private static readonly string[] tMovementIndicators = { "(3,1)", "(3,2)", "(3,3)", "(2,2)" };

    int stop = -1;
    int stopperID = 0;

    void Awake()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (swipeInput == null) swipeInput = FindFirstObjectByType<SwipeInput>();
        if (sphericalGrid == null) sphericalGrid = FindFirstObjectByType<SphericalGrid>();
        if (index == null) index = FindFirstObjectByType<IndexManager>();
        if (blockTInstantiator == null) blockTInstantiator = FindFirstObjectByType<BlockTInstantiator>();
        blockGuide = GameObject.Find("Block Guide");

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
        SetIndicators();
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
    //  INDICATOR HELPERS
    //  SetIndicators        — called once on Start; activates only the
    //                         cells that match this block's shape and
    //                         deactivates every other cell.
    //  DeactivateAllIndicators — called at every landing spot so the
    //                         guide goes dark the moment a block lands.
    // ================================================================

    void SetIndicators()
    {
        if (blockGuide == null) return;

        // Deactivate every child first
        foreach (Transform child in blockGuide.transform)
            child.gameObject.SetActive(false);

        // Activate only the cells that belong to TMovement
        foreach (string indicatorName in tMovementIndicators)
        {
            Transform indicator = blockGuide.transform.Find(indicatorName);
            if (indicator != null)
                indicator.gameObject.SetActive(true);
        }
    }

    void DeactivateAllIndicators()
    {
        if (blockGuide == null) return;
        foreach (Transform child in blockGuide.transform)
            child.gameObject.SetActive(false);
    }

    // ================================================================
    //  GAME OVER CHECK
    //  ---------------------------------------------------------------
    //  If the block lands at coordinate list index ≤ 3, the outermost
    //  zone has been reached — the stack has overflowed and the game
    //  is over.
    //
    //  Returns true when game over is triggered so every call site can
    //  skip ring-detection entirely (no point scoring after game over).
    //
    //  The same pattern must be applied to T1Movement and T2Movement
    //  at their equivalent landing spots.
    // ================================================================

    bool CheckGameOverOnLanding(int landingIndex)
    {
        if (landingIndex <= 3)
        {
            GameOverController.Instance?.TriggerGameOver();
            return true;
        }
        return false;
    }

    // ================================================================
    //  LEFT DIAGONAL — uses index.indexCountLeft throughout
    // ================================================================

    IEnumerator moveLeftDiognal(Transform child, int childCount)
    {
        if (leftChildObject == null || leftChildObject.Count == 0) yield break;
        if (childCount == 1)
        {
            for (; index.indexCountLeft < leftDiagonalCoordinates.Count; index.indexCountLeft++)
            {
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
                            int landingIdx = index.indexCountLeft - 1;
                            leftflagRadius(landingIdx);
                            DeactivateAllIndicators();
                            leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            DestroyInstantiator();
                            if (!CheckGameOverOnLanding(landingIdx))
                                gameManager.CheckAndDestroyRings();
                            enabled = false;
                            index.indexCountLeft = 2;
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
                                int landingIdx = index.indexCountLeft - 1;
                                leftflagRadius(landingIdx);
                                DeactivateAllIndicators();
                                leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                DestroyInstantiator();
                                if (!CheckGameOverOnLanding(landingIdx))
                                    gameManager.CheckAndDestroyRings();
                                index.indexCountLeft = 2;
                                TryDestroySelf();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                leftChildObject[0].transform.position = leftDiagonalCoordinates[index.indexCountLeft];

                try
                {
                    if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[index.indexCountLeft + 1]))
                    {
                        if (stop == -1) { stop = index.indexCountLeft; stopperID = 1; }
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    if (leftChildObject[0].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 1])
                    {
                        // LANDING SPOT 3
                        int landingIdx = index.indexCountLeft;
                        leftflagRadius(landingIdx);
                        DeactivateAllIndicators();
                        leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        DestroyInstantiator();
                        if (!CheckGameOverOnLanding(landingIdx))
                            gameManager.CheckAndDestroyRings();
                        index.indexCountLeft = 2;
                        enabled = false;
                        TryDestroySelf();
                    }
                    yield break;
                }

                while (gameManager.isRotating)
                    yield return null;

                // Pause while swap-check is running
                while (blockTInstantiator != null && blockTInstantiator.isCheckingSwap)
                    yield return null;

                // ← Hold-to-fast-fall: use fastMoveSpeed while finger is held down
                yield return new WaitForSeconds(HoldDetector.Instance != null && HoldDetector.Instance.isHolding ? fastMoveSpeed : moveSpeed);
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
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[index.indexCountRight]); } catch { stillBlocked = false; }
                        if (stillBlocked)
                        {
                            // LANDING SPOT 4
                            int landingIdx = index.indexCountRight - 1;
                            rightflagRadius(landingIdx);
                            DeactivateAllIndicators();
                            rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            DestroyInstantiator();
                            if (!CheckGameOverOnLanding(landingIdx))
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
                                // LANDING SPOT 5
                                int landingIdx = index.indexCountRight - 1;
                                rightflagRadius(landingIdx);
                                DeactivateAllIndicators();
                                rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                DestroyInstantiator();
                                if (!CheckGameOverOnLanding(landingIdx))
                                    gameManager.CheckAndDestroyRings();
                                index.indexCountRight = 2;
                                TryDestroySelf();
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
                        // LANDING SPOT 6
                        int landingIdx = index.indexCountRight;
                        rightflagRadius(landingIdx);
                        DeactivateAllIndicators();
                        rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        DestroyInstantiator();
                        if (!CheckGameOverOnLanding(landingIdx))
                            gameManager.CheckAndDestroyRings();
                        index.indexCountRight = 2;
                        enabled = false;
                        TryDestroySelf();
                    }
                    yield break;
                }

                while (gameManager.isRotating)
                    yield return null;

                // Pause while swap-check is running
                while (blockTInstantiator != null && blockTInstantiator.isCheckingSwap)
                    yield return null;

                // ← Hold-to-fast-fall: use fastMoveSpeed while finger is held down
                yield return new WaitForSeconds(HoldDetector.Instance != null && HoldDetector.Instance.isHolding ? fastMoveSpeed : moveSpeed);
            }
        }
    }

    // ================================================================
    //  VERTICAL — uses index.indexCountVertical throughout
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
                            // LANDING SPOT 7
                            int landingIdx = index.indexCountVertical - 1;
                            verticalflagRadius(landingIdx);
                            DeactivateAllIndicators();
                            verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                            DestroyInstantiator();
                            if (!CheckGameOverOnLanding(landingIdx))
                                gameManager.CheckAndDestroyRings();
                            index.indexCountVertical = 2;
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
                                int landingIdx = index.indexCountVertical - 1;
                                verticalflagRadius(landingIdx);
                                DeactivateAllIndicators();
                                verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                                DestroyInstantiator();
                                if (!CheckGameOverOnLanding(landingIdx))
                                    gameManager.CheckAndDestroyRings();
                                index.indexCountVertical = 2;
                                TryDestroySelf();
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
                        // LANDING SPOT 9
                        int landingIdx = index.indexCountVertical;
                        verticalflagRadius(landingIdx);
                        DeactivateAllIndicators();
                        verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                        DestroyInstantiator();
                        if (!CheckGameOverOnLanding(landingIdx))
                            gameManager.CheckAndDestroyRings();
                        index.indexCountVertical = 2;
                        enabled = false;
                        TryDestroySelf();
                    }
                    yield break;
                }

                while (gameManager.isRotating)
                    yield return null;

                // Pause while swap-check is running
                while (blockTInstantiator != null && blockTInstantiator.isCheckingSwap)
                    yield return null;

                // ← Hold-to-fast-fall: use fastMoveSpeed while finger is held down
                yield return new WaitForSeconds(HoldDetector.Instance != null && HoldDetector.Instance.isHolding ? fastMoveSpeed : moveSpeed);
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