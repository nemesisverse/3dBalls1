using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Z1Movement : MonoBehaviour, IFallingBlock
{
    public IndexManager index;
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
    private BlockZInstantiator zInstantiator;

    // ================================================================
    //  Block Guide / Indicator system
    //  blockGuide is resolved at runtime via Find because Z1Movement is a
    //  prefab instantiated during gameplay — a scene object cannot be
    //  pre-assigned in the prefab's Inspector field.
    //  z1MovementIndicators — the child names that belong to this block shape.
    // ================================================================
    private GameObject blockGuide;
    private static readonly string[] z1MovementIndicators = { "(1,1)", "(2,1)", "(2,2)", "(3,2)" };

    int stop      = -1;
    int stopperID =  0;

    // ================================================================
    //  AWAKE / START
    // ================================================================

    void Awake()
    {
        if (gameManager    == null) gameManager    = FindFirstObjectByType<GameManager>();
        if (swipeInput     == null) swipeInput     = FindFirstObjectByType<SwipeInput>();
        if (sphericalGrid  == null) sphericalGrid  = FindFirstObjectByType<SphericalGrid>();
        if (index          == null) index          = FindFirstObjectByType<IndexManager>();
        if (zInstantiator  == null) zInstantiator  = FindFirstObjectByType<BlockZInstantiator>();
        blockGuide = GameObject.Find("Block Guide");

        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f)
            leftDiagonalCoordinates.Add(new Vector3(-v, v, 0f));
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
    //  Destroys the BlockZInstantiator whenever a block lands
    // ================================================================

    void DestroyInstantiator()
    {
        if (zInstantiator != null)
            Destroy(zInstantiator.gameObject);
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

        // Activate only the cells that belong to Z1Movement
        foreach (string indicatorName in z1MovementIndicators)
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
            for (; index.indexCountLeft < leftDiagonalCoordinates.Count; index.indexCountLeft++)
            {
                // Pre-move look-ahead: lead block's upcoming position is [i-1].
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
                        // FIX: check [i-1] (same position that triggered the stop), not [i]
                        bool stillBlocked = false;
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[index.indexCountLeft - 1]); }
                        catch { stillBlocked = false; }

                        if (stillBlocked)
                        {
                            // LANDING SPOT 1 — block[0] rests at [i-2], block[1] at [i-3]
                            leftflagRadius(index.indexCountLeft - 2);
                            DeactivateAllIndicators();
                            leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            leftChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
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
                                // LANDING SPOT 2
                                leftflagRadius(index.indexCountLeft - 2);
                                DeactivateAllIndicators();
                                leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                leftChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                                DestroyInstantiator();
                                gameManager.CheckAndDestroyRings();
                                index.indexCountLeft = 2;
                                TryDestroySelf();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                // Move both blocks: lead to [i-1], trail to [i-2]
                leftChildObject[0].transform.position = leftDiagonalCoordinates[index.indexCountLeft - 1];
                leftChildObject[1].transform.position = leftDiagonalCoordinates[index.indexCountLeft - 2];

                // FIX: post-move look-ahead checks [i] — the lead block's NEXT position.
                // Guard with i+1 < Count so the else branch handles the end-of-track case.
                if (index.indexCountLeft + 1 < leftDiagonalCoordinates.Count)
                {
                    if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[index.indexCountLeft]))
                    {
                        if (stop == -1) { stop = index.indexCountLeft; stopperID = 1; }
                    }
                }
                else
                {
                    // End of track: block[0] at [Count-2], block[1] at [Count-3]
                    if (leftChildObject[0].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 2] &&
                        leftChildObject[1].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 3])
                    {
                        // LANDING SPOT 3
                        leftflagRadius(index.indexCountLeft - 1);
                        DeactivateAllIndicators();
                        leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        leftChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                        DestroyInstantiator();
                        gameManager.CheckAndDestroyRings();
                        index.indexCountLeft = 2;
                        enabled = false;
                        TryDestroySelf();
                    }
                    yield break;
                }

                while (gameManager.isRotating)
                    yield return null;

                // Freeze while BlockZInstantiator is running its swap check
                while (zInstantiator != null && zInstantiator.isCheckingSwap)
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
            index.indexCountRight = index.indexCountRight - 1;
            for (; index.indexCountVertical < verticalCoordinates.Count; index.indexCountVertical++)
            {
                index.indexCountRight++;
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
                            // LANDING SPOT 4
                            verticalflagRadius(index.indexCountVertical - 1);
                            DeactivateAllIndicators();
                            verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                            DestroyInstantiator();
                            //gameManager.CheckAndDestroyRings();
                            index.indexCountVertical = 2;
                            index.indexCountRight    = 2;
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
                                // LANDING SPOT 5
                                verticalflagRadius(index.indexCountVertical - 1);
                                DeactivateAllIndicators();
                                verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                                DestroyInstantiator();
                                //gameManager.CheckAndDestroyRings();
                                index.indexCountVertical = 2;
                                index.indexCountRight    = 2;
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
                        // LANDING SPOT 6
                        verticalflagRadius(index.indexCountVertical);
                        DeactivateAllIndicators();
                        verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                        DestroyInstantiator();
                        //gameManager.CheckAndDestroyRings();
                        index.indexCountVertical = 2;
                        index.indexCountRight    = 2;
                        enabled = false;
                        //TryDestroySelf();
                    }
                    yield break;
                }

                while (gameManager.isRotating)
                    yield return null;

                // Freeze while BlockZInstantiator is running its swap check
                while (zInstantiator != null && zInstantiator.isCheckingSwap)
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
            else if (worldX == 0f && !verticalStarted) { StartCoroutine(moveVertical(child,     verticalCount));     verticalStarted = true; }
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