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
    // void ResetSliderPermissions()
    // {
    //     if (sliderController != null)
    //     {
    //         sliderController.allowDecrease = true;
    //         sliderController.allowIncrease = true;
    //     }
    // }

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
                            //ResetSliderPermissions(); // Enable slider when done
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
                                //ResetSliderPermissions();
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
                        //ResetSliderPermissions(); // Enable slider when done
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
                            //ResetSliderPermissions(); // Enable slider when done
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
                                //ResetSliderPermissions();
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
                        //ResetSliderPermissions(); // Enable slider when done
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
                            //ResetSliderPermissions(); // Enable slider when done
                            gameManager.checkRingToDestroy();
                            gameManager.checkXZRingToDestroy();
                            gameManager.checkYZRingToDestroy();
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
                                //ResetSliderPermissions();
                                gameManager.checkRingToDestroy();
                                gameManager.checkXZRingToDestroy();
                                gameManager.checkYZRingToDestroy();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                verticalChildObject[0].transform.position = verticalCoordinates[i];
                verticalChildObject[1].transform.position = verticalCoordinates[i - 1];

                // --- YOUR LOGIC: Check & Lock Slider Directions ---
                // if (sliderController != null)
                // {
                //     // 1. Reset permissions to TRUE at the start of every step
                //     sliderController.allowDecrease = true;
                //     sliderController.allowIncrease = true;

                //     // 2. Check Decrease Condition
                //     if (preventDecreasingValueSlider(i))
                //     {
                //         sliderController.allowDecrease = false;
                //     }

                //     // 3. Check Increase Condition
                //     if (preventIncreasingValueSlider(i))
                //     {
                //         sliderController.allowIncrease = false;
                //     }
                // }
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
                        //ResetSliderPermissions(); // Enable slider when done
                        gameManager.checkRingToDestroy();
                        gameManager.checkXZRingToDestroy();
                        gameManager.checkYZRingToDestroy();
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
    // bool preventDecreasingValueSlider(int i)
    // {
    //     if (allDimensions == null) return false;

    //     // Safety checks for vertical count
    //     if (verticalChildObject == null || verticalChildObject.Count == 0) return false;
    //     if (i < 0) return false;

    //     for (int d = 0; d < allDimensions.Count; d++)
    //     {
    //         if (i < allDimensions[d].Count)
    //         {
    //             if (allDimensions[d][i] != null && allDimensions[d][i].transform.position.x > 0f && allDimensions[d][i].transform.position.y >= 0f)
    //             {
    //                 Debug.Log("prevent decreasing the value");
    //                 return true;
    //             }
    //         }
    //     }
    //     return false;
    // }

    // bool preventIncreasingValueSlider(int i)
    // {
    //     if (allDimensions == null) return false;

    //     // Safety checks for vertical count
    //     if (verticalChildObject == null || verticalChildObject.Count == 0) return false;
    //     if (i < 0) return false;

    //     for (int d = 0; d < allDimensions.Count; d++)
    //     {
    //         if (i < allDimensions[d].Count)
    //         {
    //             if (allDimensions[d][i] != null && allDimensions[d][i].transform.position.x < 0f && allDimensions[d][i].transform.position.y >= 0f)
    //             {
    //                 Debug.Log("prevent increasing the value");
    //                 return true;
    //             }
    //         }
    //     }
    //     return false;
    // }


    // --- RADIUS FLAG FUNCTIONS ---
    void leftflagRadius(int i)
    {
        // 1. Get the Direction the Left Child is pointing
        // Note: Since 'leftChildObject' is a child, we need its world direction relative to the center.
        // However, usually for these logic blocks, we check the MOTHER platform's rotation axes to know which "World" direction aligns with the "Local" Left Diagonal.
        // Ideally, we check the specific child's position direction, but based on your previous code, 
        // we use the Mother Platform's axes to determine orientation.

        // Let's use the Platform's axes to determine orientation, then check components.
        float align = 0.9f;
        float diag = 0.5f;

        float angleYZ = Mathf.Atan2(transform.right.y, transform.right.x) * Mathf.Rad2Deg;
        float angleZX = Mathf.Atan2(transform.up.y, transform.up.x) * Mathf.Rad2Deg;
        float angleXY = Mathf.Atan2(transform.forward.y, transform.forward.x) * Mathf.Rad2Deg;
        float tolerance = 1.0f;

        // ==========================================================================================
        // 1. TILTED LOGIC (Complex Rotations)
        // ==========================================================================================

        // Condition: Local XY Parallel to Global XY (Normal Z aligned)
        if ((Mathf.Abs(Vector3.Dot(transform.forward, Vector3.forward)) > 0.98f) &&
        (Mathf.Abs(Mathf.Abs(angleYZ) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleYZ) - 135f) <= tolerance) &&
        (Mathf.Abs(Mathf.Abs(angleZX) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleZX) - 135f) <= tolerance))
        {
            // Debug.Log("SUCCESS: Object is flat on XY, and both YZ & ZX planes are at 45° diagonals!");

            // Check YZ Plane Angle
            if (Mathf.Abs(angleYZ - 45f) <= tolerance || Mathf.Abs(angleYZ - (-135f)) <= tolerance)
            {
                // Debug.Log("loca YZ was making +45 with global YZ");
                // Use Direction Logic if possible, or fallback to greedy for complex diagonals
                // Since this is specific to "Left Diagonal", we usually hardcode the mapping or check the specific vector.
                // For now, I will keep your existing logic for TILTED as it is specific to the angle, 
                // but ensure we don't overwrite if not null.

                if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusYplusZDimension[i - 1] == null) gameManager.plusYplusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusYminusZDimension[i - 1] == null) gameManager.plusYminusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusYplusZDimension[i - 1] == null) gameManager.minusYplusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusYminusZDimension[i - 1] == null) gameManager.minusYminusZDimension[i - 1] = leftChildObject[0];
            }
            // ZX making +45 degree angle
            else if (Mathf.Abs(angleZX - 45f) <= tolerance || Mathf.Abs(angleZX - (-135f)) <= tolerance)
            {
                // Debug.Log("local ZX making +ve angle");
                if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusXminusZDimension[i - 1] == null) gameManager.minusXminusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusXplusZDimension[i - 1] == null) gameManager.minusXplusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusXminusZDimension[i - 1] == null) gameManager.plusXminusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusXplusZDimension[i - 1] == null) gameManager.plusXplusZDimension[i - 1] = leftChildObject[0];
            }
        }

        // Condition: Local YZ Normal (Right) is Parallel to Global XY Normal (Forward)
        else if ((Mathf.Abs(Vector3.Dot(transform.right, Vector3.forward)) > 0.98f) &&
        (Mathf.Abs(Mathf.Abs(angleXY) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleXY) - 135f) <= tolerance) &&
        (Mathf.Abs(Mathf.Abs(angleZX) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleZX) - 135f) <= tolerance))
        {
            // Debug.Log("SUCCESS: Local YZ is flat on XY, and both XY & ZX planes are at 45° diagonals!");
            // [Local XY] is +45° on left side
            if (Mathf.Abs(angleXY - 45f) <= tolerance || Mathf.Abs(angleXY - (-135f)) <= tolerance)
            {
                if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusXplusYDimension[i - 1] == null) gameManager.minusXplusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusXplusYDimension[i - 1] == null) gameManager.plusXplusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusXminusYDimension[i - 1] == null) gameManager.minusXminusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusXminusYDimension[i - 1] == null) gameManager.plusXminusYDimension[i - 1] = leftChildObject[0];
            }
            // ZX tilted left side
            else if (Mathf.Abs(angleZX - 45f) <= tolerance || Mathf.Abs(angleZX - (-135f)) <= tolerance)
            {
                if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusXminusZDimension[i - 1] == null) gameManager.minusXminusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusXplusZDimension[i - 1] == null) gameManager.minusXplusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusXminusZDimension[i - 1] == null) gameManager.plusXminusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusXplusZDimension[i - 1] == null) gameManager.plusXplusZDimension[i - 1] = leftChildObject[0];
            }
        }

        // Condition: Local ZX Normal (Up) is Parallel to Global XY Normal (Forward)
        else if ((Mathf.Abs(Vector3.Dot(transform.up, Vector3.forward)) > 0.98f) &&
            (Mathf.Abs(Mathf.Abs(angleYZ) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleYZ) - 135f) <= tolerance) &&
            (Mathf.Abs(Mathf.Abs(angleXY) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleXY) - 135f) <= tolerance))
        {
            // Debug.Log("SUCCESS: Local ZX is flat on XY, and both YZ & XY planes are at 45° diagonals!");
            // YZ 
            if (Mathf.Abs(angleYZ - 45f) <= tolerance || Mathf.Abs(angleYZ - (-135f)) <= tolerance)
            {
                if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusYplusZDimension[i - 1] == null) gameManager.plusYplusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusYminusZDimension[i - 1] == null) gameManager.plusYminusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusYplusZDimension[i - 1] == null) gameManager.minusYplusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusYminusZDimension[i - 1] == null) gameManager.minusYminusZDimension[i - 1] = leftChildObject[0];
            }
            // XY plane
            else if (Mathf.Abs(angleXY - 45f) <= tolerance || Mathf.Abs(angleXY - (-135f)) <= tolerance)
            {
                if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusXplusYDimension[i - 1] == null) gameManager.minusXplusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusXplusYDimension[i - 1] == null) gameManager.plusXplusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusXminusYDimension[i - 1] == null) gameManager.minusXminusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusXminusYDimension[i - 1] == null) gameManager.plusXminusYDimension[i - 1] = leftChildObject[0];
            }
        }

        // ==========================================================================================
        // 2. ALIGNED LOGIC (Applied Direction Awareness Here)
        // ==========================================================================================

        // We calculate the actual world position of the block to know where it is
        Vector3 blockPos = leftChildObject[0].transform.position;
        Vector3 dir = blockPos.normalized; // Direction from center (0,0,0)

        // 1. Check if Local XY Plane is the one aligned
        if (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, globalNormalZ)) > 0.98f)
        {
            Debug.Log("left Aligned Plane: Local XY (Axes: Right & Up) with XY");

            // Strict Direction Check using World Position
            if (dir.x > align) { if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = leftChildObject[0]; }
            else if (dir.x < -align) { if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = leftChildObject[0]; }
            else if (dir.y > align) { if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = leftChildObject[0]; }
            else if (dir.y < -align) { if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = leftChildObject[0]; }

            // Diagonals
            else if (dir.x < -diag && dir.y > diag) { if (gameManager.minusXplusYDimension[i - 1] == null) gameManager.minusXplusYDimension[i - 1] = leftChildObject[0]; }
            else if (dir.x > diag && dir.y > diag) { if (gameManager.plusXplusYDimension[i - 1] == null) gameManager.plusXplusYDimension[i - 1] = leftChildObject[0]; }
            else if (dir.x < -diag && dir.y < -diag) { if (gameManager.minusXminusYDimension[i - 1] == null) gameManager.minusXminusYDimension[i - 1] = leftChildObject[0]; }
            else if (dir.x > diag && dir.y < -diag) { if (gameManager.plusXminusYDimension[i - 1] == null) gameManager.plusXminusYDimension[i - 1] = leftChildObject[0]; }
        }

        // 2. Check if Local YZ Plane is the one aligned with XY
        else if (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, globalNormalZ)) > 0.98f)
        {
            Debug.Log("left Aligned Plane: Local YZ (Axes: Up & Forward) with XY");

            if (dir.z < -align) { if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = leftChildObject[0]; }
            else if (dir.z > align) { if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = leftChildObject[0]; }
            else if (dir.y > align) { if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = leftChildObject[0]; }
            else if (dir.y < -align) { if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = leftChildObject[0]; }

            else if (dir.y > diag && dir.z > diag) { if (gameManager.plusYplusZDimension[i - 1] == null) gameManager.plusYplusZDimension[i - 1] = leftChildObject[0]; }
            else if (dir.y > diag && dir.z < -diag) { if (gameManager.plusYminusZDimension[i - 1] == null) gameManager.plusYminusZDimension[i - 1] = leftChildObject[0]; }
            else if (dir.y < -diag && dir.z > diag) { if (gameManager.minusYplusZDimension[i - 1] == null) gameManager.minusYplusZDimension[i - 1] = leftChildObject[0]; }
            else if (dir.y < -diag && dir.z < -diag) { if (gameManager.minusYminusZDimension[i - 1] == null) gameManager.minusYminusZDimension[i - 1] = leftChildObject[0]; }
        }

        // 3. Check if Local ZX Plane is the one aligned
        else if (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, globalNormalZ)) > 0.98f)
        {
            Debug.Log("left Aligned Plane: Local ZX (Axes: Right & Forward) with XY");

            if (dir.z < -align) { if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = leftChildObject[0]; }
            else if (dir.z > align) { if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = leftChildObject[0]; }
            else if (dir.x > align) { if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = leftChildObject[0]; }
            else if (dir.x < -align) { if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = leftChildObject[0]; }

            else if (dir.x < -diag && dir.z < -diag) { if (gameManager.minusXminusZDimension[i - 1] == null) gameManager.minusXminusZDimension[i - 1] = leftChildObject[0]; }
            else if (dir.x < -diag && dir.z > diag) { if (gameManager.minusXplusZDimension[i - 1] == null) gameManager.minusXplusZDimension[i - 1] = leftChildObject[0]; }
            else if (dir.x > diag && dir.z < -diag) { if (gameManager.plusXminusZDimension[i - 1] == null) gameManager.plusXminusZDimension[i - 1] = leftChildObject[0]; }
            else if (dir.x > diag && dir.z > diag) { if (gameManager.plusXplusZDimension[i - 1] == null) gameManager.plusXplusZDimension[i - 1] = leftChildObject[0]; }
        }
    }

    void rightflagRadius(int i)
    {
        float align = 0.9f;
        float diag = 0.5f;

        float angleYZ = Mathf.Atan2(transform.right.y, transform.right.x) * Mathf.Rad2Deg;
        float angleZX = Mathf.Atan2(transform.up.y, transform.up.x) * Mathf.Rad2Deg;
        float angleXY = Mathf.Atan2(transform.forward.y, transform.forward.x) * Mathf.Rad2Deg;
        float tolerance = 1.0f;

        // ... (TILTED LOGIC REMAINS SAME AS YOU PROVIDED, OR COPY FROM LEFT AND CHANGE +45/-45 CHECKS IF NEEDED) ...
        // Note: I am keeping your specific Tilted Logic structure intact as requested, 
        // assuming your angle checks are correct for the "Right" child.

        // ... [PASTE YOUR TILTED IF/ELSE BLOCKS HERE] ...
        // For brevity, I am jumping to the Aligned Logic where the fix is needed.

        if ((Mathf.Abs(Vector3.Dot(transform.forward, Vector3.forward)) > 0.98f) &&
        (Mathf.Abs(Mathf.Abs(angleYZ) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleYZ) - 135f) <= tolerance) &&
        (Mathf.Abs(Mathf.Abs(angleZX) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleZX) - 135f) <= tolerance))
        {
            // ... (Keep existing tilted logic) ...
            if (Mathf.Abs(angleYZ - (-45f)) <= tolerance || Mathf.Abs(angleYZ - 135f) <= tolerance) { /*...*/ }
            else if (Mathf.Abs(angleZX - (-45f)) <= tolerance || Mathf.Abs(angleZX - 135f) <= tolerance) { /*...*/ }
        }
        else if ((Mathf.Abs(Vector3.Dot(transform.right, Vector3.forward)) > 0.98f) &&
        (Mathf.Abs(Mathf.Abs(angleXY) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleXY) - 135f) <= tolerance) &&
        (Mathf.Abs(Mathf.Abs(angleZX) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleZX) - 135f) <= tolerance))
        {
            // ... (Keep existing tilted logic) ...
            if (Mathf.Abs(angleXY - (-45f)) <= tolerance || Mathf.Abs(angleXY - 135f) <= tolerance) { /*...*/ }
            else if (Mathf.Abs(angleZX - (-45f)) <= tolerance || Mathf.Abs(angleZX - 135f) <= tolerance) { /*...*/ }
        }
        else if ((Mathf.Abs(Vector3.Dot(transform.up, Vector3.forward)) > 0.98f) &&
            (Mathf.Abs(Mathf.Abs(angleYZ) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleYZ) - 135f) <= tolerance) &&
            (Mathf.Abs(Mathf.Abs(angleXY) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleXY) - 135f) <= tolerance))
        {
            // ... (Keep existing tilted logic) ...
            if (Mathf.Abs(angleYZ - (-45f)) <= tolerance || Mathf.Abs(angleYZ - 135f) <= tolerance) { /*...*/ }
            else if (Mathf.Abs(angleXY - (-45f)) <= tolerance || Mathf.Abs(angleXY - 135f) <= tolerance) { /*...*/ }
        }


        // ==========================================================================================
        // FIXED ALIGNED LOGIC
        // ==========================================================================================

        // Calculate World Direction of the block
        Vector3 dir = rightChildObject[0].transform.position.normalized;

        // 1. Check if Local XY Plane is the one aligned
        if (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, globalNormalZ)) > 0.98f)
        {
            Debug.Log("right Aligned Plane: Local XY (Axes: Right & Up) with XY");

            if (dir.x > align) { if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = rightChildObject[0]; }
            else if (dir.x < -align) { if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = rightChildObject[0]; }
            else if (dir.y > align) { if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = rightChildObject[0]; }
            else if (dir.y < -align) { if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = rightChildObject[0]; }

            else if (dir.x < -diag && dir.y > diag) { if (gameManager.minusXplusYDimension[i - 1] == null) gameManager.minusXplusYDimension[i - 1] = rightChildObject[0]; }
            else if (dir.x > diag && dir.y > diag) { if (gameManager.plusXplusYDimension[i - 1] == null) gameManager.plusXplusYDimension[i - 1] = rightChildObject[0]; }
            else if (dir.x < -diag && dir.y < -diag) { if (gameManager.minusXminusYDimension[i - 1] == null) gameManager.minusXminusYDimension[i - 1] = rightChildObject[0]; }
            else if (dir.x > diag && dir.y < -diag) { if (gameManager.plusXminusYDimension[i - 1] == null) gameManager.plusXminusYDimension[i - 1] = rightChildObject[0]; }
        }

        // 2. Check if Local YZ Plane is the one aligned
        else if (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, globalNormalZ)) > 0.98f)
        {
            Debug.Log("right Aligned Plane: Local YZ (Axes: Up & Forward) with XY");

            if (dir.z < -align) { if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = rightChildObject[0]; }
            else if (dir.z > align) { if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = rightChildObject[0]; }
            else if (dir.y > align) { if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = rightChildObject[0]; }
            else if (dir.y < -align) { if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = rightChildObject[0]; }

            else if (dir.y > diag && dir.z > diag) { if (gameManager.plusYplusZDimension[i - 1] == null) gameManager.plusYplusZDimension[i - 1] = rightChildObject[0]; }
            else if (dir.y > diag && dir.z < -diag) { if (gameManager.plusYminusZDimension[i - 1] == null) gameManager.plusYminusZDimension[i - 1] = rightChildObject[0]; }
            else if (dir.y < -diag && dir.z > diag) { if (gameManager.minusYplusZDimension[i - 1] == null) gameManager.minusYplusZDimension[i - 1] = rightChildObject[0]; }
            else if (dir.y < -diag && dir.z < -diag) { if (gameManager.minusYminusZDimension[i - 1] == null) gameManager.minusYminusZDimension[i - 1] = rightChildObject[0]; }
        }

        // 3. Check if Local ZX Plane is the one aligned
        else if (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, globalNormalZ)) > 0.98f)
        {
            Debug.Log("right Aligned Plane: Local ZX (Axes: Right & Forward) with XY");

            if (dir.z < -align) { if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = rightChildObject[0]; }
            else if (dir.z > align) { if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = rightChildObject[0]; }
            else if (dir.x > align) { if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = rightChildObject[0]; }
            else if (dir.x < -align) { if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = rightChildObject[0]; }

            else if (dir.x < -diag && dir.z < -diag) { if (gameManager.minusXminusZDimension[i - 1] == null) gameManager.minusXminusZDimension[i - 1] = rightChildObject[0]; }
            else if (dir.x < -diag && dir.z > diag) { if (gameManager.minusXplusZDimension[i - 1] == null) gameManager.minusXplusZDimension[i - 1] = rightChildObject[0]; }
            else if (dir.x > diag && dir.z < -diag) { if (gameManager.plusXminusZDimension[i - 1] == null) gameManager.plusXminusZDimension[i - 1] = rightChildObject[0]; }
            else if (dir.x > diag && dir.z > diag) { if (gameManager.plusXplusZDimension[i - 1] == null) gameManager.plusXplusZDimension[i - 1] = rightChildObject[0]; }
        }
    }
    void verticalflagRadius(int i)
    {
        // 1. Get the actual World Direction of the Vertical Strip (Local Y Axis)
        Vector3 dir = gameManager.motherPlatform.transform.up;

        // Thresholds: 
        // > 0.9 means it is aligned with an axis (Straight)
        // > 0.5 means it has a component in that direction (Diagonal)
        float align = 0.9f;
        float diag = 0.5f;

        // ==========================================================================================
        // 1. TILTED LOGIC (Between 0.02 and 0.98)
        // ==========================================================================================

        // Condition: Local XY parallel to Global XY (Normal Z aligned)
        // The Strip (Local Y) is rotating in the XY Screen Plane
        if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, Vector3.forward)) > 0.98f) &&
             Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, Vector3.right)) > 0.02f &&
             Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, Vector3.right)) < 0.98f)
        {
            Debug.Log("local XY is flat against XY Plane, but rotated (Tilted)!");

            // DIAGONALS (Since we are tilted, we look for split values)
            if (dir.x > diag && dir.y > diag) // Top-Right (+X +Y)
            {
                if (gameManager.plusXplusYDimension[i - 1] == null && gameManager.plusXplusYDimension[i - 2] == null) { gameManager.plusXplusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusXplusYDimension[i - 2] = verticalChildObject[1]; }
            }
            else if (dir.x < -diag && dir.y > diag) // Top-Left (-X +Y)
            {
                if (gameManager.minusXplusYDimension[i - 1] == null && gameManager.minusXplusYDimension[i - 2] == null) { gameManager.minusXplusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusXplusYDimension[i - 2] = verticalChildObject[1]; }
            }
            else if (dir.x < -diag && dir.y < -diag) // Bottom-Left (-X -Y)
            {
                if (gameManager.minusXminusYDimension[i - 1] == null && gameManager.minusXminusYDimension[i - 2] == null) { gameManager.minusXminusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusXminusYDimension[i - 2] = verticalChildObject[1]; }
            }
            else if (dir.x > diag && dir.y < -diag) // Bottom-Right (+X -Y)
            {
                if (gameManager.plusXminusYDimension[i - 1] == null && gameManager.plusXminusYDimension[i - 2] == null) { gameManager.plusXminusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusXminusYDimension[i - 2] = verticalChildObject[1]; }
            }
        }

        // Condition: Local YZ is Parallel to Global XY (Locked on Z)
        // The Strip (Local Y) is rotating in the YZ Plane (Side Wall)
        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, Vector3.forward)) > 0.98f) &&
                 Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, Vector3.up)) > 0.02f &&
                 Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, Vector3.up)) < 0.98f)
        {
            Debug.Log("Local YZ Plane is flat against Global XY, but rotated (Tilted)!");

            // DIAGONALS (Y and Z components)
            if (dir.y > diag && dir.z > diag) // Up-Forward (+Y +Z)
            {
                if (gameManager.plusYplusZDimension[i - 1] == null && gameManager.plusYplusZDimension[i - 2] == null) { gameManager.plusYplusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusYplusZDimension[i - 2] = verticalChildObject[1]; }
            }
            else if (dir.y > diag && dir.z < -diag) // Up-Back (+Y -Z)
            {
                if (gameManager.plusYminusZDimension[i - 1] == null && gameManager.plusYminusZDimension[i - 2] == null) { gameManager.plusYminusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusYminusZDimension[i - 2] = verticalChildObject[1]; }
            }
            else if (dir.y < -diag && dir.z > diag) // Down-Forward (-Y +Z)
            {
                if (gameManager.minusYplusZDimension[i - 1] == null && gameManager.minusYplusZDimension[i - 2] == null) { gameManager.minusYplusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusYplusZDimension[i - 2] = verticalChildObject[1]; }
            }
            else if (dir.y < -diag && dir.z < -diag) // Down-Back (-Y -Z)
            {
                if (gameManager.minusYminusZDimension[i - 1] == null && gameManager.minusYminusZDimension[i - 2] == null) { gameManager.minusYminusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusYminusZDimension[i - 2] = verticalChildObject[1]; }
            }
        }

        // Condition: Local XZ Plane is Parallel to Global XY (Locked on Z)
        // The Strip (Local Y) is pointing at the camera (Global Z)
        // *NOTE: Since Normal is Y, and Strip is Y, the Strip IS the Normal. It must be Z-aligned.*
        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, Vector3.forward)) > 0.98f) &&
                 Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, Vector3.right)) > 0.02f &&
                 Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, Vector3.right)) < 0.98f)
        {
            Debug.Log("Local XZ Plane is flat against Global XY, but rotated (Tilted)!");
            // If the Strip (Up) is the normal to the screen, it can only be +Z or -Z.
            // It cannot be tilted 45 degrees in X or Y if it is locked to the screen normal.

            if (dir.z > align) // Forward
            {
                if (gameManager.plusZDimension[i - 1] == null && gameManager.plusZDimension[i - 2] == null) { gameManager.plusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusZDimension[i - 2] = verticalChildObject[1]; }
            }
            else if (dir.z < -align) // Back
            {
                if (gameManager.minusZDimension[i - 1] == null && gameManager.minusZDimension[i - 2] == null) { gameManager.minusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusZDimension[i - 2] = verticalChildObject[1]; }
            }
        }


        // ==========================================================================================
        // 2. ALIGNED LOGIC (Perfect 90 Degree Rotations)
        // ==========================================================================================

        // XY Parallel to Global XY, ZX Parallel to Global YZ
        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, globalNormalZ)) > 0.99f) &&
                 (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, globalNormalX)) > 0.98f))
        {
            Debug.Log("XY aligned");

            // Use Direction Logic to pick the ONE correct axis
            if (dir.y > align) // Pointing UP
            {
                if (gameManager.plusYDimension[i - 1] == null && gameManager.plusYDimension[i - 2] == null) { gameManager.plusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusYDimension[i - 2] = verticalChildObject[1]; }
            }
            else if (dir.y < -align) // Pointing DOWN
            {
                if (gameManager.minusYDimension[i - 1] == null && gameManager.minusYDimension[i - 2] == null) { gameManager.minusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusYDimension[i - 2] = verticalChildObject[1]; }
            }
            else if (dir.x > align) // Pointing RIGHT
            {
                if (gameManager.plusXDimension[i - 1] == null && gameManager.plusXDimension[i - 2] == null) { gameManager.plusXDimension[i - 1] = verticalChildObject[0]; gameManager.plusXDimension[i - 2] = verticalChildObject[1]; }
            }
            else if (dir.x < -align) // Pointing LEFT
            {
                if (gameManager.minusXDimension[i - 1] == null && gameManager.minusXDimension[i - 2] == null) { gameManager.minusXDimension[i - 1] = verticalChildObject[0]; gameManager.minusXDimension[i - 2] = verticalChildObject[1]; }
            }
        }

        // YZ Parallel to Global XY, XY Parallel to Global YZ
        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, globalNormalZ)) > 0.98f) &&
                 (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, globalNormalX)) > 0.98f))
        {
            Debug.Log("YZ aligned");

            // Check Y and Z axes
            if (dir.y > align) // UP
            {
                if (gameManager.plusYDimension[i - 1] == null && gameManager.plusYDimension[i - 2] == null) { gameManager.plusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusYDimension[i - 2] = verticalChildObject[1]; }
            }
            else if (dir.y < -align) // DOWN
            {
                if (gameManager.minusYDimension[i - 1] == null && gameManager.minusYDimension[i - 2] == null) { gameManager.minusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusYDimension[i - 2] = verticalChildObject[1]; }
            }
            else if (dir.z > align) // FORWARD
            {
                if (gameManager.plusZDimension[i - 1] == null && gameManager.plusZDimension[i - 2] == null) { gameManager.plusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusZDimension[i - 2] = verticalChildObject[1]; }
            }
            else if (dir.z < -align) // BACK
            {
                if (gameManager.minusZDimension[i - 1] == null && gameManager.minusZDimension[i - 2] == null) { gameManager.minusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusZDimension[i - 2] = verticalChildObject[1]; }
            }
        }

        // XZ Parallel to Global XY (Locked on Z), XY Parallel to Global YZ
        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, globalNormalZ)) > 0.98f) &&
                 (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, globalNormalX)) > 0.98f))
        {
            Debug.Log("XZ aligned");

            // Since the Strip (Up) is aligned with Global Z (Forward/Back)
            if (dir.z > align) // FORWARD
            {
                if (gameManager.plusZDimension[i - 1] == null && gameManager.plusZDimension[i - 2] == null) { gameManager.plusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusZDimension[i - 2] = verticalChildObject[1]; }
            }
            else if (dir.z < -align) // BACK
            {
                if (gameManager.minusZDimension[i - 1] == null && gameManager.minusZDimension[i - 2] == null) { gameManager.minusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusZDimension[i - 2] = verticalChildObject[1]; }
            }

            // Note: We do NOT check X here, because if the strip is along Z, it cannot be along X.
        }
    }


}
