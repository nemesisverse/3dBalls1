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


    // ... existing variables ...

    // ADD THIS LINE:
    public bool isLockedBySlider = false;

    // ... existing lists ...

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



    // Add this new helper method
// Simplified version - just check if parenting would collide
private bool WouldParentingCauseCollision(GameObject childToParent)
{
    // Store original parent
    Transform originalParent = childToParent.transform.parent;
    
    // Temporarily parent
    childToParent.transform.SetParent(gameManager.motherPlatform.transform, true);
    Physics.SyncTransforms();
    
    // Check collision
    bool collision = IsRotationColliding();
    
    // Restore original parent
    childToParent.transform.SetParent(originalParent, true);
    Physics.SyncTransforms();
    
    return collision;
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
            while (isLockedBySlider) yield return null;
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
                        
                        // MODIFIED: Check collision before parenting, but DON'T exit if collision
                        if (!WouldParentingCauseCollision(leftChildObject[0]))
                        {
                            leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            enabled = false;
                            yield break;
                        }
                        else
                        {
                            Debug.Log("Prevented left diagonal parenting due to collision - continuing movement");
                            stop = -1;
                            stopperID = 0;
                            // Continue the loop - don't yield break
                        }
                    }
                    else { stop = -1; stopperID = 0; }
                }
                else
                {
                    while (stop != -1 && stopperID != 1)
                    {
                        while (isLockedBySlider) yield return null;
                        if (!enabled)
                        {
                            leftflagRadius(i);
                            
                            // MODIFIED: Same logic
                            if (!WouldParentingCauseCollision(leftChildObject[0]))
                            {
                                leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                yield break;
                            }
                            else
                            {
                                Debug.Log("Prevented left diagonal parenting due to collision - continuing movement");
                                stop = -1;
                                stopperID = 0;
                                break; // Exit the inner while loop to continue main loop
                            }
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
                    
                    // MODIFIED: At the end, try to parent, if fails just end
                    if (!WouldParentingCauseCollision(leftChildObject[0]))
                    {
                        leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                    }
                    else
                    {
                        Debug.Log("Reached end but can't parent left diagonal due to collision - stays as T-piece child");
                    }
                    
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
            while (isLockedBySlider) yield return null;
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
                        
                        // MODIFIED: Check collision before parenting, but DON'T exit if collision
                        if (!WouldParentingCauseCollision(rightChildObject[0]))
                        {
                            rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            enabled = false;
                            yield break;
                        }
                        else
                        {
                            Debug.Log("Prevented right diagonal parenting due to collision - continuing movement");
                            stop = -1;
                            stopperID = 0;
                            // Continue the loop - don't yield break
                        }
                    }
                    else { stop = -1; stopperID = 0; }
                }
                else
                {
                    while (stop != -1 && stopperID != 2)
                    {
                        while (isLockedBySlider) yield return null;
                        if (!enabled)
                        {
                            rightflagRadius(i);
                            
                            // MODIFIED: Same logic
                            if (!WouldParentingCauseCollision(rightChildObject[0]))
                            {
                                rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                yield break;
                            }
                            else
                            {
                                Debug.Log("Prevented right diagonal parenting due to collision - continuing movement");
                                stop = -1;
                                stopperID = 0;
                                break; // Exit the inner while loop to continue main loop
                            }
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
                    
                    // MODIFIED: At the end, try to parent, if fails just end
                    if (!WouldParentingCauseCollision(rightChildObject[0]))
                    {
                        rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                    }
                    else
                    {
                        Debug.Log("Reached end but can't parent right diagonal due to collision - stays as T-piece child");
                    }
                    
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
            while (isLockedBySlider) yield return null;
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
                        
                        // MODIFIED: Check collision before parenting (both children), but DON'T exit if collision
                        bool collision0 = WouldParentingCauseCollision(verticalChildObject[0]);
                        bool collision1 = WouldParentingCauseCollision(verticalChildObject[1]);
                        
                        // Try to parent both blocks if no collision
                        bool anyParented = false;
                        
                        if (!collision0)
                        {
                            verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            anyParented = true;
                        }
                        else
                        {
                            Debug.Log("Prevented vertical[0] parenting due to collision");
                        }
                        
                        if (!collision1)
                        {
                            verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                            anyParented = true;
                        }
                        else
                        {
                            Debug.Log("Prevented vertical[1] parenting due to collision");
                        }
                        
                        // If both blocks collided, continue movement instead of stopping
                        if (collision0 && collision1)
                        {
                            Debug.Log("Both vertical blocks prevented from parenting - continuing movement");
                            stop = -1;
                            stopperID = 0;
                            // Continue the loop
                        }
                        else
                        {
                            // At least one block was parented successfully
                            // gameManager.checkRingToDestroy();
                            // gameManager.checkXZRingToDestroy();
                            // gameManager.checkYZRingToDestroy();
                            gameManager.CheckAllRingsSimplified();
                            enabled = false;
                            yield break;
                        }
                    }
                    else { stop = -1; stopperID = 0; }
                }
                else
                {
                    while (stop != -1 && stopperID != 3)
                    {
                        while (isLockedBySlider) yield return null;
                        if (!enabled)
                        {
                            verticalflagRadius(i);
                            
                            // MODIFIED: Check collision before parenting (both children)
                            bool collision0 = WouldParentingCauseCollision(verticalChildObject[0]);
                            bool collision1 = WouldParentingCauseCollision(verticalChildObject[1]);
                            
                            if (!collision0)
                            {
                                verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            }
                            else
                            {
                                Debug.Log("Prevented vertical[0] parenting due to collision");
                            }
                            
                            if (!collision1)
                            {
                                verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                            }
                            else
                            {
                                Debug.Log("Prevented vertical[1] parenting due to collision");
                            }
                            
                            // If both blocks collided, continue movement
                            if (collision0 && collision1)
                            {
                                Debug.Log("Both vertical blocks prevented from parenting - continuing movement");
                                stop = -1;
                                stopperID = 0;
                                break; // Exit the inner while loop to continue main loop
                            }
                            else
                            {
                                // gameManager.checkRingToDestroy();
                                // gameManager.checkXZRingToDestroy();
                                // gameManager.checkYZRingToDestroy();
                                gameManager.CheckAllRingsSimplified();
                                yield break;
                            }
                        }
                        yield return null;
                    }
                }
            }

            verticalChildObject[0].transform.position = verticalCoordinates[i];
            verticalChildObject[1].transform.position = verticalCoordinates[i - 1];

            try { if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, verticalCoordinates[i + 1])) { if (stop == -1) { stop = i; stopperID = 3; } } }
            catch (System.ArgumentOutOfRangeException)
            {
                if (verticalChildObject[0].transform.position == verticalCoordinates[verticalCoordinates.Count - 1] &&
                    verticalChildObject[1].transform.position == verticalCoordinates[verticalCoordinates.Count - 2])
                {
                    verticalflagRadius(i + 1);
                    
                    // MODIFIED: At the end, try to parent both, if fails just end
                    bool collision0 = WouldParentingCauseCollision(verticalChildObject[0]);
                    bool collision1 = WouldParentingCauseCollision(verticalChildObject[1]);
                    
                    if (!collision0)
                    {
                        verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                    }
                    else
                    {
                        Debug.Log("Reached end but can't parent vertical[0] due to collision - stays as T-piece child");
                    }
                    
                    if (!collision1)
                    {
                        verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                    }
                    else
                    {
                        Debug.Log("Reached end but can't parent vertical[1] due to collision - stays as T-piece child");
                    }
                    
                    // gameManager.checkRingToDestroy();
                    // gameManager.checkXZRingToDestroy();
                    // gameManager.checkYZRingToDestroy();
                    gameManager.CheckAllRingsSimplified();
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

    // // --- LOGIC TO PREVENT SLIDER MOVEMENT ---
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

void leftflagRadius(int i)
{
    var membership = gameManager.GetRingMembership(leftChildObject[0].transform.position);
    
    if (membership.radiusIndex < 0 || membership.radiusIndex >= 13)
    {
        Debug.LogWarning($"Left block at invalid radius: {membership.radiusIndex}");
        return;
    }
    
    int idx = membership.radiusIndex;
    
    // Assign to appropriate dimension list based on actual position
    if (membership.isInXYRing)
    {
        // XY Ring - try cardinal directions first, then diagonals
        if (gameManager.plusXDimension[idx] == null) gameManager.plusXDimension[idx] = leftChildObject[0];
        else if (gameManager.minusXDimension[idx] == null) gameManager.minusXDimension[idx] = leftChildObject[0];
        else if (gameManager.plusYDimension[idx] == null) gameManager.plusYDimension[idx] = leftChildObject[0];
        else if (gameManager.minusYDimension[idx] == null) gameManager.minusYDimension[idx] = leftChildObject[0];
        else if (gameManager.minusXplusYDimension[idx] == null) gameManager.minusXplusYDimension[idx] = leftChildObject[0];
        else if (gameManager.plusXplusYDimension[idx] == null) gameManager.plusXplusYDimension[idx] = leftChildObject[0];
        else if (gameManager.minusXminusYDimension[idx] == null) gameManager.minusXminusYDimension[idx] = leftChildObject[0];
        else if (gameManager.plusXminusYDimension[idx] == null) gameManager.plusXminusYDimension[idx] = leftChildObject[0];
    }
    
    if (membership.isInYZRing)
    {
        // YZ Ring
        if (gameManager.plusYDimension[idx] == null) gameManager.plusYDimension[idx] = leftChildObject[0];
        else if (gameManager.minusYDimension[idx] == null) gameManager.minusYDimension[idx] = leftChildObject[0];
        else if (gameManager.plusZDimension[idx] == null) gameManager.plusZDimension[idx] = leftChildObject[0];
        else if (gameManager.minusZDimension[idx] == null) gameManager.minusZDimension[idx] = leftChildObject[0];
        else if (gameManager.plusYplusZDimension[idx] == null) gameManager.plusYplusZDimension[idx] = leftChildObject[0];
        else if (gameManager.plusYminusZDimension[idx] == null) gameManager.plusYminusZDimension[idx] = leftChildObject[0];
        else if (gameManager.minusYplusZDimension[idx] == null) gameManager.minusYplusZDimension[idx] = leftChildObject[0];
        else if (gameManager.minusYminusZDimension[idx] == null) gameManager.minusYminusZDimension[idx] = leftChildObject[0];
    }
    
    if (membership.isInXZRing)
    {
        // XZ Ring
        if (gameManager.plusXDimension[idx] == null) gameManager.plusXDimension[idx] = leftChildObject[0];
        else if (gameManager.minusXDimension[idx] == null) gameManager.minusXDimension[idx] = leftChildObject[0];
        else if (gameManager.plusZDimension[idx] == null) gameManager.plusZDimension[idx] = leftChildObject[0];
        else if (gameManager.minusZDimension[idx] == null) gameManager.minusZDimension[idx] = leftChildObject[0];
        else if (gameManager.minusXminusZDimension[idx] == null) gameManager.minusXminusZDimension[idx] = leftChildObject[0];
        else if (gameManager.minusXplusZDimension[idx] == null) gameManager.minusXplusZDimension[idx] = leftChildObject[0];
        else if (gameManager.plusXminusZDimension[idx] == null) gameManager.plusXminusZDimension[idx] = leftChildObject[0];
        else if (gameManager.plusXplusZDimension[idx] == null) gameManager.plusXplusZDimension[idx] = leftChildObject[0];
    }
}

void rightflagRadius(int i)
{
    var membership = gameManager.GetRingMembership(rightChildObject[0].transform.position);
    
    if (membership.radiusIndex < 0 || membership.radiusIndex >= 13)
    {
        Debug.LogWarning($"Right block at invalid radius: {membership.radiusIndex}");
        return;
    }
    
    int idx = membership.radiusIndex;
    
    if (membership.isInXYRing)
    {
        if (gameManager.plusXDimension[idx] == null) gameManager.plusXDimension[idx] = rightChildObject[0];
        else if (gameManager.minusXDimension[idx] == null) gameManager.minusXDimension[idx] = rightChildObject[0];
        else if (gameManager.plusYDimension[idx] == null) gameManager.plusYDimension[idx] = rightChildObject[0];
        else if (gameManager.minusYDimension[idx] == null) gameManager.minusYDimension[idx] = rightChildObject[0];
        else if (gameManager.minusXplusYDimension[idx] == null) gameManager.minusXplusYDimension[idx] = rightChildObject[0];
        else if (gameManager.plusXplusYDimension[idx] == null) gameManager.plusXplusYDimension[idx] = rightChildObject[0];
        else if (gameManager.minusXminusYDimension[idx] == null) gameManager.minusXminusYDimension[idx] = rightChildObject[0];
        else if (gameManager.plusXminusYDimension[idx] == null) gameManager.plusXminusYDimension[idx] = rightChildObject[0];
    }
    
    if (membership.isInYZRing)
    {
        if (gameManager.plusYDimension[idx] == null) gameManager.plusYDimension[idx] = rightChildObject[0];
        else if (gameManager.minusYDimension[idx] == null) gameManager.minusYDimension[idx] = rightChildObject[0];
        else if (gameManager.plusZDimension[idx] == null) gameManager.plusZDimension[idx] = rightChildObject[0];
        else if (gameManager.minusZDimension[idx] == null) gameManager.minusZDimension[idx] = rightChildObject[0];
        else if (gameManager.plusYplusZDimension[idx] == null) gameManager.plusYplusZDimension[idx] = rightChildObject[0];
        else if (gameManager.plusYminusZDimension[idx] == null) gameManager.plusYminusZDimension[idx] = rightChildObject[0];
        else if (gameManager.minusYplusZDimension[idx] == null) gameManager.minusYplusZDimension[idx] = rightChildObject[0];
        else if (gameManager.minusYminusZDimension[idx] == null) gameManager.minusYminusZDimension[idx] = rightChildObject[0];
    }
    
    if (membership.isInXZRing)
    {
        if (gameManager.plusXDimension[idx] == null) gameManager.plusXDimension[idx] = rightChildObject[0];
        else if (gameManager.minusXDimension[idx] == null) gameManager.minusXDimension[idx] = rightChildObject[0];
        else if (gameManager.plusZDimension[idx] == null) gameManager.plusZDimension[idx] = rightChildObject[0];
        else if (gameManager.minusZDimension[idx] == null) gameManager.minusZDimension[idx] = rightChildObject[0];
        else if (gameManager.minusXminusZDimension[idx] == null) gameManager.minusXminusZDimension[idx] = rightChildObject[0];
        else if (gameManager.minusXplusZDimension[idx] == null) gameManager.minusXplusZDimension[idx] = rightChildObject[0];
        else if (gameManager.plusXminusZDimension[idx] == null) gameManager.plusXminusZDimension[idx] = rightChildObject[0];
        else if (gameManager.plusXplusZDimension[idx] == null) gameManager.plusXplusZDimension[idx] = rightChildObject[0];
    }
}

void verticalflagRadius(int i)
{
    var membership0 = gameManager.GetRingMembership(verticalChildObject[0].transform.position);
    var membership1 = gameManager.GetRingMembership(verticalChildObject[1].transform.position);
    
    // Process first vertical block
    if (membership0.radiusIndex >= 0 && membership0.radiusIndex < 13)
    {
        int idx0 = membership0.radiusIndex;
        
        if (membership0.isInXYRing)
        {
            if (gameManager.plusXDimension[idx0] == null) gameManager.plusXDimension[idx0] = verticalChildObject[0];
            else if (gameManager.plusYDimension[idx0] == null) gameManager.plusYDimension[idx0] = verticalChildObject[0];
            else if (gameManager.minusXDimension[idx0] == null) gameManager.minusXDimension[idx0] = verticalChildObject[0];
            else if (gameManager.minusYDimension[idx0] == null) gameManager.minusYDimension[idx0] = verticalChildObject[0];
            else if (gameManager.minusXplusYDimension[idx0] == null) gameManager.minusXplusYDimension[idx0] = verticalChildObject[0];
            else if (gameManager.plusXplusYDimension[idx0] == null) gameManager.plusXplusYDimension[idx0] = verticalChildObject[0];
            else if (gameManager.minusXminusYDimension[idx0] == null) gameManager.minusXminusYDimension[idx0] = verticalChildObject[0];
            else if (gameManager.plusXminusYDimension[idx0] == null) gameManager.plusXminusYDimension[idx0] = verticalChildObject[0];
        }
        
        if (membership0.isInYZRing)
        {
            if (gameManager.plusYDimension[idx0] == null) gameManager.plusYDimension[idx0] = verticalChildObject[0];
            else if (gameManager.plusZDimension[idx0] == null) gameManager.plusZDimension[idx0] = verticalChildObject[0];
            else if (gameManager.minusYDimension[idx0] == null) gameManager.minusYDimension[idx0] = verticalChildObject[0];
            else if (gameManager.minusZDimension[idx0] == null) gameManager.minusZDimension[idx0] = verticalChildObject[0];
            else if (gameManager.plusYplusZDimension[idx0] == null) gameManager.plusYplusZDimension[idx0] = verticalChildObject[0];
            else if (gameManager.plusYminusZDimension[idx0] == null) gameManager.plusYminusZDimension[idx0] = verticalChildObject[0];
            else if (gameManager.minusYplusZDimension[idx0] == null) gameManager.minusYplusZDimension[idx0] = verticalChildObject[0];
            else if (gameManager.minusYminusZDimension[idx0] == null) gameManager.minusYminusZDimension[idx0] = verticalChildObject[0];
        }
        
        if (membership0.isInXZRing)
        {
            if (gameManager.plusXDimension[idx0] == null) gameManager.plusXDimension[idx0] = verticalChildObject[0];
            else if (gameManager.plusZDimension[idx0] == null) gameManager.plusZDimension[idx0] = verticalChildObject[0];
            else if (gameManager.minusXDimension[idx0] == null) gameManager.minusXDimension[idx0] = verticalChildObject[0];
            else if (gameManager.minusZDimension[idx0] == null) gameManager.minusZDimension[idx0] = verticalChildObject[0];
            else if (gameManager.minusXminusZDimension[idx0] == null) gameManager.minusXminusZDimension[idx0] = verticalChildObject[0];
            else if (gameManager.minusXplusZDimension[idx0] == null) gameManager.minusXplusZDimension[idx0] = verticalChildObject[0];
            else if (gameManager.plusXminusZDimension[idx0] == null) gameManager.plusXminusZDimension[idx0] = verticalChildObject[0];
            else if (gameManager.plusXplusZDimension[idx0] == null) gameManager.plusXplusZDimension[idx0] = verticalChildObject[0];
        }
    }
    
    // Process second vertical block (same logic but with verticalChildObject[1])
    if (membership1.radiusIndex >= 0 && membership1.radiusIndex < 13)
    {
        int idx1 = membership1.radiusIndex;
        
        if (membership1.isInXYRing)
        {
            if (gameManager.plusXDimension[idx1] == null) gameManager.plusXDimension[idx1] = verticalChildObject[1];
            else if (gameManager.plusYDimension[idx1] == null) gameManager.plusYDimension[idx1] = verticalChildObject[1];
            else if (gameManager.minusXDimension[idx1] == null) gameManager.minusXDimension[idx1] = verticalChildObject[1];
            else if (gameManager.minusYDimension[idx1] == null) gameManager.minusYDimension[idx1] = verticalChildObject[1];
            else if (gameManager.minusXplusYDimension[idx1] == null) gameManager.minusXplusYDimension[idx1] = verticalChildObject[1];
            else if (gameManager.plusXplusYDimension[idx1] == null) gameManager.plusXplusYDimension[idx1] = verticalChildObject[1];
            else if (gameManager.minusXminusYDimension[idx1] == null) gameManager.minusXminusYDimension[idx1] = verticalChildObject[1];
            else if (gameManager.plusXminusYDimension[idx1] == null) gameManager.plusXminusYDimension[idx1] = verticalChildObject[1];
        }
        
        if (membership1.isInYZRing)
        {
            if (gameManager.plusYDimension[idx1] == null) gameManager.plusYDimension[idx1] = verticalChildObject[1];
            else if (gameManager.plusZDimension[idx1] == null) gameManager.plusZDimension[idx1] = verticalChildObject[1];
            else if (gameManager.minusYDimension[idx1] == null) gameManager.minusYDimension[idx1] = verticalChildObject[1];
            else if (gameManager.minusZDimension[idx1] == null) gameManager.minusZDimension[idx1] = verticalChildObject[1];
            else if (gameManager.plusYplusZDimension[idx1] == null) gameManager.plusYplusZDimension[idx1] = verticalChildObject[1];
            else if (gameManager.plusYminusZDimension[idx1] == null) gameManager.plusYminusZDimension[idx1] = verticalChildObject[1];
            else if (gameManager.minusYplusZDimension[idx1] == null) gameManager.minusYplusZDimension[idx1] = verticalChildObject[1];
            else if (gameManager.minusYminusZDimension[idx1] == null) gameManager.minusYminusZDimension[idx1] = verticalChildObject[1];
        }
        
        if (membership1.isInXZRing)
        {
            if (gameManager.plusXDimension[idx1] == null) gameManager.plusXDimension[idx1] = verticalChildObject[1];
            else if (gameManager.plusZDimension[idx1] == null) gameManager.plusZDimension[idx1] = verticalChildObject[1];
            else if (gameManager.minusXDimension[idx1] == null) gameManager.minusXDimension[idx1] = verticalChildObject[1];
            else if (gameManager.minusZDimension[idx1] == null) gameManager.minusZDimension[idx1] = verticalChildObject[1];
            else if (gameManager.minusXminusZDimension[idx1] == null) gameManager.minusXminusZDimension[idx1] = verticalChildObject[1];
            else if (gameManager.minusXplusZDimension[idx1] == null) gameManager.minusXplusZDimension[idx1] = verticalChildObject[1];
            else if (gameManager.plusXminusZDimension[idx1] == null) gameManager.plusXminusZDimension[idx1] = verticalChildObject[1];
            else if (gameManager.plusXplusZDimension[idx1] == null) gameManager.plusXplusZDimension[idx1] = verticalChildObject[1];
        }
    }
}
}



    

