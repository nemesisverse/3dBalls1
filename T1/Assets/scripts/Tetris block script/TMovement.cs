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

    private List<List<GameObject>> allDimensions;
    Vector3 globalNormalX = Vector3.right;
    Vector3 globalNormalZ = Vector3.forward;

    // ADD THESE: For slider locking and state management
    public bool isLockedBySlider = false;
    private Quaternion lastValidRotation;
    private Vector3 lastValidPosition;

    void Awake()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (swipeInput == null) swipeInput = FindFirstObjectByType<SwipeInput>();
        if (sliderController == null) sliderController = FindFirstObjectByType<SliderPedestalController1>();

        for (float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f) leftDiagonalCoordinates.Add(new Vector3(-v, v, 0f));
        for (float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f) rightDiagonalCoordinates.Add(new Vector3(v, v, 0f));
        for (float v = 14.5f; v >= 2.5f; v -= 1f) verticalCoordinates.Add(new Vector3(0f, v, 0f));
    }

    void Start()
    {
        countChildren();
        CheckChildrenWorldX();

        // Save initial valid state
        lastValidRotation = transform.rotation;
        lastValidPosition = transform.position;

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

    // PUBLIC METHODS FOR SAVING/REVERTING STATE
    public void SaveCurrentState()
    {
        lastValidRotation = transform.rotation;
        lastValidPosition = transform.position;
    }

    public void RevertToLastValidState()
    {
        transform.rotation = lastValidRotation;
        transform.position = lastValidPosition;
        Physics.SyncTransforms();
        Debug.Log("<color=yellow>Reverted T-piece to last valid state due to collision</color>");
    }

    // COLLISION DETECTION - Checks against motherPlatform children
    public bool IsRotationColliding()
    {
        List<GameObject> activeMovingChildren = new List<GameObject>();
        if (leftChildObject != null) activeMovingChildren.AddRange(leftChildObject);
        if (rightChildObject != null) activeMovingChildren.AddRange(rightChildObject);
        if (verticalChildObject != null) activeMovingChildren.AddRange(verticalChildObject);

        if (activeMovingChildren.Count == 0) return false;

        // Check collision with ALL children of motherPlatform (actual placed blocks)
        foreach (Transform placedChild in gameManager.motherPlatform.transform)
        {
            if (placedChild == null) continue;

            foreach (var movingBlock in activeMovingChildren)
            {
                if (movingBlock == null) continue;
                if (placedChild.gameObject == movingBlock) continue; // Ignore self

                if (ArePositionsOverlapping(placedChild.position, movingBlock.transform.position))
                {
                    Debug.Log($"<color=red>COLLISION!</color> {movingBlock.name} at {movingBlock.transform.position} overlaps with {placedChild.name} at {placedChild.position}");
                    return true;
                }
            }
        }
        
        return false;
    }

    private bool WouldParentingCauseCollision(GameObject childToParent)
    {
        Transform originalParent = childToParent.transform.parent;
        
        childToParent.transform.SetParent(gameManager.motherPlatform.transform, true);
        Physics.SyncTransforms();
        
        bool collision = IsRotationColliding();
        
        childToParent.transform.SetParent(originalParent, true);
        Physics.SyncTransforms();
        
        return collision;
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

    // MOVEMENT COROUTINES
    int stop = -1;
    int stopperID = 0;

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
                                    break;
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
                                    break;
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
                            
                            if (collision0 && collision1)
                            {
                                Debug.Log("Both vertical blocks prevented from parenting - continuing movement");
                                stop = -1;
                                stopperID = 0;
                            }
                            else
                            {
                                gameManager.checkRingToDestroy();
                                gameManager.checkXZRingToDestroy();
                                gameManager.checkYZRingToDestroy();
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
                                
                                if (collision0 && collision1)
                                {
                                    Debug.Log("Both vertical blocks prevented from parenting - continuing movement");
                                    stop = -1;
                                    stopperID = 0;
                                    break;
                                }
                                else
                                {
                                    gameManager.checkRingToDestroy();
                                    gameManager.checkXZRingToDestroy();
                                    gameManager.checkYZRingToDestroy();
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

    // --- RADIUS FLAG FUNCTIONS ---
    void leftflagRadius(int i)
    {
        float angleYZ = Mathf.Atan2(transform.right.y, transform.right.x) * Mathf.Rad2Deg;
        float angleZX = Mathf.Atan2(transform.up.y, transform.up.x) * Mathf.Rad2Deg;
        float angleXY = Mathf.Atan2(transform.forward.y, transform.forward.x) * Mathf.Rad2Deg; // Angle of Local XY Plane Normal (Z)
        float tolerance = 1.0f;

        // 2. THE SINGLE IF CONDITION
        // Check: (Local XY Parallel to Global XY) && (YZ is Diagonal) && (ZX is Diagonal)
        if ((Mathf.Abs(Vector3.Dot(transform.forward, Vector3.forward)) > 0.99f) &&
        (Mathf.Abs(Mathf.Abs(angleYZ) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleYZ) - 135f) <= tolerance) &&
        (Mathf.Abs(Mathf.Abs(angleZX) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleZX) - 135f) <= tolerance))
        {
            //Debug.Log("SUCCESS: Object is flat on XY, and both YZ & ZX planes are at 45° diagonals!");
            // 3. Log the specific details for YZ Plane (Right Axis)
            if (Mathf.Abs(angleYZ - 45f) <= tolerance || Mathf.Abs(angleYZ - (-135f)) <= tolerance)
            {
                Debug.Log("local XY is parrelel to global");
                Debug.Log("loca YZ was making +45 with  global YZ so adding it in left diognal ring ");
                if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusYplusZDimension[i - 1] == null) gameManager.plusYplusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusYminusZDimension[i - 1] == null) gameManager.plusYminusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusYplusZDimension[i - 1] == null) gameManager.minusYplusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusYminusZDimension[i - 1] == null) gameManager.minusYminusZDimension[i - 1] = leftChildObject[0];

            }
            //ZX making +45 degree angle
            else if (Mathf.Abs(angleZX - 45f) <= tolerance || Mathf.Abs(angleZX - (-135f)) <= tolerance)
            {
                Debug.Log("local ZX making  +ve angle with global YZ ");
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

        // Check A: Local YZ Normal (Right) is Parallel to Global XY Normal (Forward) -> Object is "side-on" to camera
        // Check B: Local XY Plane (Forward) is Diagonal
        // Check C: Local ZX Plane (Up) is Diagonal
        else if ((Mathf.Abs(Vector3.Dot(transform.right, Vector3.forward)) > 0.99f) &&
        (Mathf.Abs(Mathf.Abs(angleXY) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleXY) - 135f) <= tolerance) &&
        (Mathf.Abs(Mathf.Abs(angleZX) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleZX) - 135f) <= tolerance))
        {
            Debug.Log("SUCCESS: Local YZ is flat on XY, and both XY & ZX planes are at 45° diagonals!");
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
            //ZX tilted left side
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


        // Check A: Local ZX Normal (Up) is Parallel to Global XY Normal (Forward) -> Object is "top-down" to camera
        // Check B: Local YZ Plane (Right) is Diagonal
        // Check C: Local XY Plane (Forward) is Diagonal
        else if ((Mathf.Abs(Vector3.Dot(transform.up, Vector3.forward)) > 0.99f) &&
            (Mathf.Abs(Mathf.Abs(angleYZ) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleYZ) - 135f) <= tolerance) &&
            (Mathf.Abs(Mathf.Abs(angleXY) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleXY) - 135f) <= tolerance))
        {
            Debug.Log("SUCCESS: Local ZX is flat on XY, and both YZ & XY planes are at 45° diagonals!");
            //YZ 
            if (Mathf.Abs(angleYZ - 45f) <= tolerance || Mathf.Abs(angleYZ - (-135f)) <= tolerance)
            {
                Debug.Log("loca YZ was making +45 with  global YZ so adding it in left diognal ring ");

                if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusYplusZDimension[i - 1] == null) gameManager.plusYplusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.plusYminusZDimension[i - 1] == null) gameManager.plusYminusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusYplusZDimension[i - 1] == null) gameManager.minusYplusZDimension[i - 1] = leftChildObject[0];
                else if (gameManager.minusYminusZDimension[i - 1] == null) gameManager.minusYminusZDimension[i - 1] = leftChildObject[0];

            }

            //XY plane
            // [Local XY] is +45° on left side
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
        // 1. Check if Local XY Plane is the one aligned
        // The Normal of Local XY is Local Forward (Z)
        else if (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, globalNormalZ)) > 0.99f)
        {
            Debug.Log("left Aligned Plane: Local XY (Axes: Right & Up) with XY");
            if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = leftChildObject[0];
            else if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = leftChildObject[0];
            else if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = leftChildObject[0];
            else if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = leftChildObject[0];
            else if (gameManager.minusXplusYDimension[i - 1] == null) gameManager.minusXplusYDimension[i - 1] = leftChildObject[0];
            else if (gameManager.plusXplusYDimension[i - 1] == null) gameManager.plusXplusYDimension[i - 1] = leftChildObject[0];
            else if (gameManager.minusXminusYDimension[i - 1] == null) gameManager.minusXminusYDimension[i - 1] = leftChildObject[0];
            else if (gameManager.plusXminusYDimension[i - 1] == null) gameManager.plusXminusYDimension[i - 1] = leftChildObject[0];

        }

        // 2. Check if Local YZ Plane is the one aligned with XY
        // The Normal of Local YZ is Local Right (X)
        else if (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, globalNormalZ)) > 0.99f)
        {
            Debug.Log("left Aligned Plane: Local YZ (Axes: Up & Forward) with XY");
            if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = leftChildObject[0];
            else if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = leftChildObject[0];
            else if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = leftChildObject[0];
            else if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = leftChildObject[0];
            else if (gameManager.plusYplusZDimension[i - 1] == null) gameManager.plusYplusZDimension[i - 1] = leftChildObject[0];
            else if (gameManager.plusYminusZDimension[i - 1] == null) gameManager.plusYminusZDimension[i - 1] = leftChildObject[0];
            else if (gameManager.minusYplusZDimension[i - 1] == null) gameManager.minusYplusZDimension[i - 1] = leftChildObject[0];
            else if (gameManager.minusYminusZDimension[i - 1] == null) gameManager.minusYminusZDimension[i - 1] = leftChildObject[0];
        }


        // 3. Check if Local ZX Plane is the one aligned
        // The Normal of Local ZX is Local Up (Y)
        else if (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, globalNormalZ)) > 0.99f)
        {
            Debug.Log("left Aligned Plane: Local ZX (Axes: Right & Forward) with XY");
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

    void rightflagRadius(int i)
    {
        float angleYZ = Mathf.Atan2(transform.right.y, transform.right.x) * Mathf.Rad2Deg;
        float angleZX = Mathf.Atan2(transform.up.y, transform.up.x) * Mathf.Rad2Deg;
        float angleXY = Mathf.Atan2(transform.forward.y, transform.forward.x) * Mathf.Rad2Deg; // Angle of Local XY Plane Normal (Z)
        float tolerance = 1.0f;

        // 2. THE SINGLE IF CONDITION
        // Check: (Local XY Parallel to Global XY) && (YZ is Diagonal) && (ZX is Diagonal)
        if ((Mathf.Abs(Vector3.Dot(transform.forward, Vector3.forward)) > 0.99f) &&
        (Mathf.Abs(Mathf.Abs(angleYZ) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleYZ) - 135f) <= tolerance) &&
        (Mathf.Abs(Mathf.Abs(angleZX) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleZX) - 135f) <= tolerance))
        {
            Debug.Log("SUCCESS: Object is flat on XY, and both YZ & ZX planes are at 45° diagonals!");
            // 3. Log the specific details for YZ Plane (Right Axis)
            if (Mathf.Abs(angleYZ - (-45f)) <= tolerance || Mathf.Abs(angleYZ - 135f) <= tolerance)
            {
                Debug.Log("loca YZ was making +45 with  global YZ so adding it in left diognal ring ");
                if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusYplusZDimension[i - 1] == null) gameManager.plusYplusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusYminusZDimension[i - 1] == null) gameManager.plusYminusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusYplusZDimension[i - 1] == null) gameManager.minusYplusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusYminusZDimension[i - 1] == null) gameManager.minusYminusZDimension[i - 1] = rightChildObject[0];

            }
            //ZX making +45 degree angle
            else if (Mathf.Abs(angleZX - (-45f)) <= tolerance || Mathf.Abs(angleZX - 135f) <= tolerance)
            {
                Debug.Log("local ZX making  +ve angle with global YZ ");
                if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusXminusZDimension[i - 1] == null) gameManager.minusXminusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusXplusZDimension[i - 1] == null) gameManager.minusXplusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusXminusZDimension[i - 1] == null) gameManager.plusXminusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusXplusZDimension[i - 1] == null) gameManager.plusXplusZDimension[i - 1] = rightChildObject[0];


            }
        }

        // Check A: Local YZ Normal (Right) is Parallel to Global XY Normal (Forward) -> Object is "side-on" to camera
        // Check B: Local XY Plane (Forward) is Diagonal
        // Check C: Local ZX Plane (Up) is Diagonal
        else if ((Mathf.Abs(Vector3.Dot(transform.right, Vector3.forward)) > 0.99f) &&
        (Mathf.Abs(Mathf.Abs(angleXY) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleXY) - 135f) <= tolerance) &&
        (Mathf.Abs(Mathf.Abs(angleZX) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleZX) - 135f) <= tolerance))
        {
            Debug.Log("SUCCESS: Local YZ is flat on XY, and both XY & ZX planes arightt 45° diagonals!");
            // [Local XY] is +45° on left side
            if (Mathf.Abs(angleXY - (-45f)) <= tolerance || Mathf.Abs(angleXY - 135f) <= tolerance)
            {
                if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusXplusYDimension[i - 1] == null) gameManager.minusXplusYDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusXplusYDimension[i - 1] == null) gameManager.plusXplusYDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusXminusYDimension[i - 1] == null) gameManager.minusXminusYDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusXminusYDimension[i - 1] == null) gameManager.plusXminusYDimension[i - 1] = rightChildObject[0];

            }
            //ZX tilted left side
            else if (Mathf.Abs(angleZX - (-45f)) <= tolerance || Mathf.Abs(angleZX - 135f) <= tolerance)
            {
                if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusXminusZDimension[i - 1] == null) gameManager.minusXminusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusXplusZDimension[i - 1] == null) gameManager.minusXplusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusXminusZDimension[i - 1] == null) gameManager.plusXminusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusXplusZDimension[i - 1] == null) gameManager.plusXplusZDimension[i - 1] = rightChildObject[0];
            }





        }


        // Check A: Local ZX Normal (Up) is Parallel to Global XY Normal (Forward) -> Object is "top-down" to camera
        // Check B: Local YZ Plane (Right) is Diagonal
        // Check C: Local XY Plane (Forward) is Diagonal
        else if ((Mathf.Abs(Vector3.Dot(transform.up, Vector3.forward)) > 0.99f) &&
            (Mathf.Abs(Mathf.Abs(angleYZ) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleYZ) - 135f) <= tolerance) &&
            (Mathf.Abs(Mathf.Abs(angleXY) - 45f) <= tolerance || Mathf.Abs(Mathf.Abs(angleXY) - 135f) <= tolerance))
        {
            Debug.Log("SUCCESS: Local ZX is flat on XY, and both YZ & XY planes are at 45° diagonals!");
            //YZ 
            if (Mathf.Abs(angleYZ - (-45f)) <= tolerance || Mathf.Abs(angleYZ - 135f) <= tolerance)
            {
                Debug.Log("loca YZ was making +45 with  global YZ so adding it in left diognal ring ");

                if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusYplusZDimension[i - 1] == null) gameManager.plusYplusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusYminusZDimension[i - 1] == null) gameManager.plusYminusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusYplusZDimension[i - 1] == null) gameManager.minusYplusZDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusYminusZDimension[i - 1] == null) gameManager.minusYminusZDimension[i - 1] = rightChildObject[0];

            }

            //XY plane
            // [Local XY] is +45° on left side
            else if (Mathf.Abs(angleXY - (-45f)) <= tolerance || Mathf.Abs(angleXY - 135f) <= tolerance)
            {
                if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusXplusYDimension[i - 1] == null) gameManager.minusXplusYDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusXplusYDimension[i - 1] == null) gameManager.plusXplusYDimension[i - 1] = rightChildObject[0];
                else if (gameManager.minusXminusYDimension[i - 1] == null) gameManager.minusXminusYDimension[i - 1] = rightChildObject[0];
                else if (gameManager.plusXminusYDimension[i - 1] == null) gameManager.plusXminusYDimension[i - 1] = rightChildObject[0];
            }


        }
        // 1. Check if Local XY Plane is the one aligned
        // The Normal of Local XY is Local Forward (Z)
        if (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, globalNormalZ)) > 0.99f)
        {

            Debug.Log("right Aligned Plane: Local XY (Axes: Right & Up) with XY");
            if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = rightChildObject[0];
            else if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = rightChildObject[0];
            else if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = rightChildObject[0];
            else if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = rightChildObject[0];
            else if (gameManager.minusXplusYDimension[i - 1] == null) gameManager.minusXplusYDimension[i - 1] = rightChildObject[0];
            else if (gameManager.plusXplusYDimension[i - 1] == null) gameManager.plusXplusYDimension[i - 1] = rightChildObject[0];
            else if (gameManager.minusXminusYDimension[i - 1] == null) gameManager.minusXminusYDimension[i - 1] = rightChildObject[0];
            else if (gameManager.plusXminusYDimension[i - 1] == null) gameManager.plusXminusYDimension[i - 1] = rightChildObject[0];
        }
        // 2. Check if Local YZ Plane is the one aligned
        // The Normal of Local YZ is Local Right (X)
        else if (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, globalNormalZ)) > 0.99f)
        {
            Debug.Log("right Aligned Plane: Local YZ (Axes: Up & Forward) with XY");
            if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = rightChildObject[0];
            else if (gameManager.minusYDimension[i - 1] == null) gameManager.minusYDimension[i - 1] = rightChildObject[0];
            else if (gameManager.plusYDimension[i - 1] == null) gameManager.plusYDimension[i - 1] = rightChildObject[0];
            else if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = rightChildObject[0];
            else if (gameManager.plusYplusZDimension[i - 1] == null) gameManager.plusYplusZDimension[i - 1] = rightChildObject[0];
            else if (gameManager.plusYminusZDimension[i - 1] == null) gameManager.plusYminusZDimension[i - 1] = rightChildObject[0];
            else if (gameManager.minusYplusZDimension[i - 1] == null) gameManager.minusYplusZDimension[i - 1] = rightChildObject[0];
            else if (gameManager.minusYminusZDimension[i - 1] == null) gameManager.minusYminusZDimension[i - 1] = rightChildObject[0];

        }

        // 3. Check if Local ZX Plane is the one aligned
        // The Normal of Local ZX is Local Up (Y)
        else if (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, globalNormalZ)) > 0.99f)
        {
            Debug.Log("right Aligned Plane: Local ZX (Axes: Right & Forward) with XY");
            if (gameManager.minusZDimension[i - 1] == null) gameManager.minusZDimension[i - 1] = rightChildObject[0];
            else if (gameManager.plusXDimension[i - 1] == null) gameManager.plusXDimension[i - 1] = rightChildObject[0];
            else if (gameManager.minusXDimension[i - 1] == null) gameManager.minusXDimension[i - 1] = rightChildObject[0];
            else if (gameManager.plusZDimension[i - 1] == null) gameManager.plusZDimension[i - 1] = rightChildObject[0];
            else if (gameManager.minusXminusZDimension[i - 1] == null) gameManager.minusXminusZDimension[i - 1] = rightChildObject[0];
            else if (gameManager.minusXplusZDimension[i - 1] == null) gameManager.minusXplusZDimension[i - 1] = rightChildObject[0];
            else if (gameManager.plusXminusZDimension[i - 1] == null) gameManager.plusXminusZDimension[i - 1] = rightChildObject[0];
            else if (gameManager.plusXplusZDimension[i - 1] == null) gameManager.plusXplusZDimension[i - 1] = rightChildObject[0];
        }

    }

    void verticalflagRadius(int i)
    {
        //local XY parallel against global XY
        if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, Vector3.forward)) > 0.99f) &&
     Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, Vector3.right)) > 0.01f && Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, Vector3.right)) < 0.99f)
        {
            // Executes if the object is tilted (e.g., 45°, 30°, etc.) relative to the Global YZ wall
            Debug.Log("local XY is flat against XY Plane, but rotated (Tilted)!");
            // Block only get added in XY plane
            if (gameManager.plusXDimension[i - 1] == null && gameManager.plusXDimension[i - 2] == null) { gameManager.plusXDimension[i - 1] = verticalChildObject[0]; gameManager.plusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusYDimension[i - 1] == null && gameManager.plusYDimension[i - 2] == null) { gameManager.plusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i - 1] == null && gameManager.minusXDimension[i - 2] == null) { gameManager.minusXDimension[i - 1] = verticalChildObject[0]; gameManager.minusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i - 1] == null && gameManager.minusYDimension[i - 2] == null) { gameManager.minusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusYDimension[i - 2] = verticalChildObject[1]; }

            else if (gameManager.minusXplusYDimension[i - 1] == null && gameManager.minusXplusYDimension[i - 2] == null) { gameManager.minusXplusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusXplusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXplusYDimension[i - 1] == null && gameManager.plusXplusYDimension[i - 2] == null) { gameManager.plusXplusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusXplusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXminusYDimension[i - 1] == null && gameManager.minusXminusYDimension[i - 2] == null) { gameManager.minusXminusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusXminusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXminusYDimension[i - 1] == null && gameManager.plusXminusYDimension[i - 2] == null) { gameManager.plusXminusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusXminusYDimension[i - 2] = verticalChildObject[1]; }

        }

        /////////
        // Condition: Local YZ is Parallel to Global XY (Locked on Z) AND the other axes are Tilted
        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, Vector3.forward)) > 0.99f) &&
             Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, Vector3.up)) > 0.01f && Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, Vector3.up)) < 0.99f)
        {
            Debug.Log("Local YZ Plane is flat against Global XY, but rotated (Tilted)!");
            //Block add in YZ

            if (gameManager.plusYDimension[i - 1] == null && gameManager.plusYDimension[i - 2] == null) { gameManager.plusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i - 1] == null && gameManager.plusZDimension[i - 2] == null) { gameManager.plusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i - 1] == null && gameManager.minusYDimension[i - 2] == null) { gameManager.minusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i - 1] == null && gameManager.minusZDimension[i - 2] == null) { gameManager.minusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusZDimension[i - 2] = verticalChildObject[1]; }

            else if (gameManager.plusYplusZDimension[i - 1] == null && gameManager.plusYplusZDimension[i - 2] == null) { gameManager.plusYplusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusYplusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusYminusZDimension[i - 1] == null && gameManager.plusYminusZDimension[i - 2] == null) { gameManager.plusYminusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusYminusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYplusZDimension[i - 1] == null && gameManager.minusYplusZDimension[i - 2] == null) { gameManager.minusYplusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusYplusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYminusZDimension[i - 1] == null && gameManager.minusYminusZDimension[i - 2] == null) { gameManager.minusYminusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusYminusZDimension[i - 2] = verticalChildObject[1]; }

        }
        /////////
        // Condition: Local XZ Plane is Parallel to Global XY (Locked on Z) AND the other axes are Tilted
        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, Vector3.forward)) > 0.99f) &&
             Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, Vector3.right)) > 0.01f && Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, Vector3.right)) < 0.99f)
        {
            Debug.Log("Local XZ Plane is flat against Global XY, but rotated (Tilted)!");

            //Block need to add in ZX
            if (gameManager.plusXDimension[i - 1] == null && gameManager.plusXDimension[i - 2] == null) { gameManager.plusXDimension[i - 1] = verticalChildObject[0]; gameManager.plusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i - 1] == null && gameManager.plusZDimension[i - 2] == null) { gameManager.plusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i - 1] == null && gameManager.minusXDimension[i - 2] == null) { gameManager.minusXDimension[i - 1] = verticalChildObject[0]; gameManager.minusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i - 1] == null && gameManager.minusZDimension[i - 2] == null) { gameManager.minusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusZDimension[i - 2] = verticalChildObject[1]; }

            else if (gameManager.minusXminusZDimension[i - 1] == null && gameManager.minusXminusZDimension[i - 2] == null) { gameManager.minusXminusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusXminusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXplusZDimension[i - 1] == null && gameManager.minusXplusZDimension[i - 2] == null) { gameManager.minusXplusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusXplusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXminusZDimension[i - 1] == null && gameManager.plusXminusZDimension[i - 2] == null) { gameManager.plusXminusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusXminusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXplusZDimension[i - 1] == null && gameManager.plusXplusZDimension[i - 2] == null) { gameManager.plusXplusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusXplusZDimension[i - 2] = verticalChildObject[1]; }
        }

        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, globalNormalZ)) > 99f) && (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, globalNormalX)) > 0.99f))
        {
            // add block in local XY and local zx plane ring
            Debug.Log("XY is paraller to global XY and ZX is parallel to global YZ");
            //adding in XY ring 
            if (gameManager.plusXDimension[i - 1] == null && gameManager.plusXDimension[i - 2] == null) { gameManager.plusXDimension[i - 1] = verticalChildObject[0]; gameManager.plusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusYDimension[i - 1] == null && gameManager.plusYDimension[i - 2] == null) { gameManager.plusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i - 1] == null && gameManager.minusXDimension[i - 2] == null) { gameManager.minusXDimension[i - 1] = verticalChildObject[0]; gameManager.minusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i - 1] == null && gameManager.minusYDimension[i - 2] == null) { gameManager.minusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusYDimension[i - 2] = verticalChildObject[1]; }

            else if (gameManager.minusXplusYDimension[i - 1] == null && gameManager.minusXplusYDimension[i - 2] == null) { gameManager.minusXplusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusXplusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXplusYDimension[i - 1] == null && gameManager.plusXplusYDimension[i - 2] == null) { gameManager.plusXplusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusXplusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXminusYDimension[i - 1] == null && gameManager.minusXminusYDimension[i - 2] == null) { gameManager.minusXminusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusXminusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXminusYDimension[i - 1] == null && gameManager.plusXminusYDimension[i - 2] == null) { gameManager.plusXminusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusXminusYDimension[i - 2] = verticalChildObject[1]; }


            // adding in ZX plane
            if (gameManager.plusXDimension[i - 1] == null && gameManager.plusXDimension[i - 2] == null) { gameManager.plusXDimension[i - 1] = verticalChildObject[0]; gameManager.plusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i - 1] == null && gameManager.plusZDimension[i - 2] == null) { gameManager.plusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i - 1] == null && gameManager.minusXDimension[i - 2] == null) { gameManager.minusXDimension[i - 1] = verticalChildObject[0]; gameManager.minusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i - 1] == null && gameManager.minusZDimension[i - 2] == null) { gameManager.minusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusZDimension[i - 2] = verticalChildObject[1]; }

            else if (gameManager.minusXminusZDimension[i - 1] == null && gameManager.minusXminusZDimension[i - 2] == null) { gameManager.minusXminusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusXminusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXplusZDimension[i - 1] == null && gameManager.minusXplusZDimension[i - 2] == null) { gameManager.minusXplusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusXplusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXminusZDimension[i - 1] == null && gameManager.plusXminusZDimension[i - 2] == null) { gameManager.plusXminusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusXminusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXplusZDimension[i - 1] == null && gameManager.plusXplusZDimension[i - 2] == null) { gameManager.plusXplusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusXplusZDimension[i - 2] = verticalChildObject[1]; }

        }
        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, globalNormalZ)) > 0.99f) && (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, globalNormalX)) > 0.99f))
        {
            // add block in local XY and local yz  plane ring 
            Debug.Log("XY is paraller to global XY and YZ is parallel to global YZ");
            //Block in XY plane
            if (gameManager.plusXDimension[i - 1] == null && gameManager.plusXDimension[i - 2] == null) { gameManager.plusXDimension[i - 1] = verticalChildObject[0]; gameManager.plusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusYDimension[i - 1] == null && gameManager.plusYDimension[i - 2] == null) { gameManager.plusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i - 1] == null && gameManager.minusXDimension[i - 2] == null) { gameManager.minusXDimension[i - 1] = verticalChildObject[0]; gameManager.minusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i - 1] == null && gameManager.minusYDimension[i - 2] == null) { gameManager.minusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusYDimension[i - 2] = verticalChildObject[1]; }

            else if (gameManager.minusXplusYDimension[i - 1] == null && gameManager.minusXplusYDimension[i - 2] == null) { gameManager.minusXplusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusXplusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXplusYDimension[i - 1] == null && gameManager.plusXplusYDimension[i - 2] == null) { gameManager.plusXplusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusXplusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXminusYDimension[i - 1] == null && gameManager.minusXminusYDimension[i - 2] == null) { gameManager.minusXminusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusXminusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXminusYDimension[i - 1] == null && gameManager.plusXminusYDimension[i - 2] == null) { gameManager.plusXminusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusXminusYDimension[i - 2] = verticalChildObject[1]; }

            // Block in YZ local plane
            if (gameManager.plusYDimension[i - 1] == null && gameManager.plusYDimension[i - 2] == null) { gameManager.plusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i - 1] == null && gameManager.plusZDimension[i - 2] == null) { gameManager.plusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i - 1] == null && gameManager.minusYDimension[i - 2] == null) { gameManager.minusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i - 1] == null && gameManager.minusZDimension[i - 2] == null) { gameManager.minusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusZDimension[i - 2] = verticalChildObject[1]; }

            else if (gameManager.plusYplusZDimension[i - 1] == null && gameManager.plusYplusZDimension[i - 2] == null) { gameManager.plusYplusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusYplusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusYminusZDimension[i - 1] == null && gameManager.plusYminusZDimension[i - 2] == null) { gameManager.plusYminusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusYminusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYplusZDimension[i - 1] == null && gameManager.minusYplusZDimension[i - 2] == null) { gameManager.minusYplusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusYplusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYminusZDimension[i - 1] == null && gameManager.minusYminusZDimension[i - 2] == null) { gameManager.minusYminusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusYminusZDimension[i - 2] = verticalChildObject[1]; }

        }
        //


        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, globalNormalZ)) > 0.99f) && (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, globalNormalX)) > 0.99f))
        {
            // add block in local YZ and local XY plane ring 
            Debug.Log("YZ is paraller to global XY and XY is parallel to global YZ");
            //Block in XY
            if (gameManager.plusXDimension[i - 1] == null && gameManager.plusXDimension[i - 2] == null) { gameManager.plusXDimension[i - 1] = verticalChildObject[0]; gameManager.plusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusYDimension[i - 1] == null && gameManager.plusYDimension[i - 2] == null) { gameManager.plusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i - 1] == null && gameManager.minusXDimension[i - 2] == null) { gameManager.minusXDimension[i - 1] = verticalChildObject[0]; gameManager.minusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i - 1] == null && gameManager.minusYDimension[i - 2] == null) { gameManager.minusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXplusYDimension[i - 1] == null && gameManager.minusXplusYDimension[i - 2] == null) { gameManager.minusXplusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusXplusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXplusYDimension[i - 1] == null && gameManager.plusXplusYDimension[i - 2] == null) { gameManager.plusXplusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusXplusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXminusYDimension[i - 1] == null && gameManager.minusXminusYDimension[i - 2] == null) { gameManager.minusXminusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusXminusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXminusYDimension[i - 1] == null && gameManager.plusXminusYDimension[i - 2] == null) { gameManager.plusXminusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusXminusYDimension[i - 2] = verticalChildObject[1]; }

            //Block in YZ local
            if (gameManager.plusYDimension[i - 1] == null && gameManager.plusYDimension[i - 2] == null) { gameManager.plusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i - 1] == null && gameManager.plusZDimension[i - 2] == null) { gameManager.plusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i - 1] == null && gameManager.minusYDimension[i - 2] == null) { gameManager.minusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i - 1] == null && gameManager.minusZDimension[i - 2] == null) { gameManager.minusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusYplusZDimension[i - 1] == null && gameManager.plusYplusZDimension[i - 2] == null) { gameManager.plusYplusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusYplusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusYminusZDimension[i - 1] == null && gameManager.plusYminusZDimension[i - 2] == null) { gameManager.plusYminusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusYminusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYplusZDimension[i - 1] == null && gameManager.minusYplusZDimension[i - 2] == null) { gameManager.minusYplusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusYplusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYminusZDimension[i - 1] == null && gameManager.minusYminusZDimension[i - 2] == null) { gameManager.minusYminusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusYminusZDimension[i - 2] = verticalChildObject[1]; }


        }


        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, globalNormalZ)) > 0.99f) && (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, globalNormalX)) > 0.99f))
        {
            // add block in local YZ and local XZ plane ring

            //Block in YZ
            if (gameManager.plusYDimension[i - 1] == null && gameManager.plusYDimension[i - 2] == null) { gameManager.plusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i - 1] == null && gameManager.plusZDimension[i - 2] == null) { gameManager.plusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i - 1] == null && gameManager.minusYDimension[i - 2] == null) { gameManager.minusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i - 1] == null && gameManager.minusZDimension[i - 2] == null) { gameManager.minusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusZDimension[i - 2] = verticalChildObject[1]; }

            else if (gameManager.plusYplusZDimension[i - 1] == null && gameManager.plusYplusZDimension[i - 2] == null) { gameManager.plusYplusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusYplusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusYminusZDimension[i - 1] == null && gameManager.plusYminusZDimension[i - 2] == null) { gameManager.plusYminusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusYminusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYplusZDimension[i - 1] == null && gameManager.minusYplusZDimension[i - 2] == null) { gameManager.minusYplusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusYplusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYminusZDimension[i - 1] == null && gameManager.minusYminusZDimension[i - 2] == null) { gameManager.minusYminusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusYminusZDimension[i - 2] = verticalChildObject[1]; }

            //Block in XZ ring
            if (gameManager.plusXDimension[i - 1] == null && gameManager.plusXDimension[i - 2] == null) { gameManager.plusXDimension[i - 1] = verticalChildObject[0]; gameManager.plusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i - 1] == null && gameManager.plusZDimension[i - 2] == null) { gameManager.plusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i - 1] == null && gameManager.minusXDimension[i - 2] == null) { gameManager.minusXDimension[i - 1] = verticalChildObject[0]; gameManager.minusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i - 1] == null && gameManager.minusZDimension[i - 2] == null) { gameManager.minusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusZDimension[i - 2] = verticalChildObject[1]; }

            else if (gameManager.minusXminusZDimension[i - 1] == null && gameManager.minusXminusZDimension[i - 2] == null) { gameManager.minusXminusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusXminusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXplusZDimension[i - 1] == null && gameManager.minusXplusZDimension[i - 2] == null) { gameManager.minusXplusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusXplusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXminusZDimension[i - 1] == null && gameManager.plusXminusZDimension[i - 2] == null) { gameManager.plusXminusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusXminusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXplusZDimension[i - 1] == null && gameManager.plusXplusZDimension[i - 2] == null) { gameManager.plusXplusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusXplusZDimension[i - 2] = verticalChildObject[1]; }
        }
        //


        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, globalNormalZ)) > 0.99f) && (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, globalNormalX)) > 0.99f))
        {
            // add block in  local XZ plane and local XY 
            Debug.Log("ZX is paraller to global XY and XY is parallel to global YZ");

            //Block in XY
            if (gameManager.plusXDimension[i - 1] == null && gameManager.plusXDimension[i - 2] == null) { gameManager.plusXDimension[i - 1] = verticalChildObject[0]; gameManager.plusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusYDimension[i - 1] == null && gameManager.plusYDimension[i - 2] == null) { gameManager.plusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i - 1] == null && gameManager.minusXDimension[i - 2] == null) { gameManager.minusXDimension[i - 1] = verticalChildObject[0]; gameManager.minusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i - 1] == null && gameManager.minusYDimension[i - 2] == null) { gameManager.minusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusYDimension[i - 2] = verticalChildObject[1]; }

            else if (gameManager.minusXplusYDimension[i - 1] == null && gameManager.minusXplusYDimension[i - 2] == null) { gameManager.minusXplusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusXplusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXplusYDimension[i - 1] == null && gameManager.plusXplusYDimension[i - 2] == null) { gameManager.plusXplusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusXplusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXminusYDimension[i - 1] == null && gameManager.minusXminusYDimension[i - 2] == null) { gameManager.minusXminusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusXminusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXminusYDimension[i - 1] == null && gameManager.plusXminusYDimension[i - 2] == null) { gameManager.plusXminusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusXminusYDimension[i - 2] = verticalChildObject[1]; }

            //Block in XZ plane 
            if (gameManager.plusXDimension[i - 1] == null && gameManager.plusXDimension[i - 2] == null) { gameManager.plusXDimension[i - 1] = verticalChildObject[0]; gameManager.plusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i - 1] == null && gameManager.plusZDimension[i - 2] == null) { gameManager.plusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i - 1] == null && gameManager.minusXDimension[i - 2] == null) { gameManager.minusXDimension[i - 1] = verticalChildObject[0]; gameManager.minusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i - 1] == null && gameManager.minusZDimension[i - 2] == null) { gameManager.minusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusZDimension[i - 2] = verticalChildObject[1]; }

            else if (gameManager.minusXminusZDimension[i - 1] == null && gameManager.minusXminusZDimension[i - 2] == null) { gameManager.minusXminusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusXminusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXplusZDimension[i - 1] == null && gameManager.minusXplusZDimension[i - 2] == null) { gameManager.minusXplusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusXplusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXminusZDimension[i - 1] == null && gameManager.plusXminusZDimension[i - 2] == null) { gameManager.plusXminusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusXminusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXplusZDimension[i - 1] == null && gameManager.plusXplusZDimension[i - 2] == null) { gameManager.plusXplusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusXplusZDimension[i - 2] = verticalChildObject[1]; }


        }
        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, globalNormalZ)) > 0.99f) && (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, globalNormalX)) > 0.99f))
        {
            // add block in local XZ and local YZ plane

            //Block in XZ plane
            if (gameManager.plusXDimension[i - 1] == null && gameManager.plusXDimension[i - 2] == null) { gameManager.plusXDimension[i - 1] = verticalChildObject[0]; gameManager.plusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i - 1] == null && gameManager.plusZDimension[i - 2] == null) { gameManager.plusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i - 1] == null && gameManager.minusXDimension[i - 2] == null) { gameManager.minusXDimension[i - 1] = verticalChildObject[0]; gameManager.minusXDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i - 1] == null && gameManager.minusZDimension[i - 2] == null) { gameManager.minusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusZDimension[i - 2] = verticalChildObject[1]; }

            else if (gameManager.minusXminusZDimension[i - 1] == null && gameManager.minusXminusZDimension[i - 2] == null) { gameManager.minusXminusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusXminusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusXplusZDimension[i - 1] == null && gameManager.minusXplusZDimension[i - 2] == null) { gameManager.minusXplusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusXplusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXminusZDimension[i - 1] == null && gameManager.plusXminusZDimension[i - 2] == null) { gameManager.plusXminusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusXminusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusXplusZDimension[i - 1] == null && gameManager.plusXplusZDimension[i - 2] == null) { gameManager.plusXplusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusXplusZDimension[i - 2] = verticalChildObject[1]; }

            //Block in YZ plane

            if (gameManager.plusYDimension[i - 1] == null && gameManager.plusYDimension[i - 2] == null) { gameManager.plusYDimension[i - 1] = verticalChildObject[0]; gameManager.plusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i - 1] == null && gameManager.plusZDimension[i - 2] == null) { gameManager.plusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i - 1] == null && gameManager.minusYDimension[i - 2] == null) { gameManager.minusYDimension[i - 1] = verticalChildObject[0]; gameManager.minusYDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i - 1] == null && gameManager.minusZDimension[i - 2] == null) { gameManager.minusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusZDimension[i - 2] = verticalChildObject[1]; }

            else if (gameManager.plusYplusZDimension[i - 1] == null && gameManager.plusYplusZDimension[i - 2] == null) { gameManager.plusYplusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusYplusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.plusYminusZDimension[i - 1] == null && gameManager.plusYminusZDimension[i - 2] == null) { gameManager.plusYminusZDimension[i - 1] = verticalChildObject[0]; gameManager.plusYminusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYplusZDimension[i - 1] == null && gameManager.minusYplusZDimension[i - 2] == null) { gameManager.minusYplusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusYplusZDimension[i - 2] = verticalChildObject[1]; }
            else if (gameManager.minusYminusZDimension[i - 1] == null && gameManager.minusYminusZDimension[i - 2] == null) { gameManager.minusYminusZDimension[i - 1] = verticalChildObject[0]; gameManager.minusYminusZDimension[i - 2] = verticalChildObject[1]; }
        }
        // else
        // {
        //     Debug.Log("vertical flag radius failed");
        // }



    }
}
