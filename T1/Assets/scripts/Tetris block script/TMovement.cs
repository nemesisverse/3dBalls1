using System.Collections;
using System.Collections.Generic;
using Unity.Android.Gradle;
using UnityEngine;

public class TMovement : MonoBehaviour
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
    public SliderPedestalController1 sliderController;

    // Optimized list for collision checking
    private List<List<GameObject>> allDimensions;
    Vector3 globalNormalX = Vector3.right; //YZ plane ke liye
    Vector3 globalNormalZ = Vector3.forward; // XY plane ke liye

    void Awake()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (swipeInput == null) swipeInput = FindFirstObjectByType<SwipeInput>();
        if (sliderController == null) sliderController = FindFirstObjectByType<SliderPedestalController1>();

        // Populate Coordinates
        for (float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f) leftDiagonalCoordinates.Add(new Vector3(-v, v, 0f));
        for (float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f) rightDiagonalCoordinates.Add(new Vector3(v, v, 0f));
        for (float v = 14.5f; v >= 2.5f; v -= 1f) verticalCoordinates.Add(new Vector3(0f, v, 0f));
    }

    void Start()
    {
        countChildren();
        CheckChildrenWorldX();

        // Initialize the Grid List ONCE to save performance
        allDimensions = new List<List<GameObject>>
        {
            gameManager.plusXDimension, gameManager.plusYDimension, gameManager.plusZDimension,
            gameManager.minusXDimension, gameManager.minusYDimension, gameManager.minusZDimension,
            gameManager.plusYplusZDimension, gameManager.plusYminusZDimension,
            gameManager.minusYplusZDimension, gameManager.minusYminusZDimension,
            gameManager.minusXminusZDimension, gameManager.minusXplusZDimension,
            gameManager.plusXminusZDimension, gameManager.plusXplusZDimension,
            gameManager.minusXplusYDimension, gameManager.plusXplusYDimension,
            gameManager.minusXminusYDimension, gameManager.plusXminusYDimension
        };
    }

    // ------------------------------------------------------------------------
    // COLLISION LOGIC (CALLED BY SLIDER & SWIPE)
    // ------------------------------------------------------------------------
    public bool IsRotationColliding()
    {
        List<GameObject> activeMovingChildren = new List<GameObject>();
        if (leftChildObject != null) activeMovingChildren.AddRange(leftChildObject);
        if (rightChildObject != null) activeMovingChildren.AddRange(rightChildObject);
        if (verticalChildObject != null) activeMovingChildren.AddRange(verticalChildObject);

        if (activeMovingChildren.Count == 0) return false;

        if (allDimensions == null) return false;

        foreach (var dimensionList in allDimensions)
        {
            if (dimensionList == null) continue;

            foreach (var placedBlock in dimensionList)
            {
                if (placedBlock == null) continue;

                foreach (var movingBlock in activeMovingChildren)
                {
                    if (movingBlock == null) continue;
                    if (placedBlock == movingBlock) continue; // Ignore self

                    if (ArePositionsOverlapping(placedBlock.transform.position, movingBlock.transform.position))
                    {
                        Debug.Log($"Collision detected! Placed: {placedBlock.name} | Moving: {movingBlock.name}");
                        return true;
                    }
                }
            }
        }
        return false;
    }

    bool ArePositionsOverlapping(Vector3 posA, Vector3 posB)
    {
        float x1 = (float)System.Math.Round(posA.x, 2);
        float y1 = (float)System.Math.Round(posA.y, 2);
        float z1 = (float)System.Math.Round(posA.z, 2);

        float x2 = (float)System.Math.Round(posB.x, 2);
        float y2 = (float)System.Math.Round(posB.y, 2);
        float z2 = (float)System.Math.Round(posB.z, 2);

        return (x1 == x2) && (y1 == y2) && (z1 == z2);
    }

    // ------------------------------------------------------------------------
    // MOVEMENT COROUTINES
    // ------------------------------------------------------------------------
    int stop = -1;
    int stopperID = 0;

    // Helper to re-enable slider when block is placed (Finished)
    void ResetSliderPermissions()
    {
        if (sliderController != null)
        {
            sliderController.allowDecrease = true;
            sliderController.allowIncrease = true;
        }
    }

    IEnumerator moveLeftDiognal(Transform child, int childCount)
    {
        if (leftChildObject == null || leftChildObject.Count == 0) yield break;
        if (childCount == 1)
        {
            for (int i = 2; i < leftDiagonalCoordinates.Count; i++)
            {
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
                            leftflagRadius(i);
                            leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            ResetSliderPermissions(); // Enable slider when done
                            enabled = false;
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
                                leftflagRadius(i);
                                leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                ResetSliderPermissions();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                leftChildObject[0].transform.position = leftDiagonalCoordinates[i];

                try { if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[i + 1])) { if (stop == -1) { stop = i; stopperID = 1; } } }
                catch (System.ArgumentOutOfRangeException)
                {
                    if (leftChildObject[0].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 1])
                    {
                        leftflagRadius(i + 1);
                        leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        ResetSliderPermissions(); // Enable slider when done
                        enabled = false;
                    }
                    yield break;
                }
                yield return new WaitForSeconds(moveSpeed);
            }
        }
    }

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
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[i]); } catch { stillBlocked = false; }
                        if (stillBlocked)
                        {
                            rightflagRadius(i);
                            rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            ResetSliderPermissions(); // Enable slider when done
                            enabled = false;
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
                                rightflagRadius(i);
                                rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                ResetSliderPermissions();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                rightChildObject[0].transform.position = rightDiagonalCoordinates[i];

                try { if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[i + 1])) { if (stop == -1) { stop = i; stopperID = 2; } } }
                catch (System.ArgumentOutOfRangeException)
                {
                    if (rightChildObject[0].transform.position == rightDiagonalCoordinates[rightDiagonalCoordinates.Count - 1])
                    {
                        rightflagRadius(i + 1);
                        rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        ResetSliderPermissions(); // Enable slider when done
                        enabled = false;
                    }
                    yield break;
                }
                yield return new WaitForSeconds(moveSpeed);
            }
        }
    }

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
                        try { stillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, verticalCoordinates[i]); } catch { stillBlocked = false; }
                        if (stillBlocked)
                        {
                            verticalflagRadius(i);
                            verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                            ResetSliderPermissions(); // Enable slider when done
                            
                            enabled = false;
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
                                verticalflagRadius(i);
                                verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                                ResetSliderPermissions();
                       
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                verticalChildObject[0].transform.position = verticalCoordinates[i];
                verticalChildObject[1].transform.position = verticalCoordinates[i - 1];

                // --- YOUR LOGIC: Check & Lock Slider Directions ---
                if (sliderController != null)
                {
                    // 1. Reset permissions to TRUE at the start of every step
                    sliderController.allowDecrease = true;
                    sliderController.allowIncrease = true;

                    // 2. Check Decrease Condition
                    if (preventDecreasingValueSlider(i))
                    {
                        sliderController.allowDecrease = false;
                    }

                    // 3. Check Increase Condition
                    if (preventIncreasingValueSlider(i))
                    {
                        sliderController.allowIncrease = false;
                    }
                }
                // --------------------------------------------------

                try { if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, verticalCoordinates[i + 1])) { if (stop == -1) { stop = i; stopperID = 3; } } }
                catch (System.ArgumentOutOfRangeException)
                {
                    if (verticalChildObject[0].transform.position == verticalCoordinates[verticalCoordinates.Count - 1] &&
                        verticalChildObject[1].transform.position == verticalCoordinates[verticalCoordinates.Count - 2])
                    {
                        verticalflagRadius(i + 1);
                        verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                        ResetSliderPermissions(); // Enable slider when done
                      
                        enabled = false;
                    }
                    yield break;
                }
                yield return new WaitForSeconds(moveSpeed);
            }
        }
    }

    // ------------------------------------------------------------------------
    // HELPER FUNCTIONS 
    // ------------------------------------------------------------------------

    void countChildren()
    {
        leftDiagonalCount = 0; rightDiagonalCount = 0; verticalCount = 0;
        foreach (Transform child in transform) { if (child.position.x < 0f) { leftDiagonalCount++; leftChildObject.Add(child.gameObject); } }
        foreach (Transform child in transform) { if (child.position.x > 0f) { rightDiagonalCount++; rightChildObject.Add(child.gameObject); } }
        foreach (Transform child in transform) { if (child.position.x == 0f) { verticalCount++; verticalChildObject.Add(child.gameObject); } }
    }

    void CheckChildrenWorldX()
    {
        bool leftStarted = false, rightStarted = false, verticalStarted = false;
        foreach (Transform child in transform)
        {
            float worldX = child.position.x;
            if (worldX < 0f && !leftStarted) { StartCoroutine(moveLeftDiognal(child, leftDiagonalCount)); leftStarted = true; }
            else if (worldX == 0f && !verticalStarted) { StartCoroutine(moveVertical(child, verticalCount)); verticalStarted = true; }
            else if (worldX > 0f && !rightStarted) { StartCoroutine(moveRightDiognal(child, rightDiagonalCount)); rightStarted = true; }
        }
    }

    // --- LOGIC TO PREVENT SLIDER MOVEMENT ---
    bool preventDecreasingValueSlider(int i)
    {
        if (allDimensions == null) return false;

        // Safety checks for vertical count
        if (verticalChildObject == null || verticalChildObject.Count == 0) return false;
        if (i < 0) return false;

        for (int d = 0; d < allDimensions.Count; d++)
        {
            if (i < allDimensions[d].Count)
            {
                if (allDimensions[d][i] != null && allDimensions[d][i].transform.position.x > 0f && allDimensions[d][i].transform.position.y >= 0f)
                {
                    Debug.Log("prevent decreasing the value");
                    return true;
                }
            }
        }
        return false;
    }

    bool preventIncreasingValueSlider(int i)
    {
        if (allDimensions == null) return false;

        // Safety checks for vertical count
        if (verticalChildObject == null || verticalChildObject.Count == 0) return false;
        if (i < 0) return false;

        for (int d = 0; d < allDimensions.Count; d++)
        {
            if (i < allDimensions[d].Count)
            {
                if (allDimensions[d][i] != null && allDimensions[d][i].transform.position.x < 0f && allDimensions[d][i].transform.position.y >= 0f)
                {
                    Debug.Log("prevent increasing the value");
                    return true;
                }
            }
        }
        return false;
    }


    // --- RADIUS FLAG FUNCTIONS ---
    void leftflagRadius(int i)
    {
        if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = leftChildObject[0];
        else if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = leftChildObject[0];
        else if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = leftChildObject[0];
        else if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = leftChildObject[0];
        else if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = leftChildObject[0];
        else if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = leftChildObject[0];
        else if (gameManager.plusYplusZDimension[i - 1] == null) gameManager.plusYplusZDimension[i - 1] = leftChildObject[0];
        else if (gameManager.plusYminusZDimension[i - 1] == null) gameManager.plusYminusZDimension[i - 1] = leftChildObject[0];
        else if (gameManager.minusYplusZDimension[i - 1] == null) gameManager.minusYplusZDimension[i - 1] = leftChildObject[0];
        else if (gameManager.minusYminusZDimension[i - 1] == null) gameManager.minusYminusZDimension[i - 1] = leftChildObject[0];
        else if (gameManager.minusXminusZDimension[i - 1] == null) gameManager.minusXminusZDimension[i - 1] = leftChildObject[0];
        else if (gameManager.minusXplusZDimension[i - 1] == null) gameManager.minusXplusZDimension[i - 1] = leftChildObject[0];
        else if (gameManager.plusXminusZDimension[i - 1] == null) gameManager.plusXminusZDimension[i - 1] = leftChildObject[0];
        else if (gameManager.plusXplusZDimension[i - 1] == null) gameManager.plusXplusZDimension[i - 1] = leftChildObject[0];
        else if (gameManager.minusXplusYDimension[i - 1] == null) gameManager.minusXplusYDimension[i - 1] = leftChildObject[0];
        else if (gameManager.plusXplusYDimension[i - 1] == null) gameManager.plusXplusYDimension[i - 1] = leftChildObject[0];
        else if (gameManager.minusXminusYDimension[i - 1] == null) gameManager.minusXminusYDimension[i - 1] = leftChildObject[0];
        else if (gameManager.plusXminusYDimension[i - 1] == null) gameManager.plusXminusYDimension[i - 1] = leftChildObject[0];
    }

    void rightflagRadius(int i)
    {
        if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = rightChildObject[0];
        else if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = rightChildObject[0];
        else if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = rightChildObject[0];
        else if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = rightChildObject[0];
        else if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = rightChildObject[0];
        else if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = rightChildObject[0];
        else if (gameManager.plusYplusZDimension[i - 1] == null) gameManager.plusYplusZDimension[i - 1] = rightChildObject[0];
        else if (gameManager.plusYminusZDimension[i - 1] == null) gameManager.plusYminusZDimension[i - 1] = rightChildObject[0];
        else if (gameManager.minusYplusZDimension[i - 1] == null) gameManager.minusYplusZDimension[i - 1] = rightChildObject[0];
        else if (gameManager.minusYminusZDimension[i - 1] == null) gameManager.minusYminusZDimension[i - 1] = rightChildObject[0];
        else if (gameManager.minusXminusZDimension[i - 1] == null) gameManager.minusXminusZDimension[i - 1] = rightChildObject[0];
        else if (gameManager.minusXplusZDimension[i - 1] == null) gameManager.minusXplusZDimension[i - 1] = rightChildObject[0];
        else if (gameManager.plusXminusZDimension[i - 1] == null) gameManager.plusXminusZDimension[i - 1] = rightChildObject[0];
        else if (gameManager.plusXplusZDimension[i - 1] == null) gameManager.plusXplusZDimension[i - 1] = rightChildObject[0];
        else if (gameManager.minusXplusYDimension[i - 1] == null) gameManager.minusXplusYDimension[i - 1] = rightChildObject[0];
        else if (gameManager.plusXplusYDimension[i - 1] == null) gameManager.plusXplusYDimension[i - 1] = rightChildObject[0];
        else if (gameManager.minusXminusYDimension[i - 1] == null) gameManager.minusXminusYDimension[i - 1] = rightChildObject[0];
        else if (gameManager.plusXminusYDimension[i - 1] == null) gameManager.plusXminusYDimension[i - 1] = rightChildObject[0];
    }

    void verticalflagRadius(int i)
    {
        if (gameManager.plusXDimension[i - 1] == null && gameManager.plusXDimension[i - 2] == null) { gameManager.plusXDimension[i - 1] = verticalChildObject[0]; gameManager.plusXDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.plusYDimension[i - 1] == null && gameManager.plusYDimension[i - 2] == null) { gameManager.plusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusYDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.plusZDimension[i - 1] == null && gameManager.plusZDimension[i - 2] == null) { gameManager.plusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusZDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.minusXDimension[i - 1] == null && gameManager.minusXDimension[i - 2] == null) { gameManager.minusXDimension[i - 1] = verticalChildObject[0]; gameManager.minusXDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.minusYDimension[i - 1] == null && gameManager.minusYDimension[i - 2] == null) { gameManager.minusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusYDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.minusZDimension[i - 1] == null && gameManager.minusZDimension[i - 2] == null) { gameManager.minusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusZDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.plusYplusZDimension[i - 1] == null && gameManager.plusYplusZDimension[i - 2] == null) { gameManager.plusYplusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusYplusZDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.plusYminusZDimension[i - 1] == null && gameManager.plusYminusZDimension[i - 2] == null) { gameManager.plusYminusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusYminusZDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.minusYplusZDimension[i - 1] == null && gameManager.minusYplusZDimension[i - 2] == null) { gameManager.minusYplusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusYplusZDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.minusYminusZDimension[i - 1] == null && gameManager.minusYminusZDimension[i - 2] == null) { gameManager.minusYminusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusYminusZDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.minusXminusZDimension[i - 1] == null && gameManager.minusXminusZDimension[i - 2] == null) { gameManager.minusXminusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusXminusZDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.minusXplusZDimension[i - 1] == null && gameManager.minusXplusZDimension[i - 2] == null) { gameManager.minusXplusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusXplusZDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.plusXminusZDimension[i - 1] == null && gameManager.plusXminusZDimension[i - 2] == null) { gameManager.plusXminusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusXminusZDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.plusXplusZDimension[i - 1] == null && gameManager.plusXplusZDimension[i - 2] == null) { gameManager.plusXplusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusXplusZDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.minusXplusYDimension[i - 1] == null && gameManager.minusXplusYDimension[i - 2] == null) { gameManager.minusXplusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusXplusYDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.plusXplusYDimension[i - 1] == null && gameManager.plusXplusYDimension[i - 2] == null) { gameManager.plusXplusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusXplusYDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.minusXminusYDimension[i - 1] == null && gameManager.minusXminusYDimension[i - 2] == null) { gameManager.minusXminusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusXminusYDimension[i - 2] = verticalChildObject[1]; }
        else if (gameManager.plusXminusYDimension[i - 1] == null && gameManager.plusXminusYDimension[i - 2] == null) { gameManager.plusXminusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusXminusYDimension[i - 2] = verticalChildObject[1]; }
    }
}
