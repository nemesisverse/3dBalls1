using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class T1Movement : MonoBehaviour, IFallingBlock, ISwitchable
{
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
    private BlockSwitcher blockSwitcher;

    int stop = -1;
    int stopperID = 0;

    // ── ISwitchable ────────────────────────────────────────────────────────
    [HideInInspector] public int  savedIndex = 2;
    [HideInInspector] public bool isPaused   = false;

    public bool IsPaused   { get => isPaused;   set => isPaused = value; }
    public int  SavedIndex => savedIndex;

    // ── Reset helper — call just before every SetParent ───────────────────
    void NotifyLanded()
    {
        savedIndex = 2;
        blockSwitcher?.UnregisterBlock(this);
    }

    void Awake()
    {
        if (gameManager   == null) gameManager   = FindFirstObjectByType<GameManager>();
        if (swipeInput    == null) swipeInput    = FindFirstObjectByType<SwipeInput>();
        if (sphericalGrid == null) sphericalGrid = FindFirstObjectByType<SphericalGrid>();
        if (blockSwitcher == null) blockSwitcher = FindFirstObjectByType<BlockSwitcher>();

        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f)
            leftDiagonalCoordinates.Add(new Vector3(-v, v, 0f));
        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f)
            rightDiagonalCoordinates.Add(new Vector3(v, v, 0f));
        for (float v = 18.5f; v >= 2.5f; v -= 1f)
            verticalCoordinates.Add(new Vector3(0f, v, 0f));
    }

    void Start()
    {
        blockSwitcher?.RegisterBlock(this);   // ← register with switcher
        countChildren();
        CheckChildrenWorldX();
    }

    void TryDestroySelf()
    {
        if (transform.childCount == 0)
            Destroy(gameObject);
    }

    // ================================================================
    //  RIGHT DIAGONAL — T1 offset: position at [i-1], flagRadius at [i-2]
    // ================================================================

    IEnumerator moveRightDiognal(Transform child, int childCount)
    {
        if (rightChildObject == null || rightChildObject.Count == 0) yield break;
        if (childCount == 1)
        {
            for (int i = 2; i < rightDiagonalCoordinates.Count; i++)
            {
                // ── save index + pause point ────────────────────────
                savedIndex = i;
                while (isPaused) yield return null;
                // ───────────────────────────────────────────────────

                if (stop == -1)
                {
                    bool blocked = false;
                    try { blocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[i - 1]); } catch { }
                    if (blocked) { stop = i - 1; stopperID = 2; }
                }
                yield return null;

                if (stop != -1 && i > stop)
                {
                    if (stopperID == 2)
                    {
                        bool stillBlocked = false;
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[i - 1]); } catch { stillBlocked = false; }
                        if (stillBlocked)
                        {
                            // LANDING SPOT 1
                            rightflagRadius(i - 2);
                            NotifyLanded();
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
                                // LANDING SPOT 2
                                rightflagRadius(i - 2);
                                NotifyLanded();
                                rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                gameManager.CheckAndDestroyRings();
                                TryDestroySelf();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                rightChildObject[0].transform.position = rightDiagonalCoordinates[i - 1];

                if (i + 1 < rightDiagonalCoordinates.Count)
                {
                    if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[i]))
                    {
                        if (stop == -1) { stop = i; stopperID = 2; }
                    }
                }
                else
                {
                    if (rightChildObject[0].transform.position == rightDiagonalCoordinates[rightDiagonalCoordinates.Count - 2])
                    {
                        // LANDING SPOT 3
                        rightflagRadius(i - 1);
                        NotifyLanded();
                        rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
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
    //  VERTICAL — 3 children (T1-specific)
    // ================================================================

    IEnumerator moveVertical(Transform child, int childCount)
    {
        if (verticalChildObject == null || verticalChildObject.Count == 0) yield break;
        if (childCount == 3)
        {
            for (int i = 2; i < verticalCoordinates.Count; i++)
            {
                // ── save index + pause point ────────────────────────
                savedIndex = i;
                while (isPaused) yield return null;
                // ───────────────────────────────────────────────────

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
                            // LANDING SPOT 4
                            verticalflagRadius(i - 1);
                            NotifyLanded();
                            verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                            verticalChildObject[2].transform.SetParent(gameManager.motherPlatform.transform, true);
                            //gameManager.CheckAndDestroyRings();
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
                                verticalflagRadius(i - 1);
                                NotifyLanded();
                                verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                                verticalChildObject[2].transform.SetParent(gameManager.motherPlatform.transform, true);
                                //gameManager.CheckAndDestroyRings();
                                //TryDestroySelf();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                verticalChildObject[0].transform.position = verticalCoordinates[i];
                verticalChildObject[1].transform.position = verticalCoordinates[i - 1];
                verticalChildObject[2].transform.position = verticalCoordinates[i - 2];

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
                        verticalChildObject[1].transform.position == verticalCoordinates[verticalCoordinates.Count - 2] &&
                        verticalChildObject[2].transform.position == verticalCoordinates[verticalCoordinates.Count - 3])
                    {
                        // LANDING SPOT 6
                        verticalflagRadius(i);
                        NotifyLanded();
                        verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                        verticalChildObject[2].transform.SetParent(gameManager.motherPlatform.transform, true);
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
        leftDiagonalCount = 0; rightDiagonalCount = 0; verticalCount = 0;
        foreach (Transform child in transform)
            if (child.position.x < 0f) { leftDiagonalCount++; leftChildObject.Add(child.gameObject); }
        foreach (Transform child in transform)
            if (child.position.x > 0f) { rightDiagonalCount++; rightChildObject.Add(child.gameObject); }
        foreach (Transform child in transform)
            if (child.position.x == 0f) { verticalCount++; verticalChildObject.Add(child.gameObject); }
    }

    void CheckChildrenWorldX()
    {
        bool rightStarted = false, verticalStarted = false;
        foreach (Transform child in transform)
        {
            float worldX = child.position.x;
            if      (worldX > 0f && !rightStarted)     { StartCoroutine(moveRightDiognal(child, rightDiagonalCount)); rightStarted    = true; }
            else if (worldX == 0f && !verticalStarted) { StartCoroutine(moveVertical(child, verticalCount));          verticalStarted = true; }
        }
    }

    // ================================================================
    //  FLAG RADIUS
    // ================================================================

    void rightflagRadius(int i)
    {
        sphericalGrid.PlaceBlockByWorldPosition(
            rightChildObject[0].transform.position, i,
            rightChildObject[0], gameManager.motherPlatform.transform);
    }

    void verticalflagRadius(int i)
    {
        // 3 blocks at adjacent radius levels
        sphericalGrid.PlaceBlockByWorldPosition(
            verticalChildObject[0].transform.position, i,
            verticalChildObject[0], gameManager.motherPlatform.transform);
        sphericalGrid.PlaceBlockByWorldPosition(
            verticalChildObject[1].transform.position, i - 1,
            verticalChildObject[1], gameManager.motherPlatform.transform);
        sphericalGrid.PlaceBlockByWorldPosition(
            verticalChildObject[2].transform.position, i - 2,
            verticalChildObject[2], gameManager.motherPlatform.transform);
    }
}