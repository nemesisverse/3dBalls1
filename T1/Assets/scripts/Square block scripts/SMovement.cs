using System.Collections;
using System.Collections.Generic;
using Unity.Android.Gradle;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class SMovement : MonoBehaviour
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
    // public SliderPedestalController1 sliderController;

    // Optimized list for collision checking
    private List<List<GameObject>> allDimensions;
    Vector3 globalNormalX = Vector3.right; //YZ plane ke liye
    Vector3 globalNormalZ = Vector3.forward; // XY plane ke liye

    int stop = -1;
    int stopperID = 0;

    

    void Awake()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (swipeInput == null) swipeInput = FindFirstObjectByType<SwipeInput>();
        //if (sliderController == null) sliderController = FindFirstObjectByType<SliderPedestalController1>();

        // Populate Coordinates
        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f) leftDiagonalCoordinates.Add(new Vector3(-v, v, 0f));
        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f) rightDiagonalCoordinates.Add(new Vector3(v, v, 0f));
        for (float v = 18.5f; v >= 2.5f; v -= 1f) verticalCoordinates.Add(new Vector3(0f, v, 0f));
    }
    void Start()
    {
        countChildren();
        CheckChildrenWorldX();
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

//     public void leftRotationStopper(int i)
// {
//     // The falling block is at leftDiagonalCoordinates[i]
//     // Check what position it would move TO if each swipe happened
    
//     Vector3 currentPos = leftDiagonalCoordinates[i];
//     Vector3 currentPosPrevElement = leftDiagonalCoordinates[i-1];
    
//     // Simulate swipe up rotation (Vector3.right, 90f)
//     Vector3 posAfterUp = RotatePoint(currentPos, Vector3.right, 90f);
//     Vector3 posAfterUp1 = RotatePoint(currentPosPrevElement, Vector3.right, 90f);
//     if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, posAfterUp) ||gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, posAfterUp1))
//         swipeInput.canSwipeUp = false;

//     // Simulate swipe down rotation (Vector3.right, -90f)
//     Vector3 posAfterDown = RotatePoint(currentPos, Vector3.right, -90f);
//     Vector3 posAfterDown1 = RotatePoint(currentPosPrevElement, Vector3.right, -90f);
//     if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, posAfterDown) || gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, posAfterDown1) )
//         swipeInput.canSwipeDown = false;

//     // Simulate swipe left rotation (Vector3.up, 90f)
//     Vector3 posAfterLeft = RotatePoint(currentPos, Vector3.up, 90f);
//     Vector3 posAfterLeft1 = RotatePoint(currentPos, Vector3.up, 90f);
//     if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, posAfterLeft) || gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, posAfterLeft1))
//         swipeInput.canSwipeLeft = false;

//     // Simulate swipe right rotation (Vector3.up, -90f)
//     Vector3 posAfterRight = RotatePoint(currentPos, Vector3.up, -90f);
//     Vector3 posAfterRight1 = RotatePoint(currentPos, Vector3.up, -90f);
//     if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, posAfterRight) || gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, posAfterRight1))
//         swipeInput.canSwipeRight = false;
// }

// public void rightRotationStopper(int i)
// {
//     Vector3 currentPos = rightDiagonalCoordinates[i];

//     Vector3 posAfterUp = RotatePoint(currentPos, Vector3.right, 90f);
//     if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, posAfterUp))
//         swipeInput.canSwipeUp = false;

//     Vector3 posAfterDown = RotatePoint(currentPos, Vector3.right, -90f);
//     if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, posAfterDown))
//         swipeInput.canSwipeDown = false;

//     Vector3 posAfterLeft = RotatePoint(currentPos, Vector3.up, 90f);
//     if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, posAfterLeft))
//         swipeInput.canSwipeLeft = false;

//     Vector3 posAfterRight = RotatePoint(currentPos, Vector3.up, -90f);
//     if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, posAfterRight))
//         swipeInput.canSwipeRight = false;
// }

// private Vector3 RotatePoint(Vector3 worldPoint, Vector3 axis, float degrees)
// {
//     Vector3 pivot = gameManager.motherPlatform.transform.position;
//     return Quaternion.AngleAxis(degrees, axis) * (worldPoint - pivot) + pivot;
// }

    public void leftRotationStopper(int i)
{
    // T1 Left Diagonal is a single block, moving down the left diagonal

    foreach (var direction in allDimensions)
    {
        // First, make sure the slot exists and isn't empty
        if (i >= 0 && i < direction.Count && direction[i] != null)
        {
            //Vector3 posPrevElement = direction[i-1].transform.position;
            Vector3 pos = direction[i].transform.position;
            //Vector3 posPrevElement = direction[i-1].transform.position;

            // If a block exists at slot 'i', where is it?

            // 1. Is there a block on the -X / +Z plane? (Blocks DOWN swipe)
            if (pos.x < -0.1f && pos.z > 0.1f) 
            {
                swipeInput.canSwipeDown = false;
            }

            // 2. Is there a block on the -X / -Z plane? (Blocks UP swipe)
            if ((pos.x < -0.1f && pos.z < -0.1f) )
            {
                swipeInput.canSwipeUp = false;
            }

            // 3. Is there a block on the +Y / +Z plane? (Blocks LEFT swipe)
            if (pos.y > 0.1f && pos.z > 0.1f) 
            {
                swipeInput.canSwipeLeft = false;
            }

            // 4. Is there a block on the +Y / -Z plane? (Blocks RIGHT swipe)
            if (pos.y > 0.1f && pos.z < -0.1f)
            {
                swipeInput.canSwipeRight = false;
            }
        }
    }
}

    public void verticalRotationStopper(int i)
    {
        foreach (var direction in allDimensions)
        {
            // Check slot i
            if (direction[i] != null &&
                Mathf.Abs(direction[i].transform.position.x) < 0.1f &&
                Mathf.Abs(direction[i].transform.position.y) < 0.1f)
            {
                if (direction[i].transform.position.z > 0f) swipeInput.canSwipeDown = false;
                if (direction[i].transform.position.z < 0f) swipeInput.canSwipeUp = false;
            }

            // Check slot i-1 (guard against index underflow)
            if (i - 1 >= 0 && direction[i - 1] != null &&
                Mathf.Abs(direction[i - 1].transform.position.x) < 0.1f &&
                Mathf.Abs(direction[i - 1].transform.position.y) < 0.1f)
            {
                if (direction[i - 1].transform.position.z > 0f) swipeInput.canSwipeDown = false;
                if (direction[i - 1].transform.position.z < 0f) swipeInput.canSwipeUp = false;
            }
        }
    }

    public void ResetSwipePermissions()
    {
        swipeInput.canSwipeRight = true;
        swipeInput.canSwipeLeft = true;
        swipeInput.canSwipeUp = true;
        swipeInput.canSwipeDown = true;
    }

    IEnumerator moveLeftDiognal(Transform child, int childCount)
    {
        if (leftChildObject == null || leftChildObject.Count == 0) yield break;
        if (childCount == 2)
        {
            for (int i = 2; i < leftDiagonalCoordinates.Count; i++)
            {
                ResetSwipePermissions();
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
                            leftflagRadius(i - 1);
                            leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            leftChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);

                            ResetSwipePermissions();

                            gameManager.checkRingToDestroy();
                            gameManager.checkYZRingToDestroy();
                            gameManager.checkXZRingToDestroy();
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
                                leftflagRadius(i - 1);
                                leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                leftChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);

                            ResetSwipePermissions();

                                gameManager.checkRingToDestroy();
                                gameManager.checkYZRingToDestroy();
                                gameManager.checkXZRingToDestroy();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                leftChildObject[0].transform.position = leftDiagonalCoordinates[i];
                leftChildObject[1].transform.position = leftDiagonalCoordinates[i - 1];
                leftRotationStopper(i); // check for swipe permissions at new position

                // --- YOUR LOGIC: Check & Lock Slider Directions ---

                // --------------------------------------------------

                try { if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[i + 1])) { if (stop == -1) { stop = i; stopperID = 1; } } }
                catch (System.ArgumentOutOfRangeException)
                {
                    if (leftChildObject[0].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 1] &&
                        leftChildObject[1].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 2])
                    {
                        leftflagRadius(i);
                        leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        leftChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                        ResetSwipePermissions();

                        gameManager.checkRingToDestroy();
                        gameManager.checkYZRingToDestroy();
                        gameManager.checkXZRingToDestroy();
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
                ResetSwipePermissions();
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
                            verticalflagRadius(i - 1);
                            verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);

                            ResetSwipePermissions();

                            gameManager.checkRingToDestroy();
                            gameManager.checkYZRingToDestroy();
                            gameManager.checkXZRingToDestroy();
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
                                verticalflagRadius(i - 1);
                                verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);

                                ResetSwipePermissions();

                                gameManager.checkRingToDestroy();
                                gameManager.checkYZRingToDestroy();
                                gameManager.checkXZRingToDestroy();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                verticalChildObject[0].transform.position = verticalCoordinates[i];
                verticalChildObject[1].transform.position = verticalCoordinates[i - 1];
                verticalRotationStopper(i); // check for swipe permissions at new position

                // --- YOUR LOGIC: Check & Lock Slider Directions ---

                // --------------------------------------------------

                try { if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, verticalCoordinates[i + 1])) { if (stop == -1) { stop = i; stopperID = 3; } } }
                catch (System.ArgumentOutOfRangeException)
                {
                    if (verticalChildObject[0].transform.position == verticalCoordinates[verticalCoordinates.Count - 1] &&
                        verticalChildObject[1].transform.position == verticalCoordinates[verticalCoordinates.Count - 2])
                    {
                        verticalflagRadius(i);
                        verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                        ResetSwipePermissions();

                        gameManager.checkRingToDestroy();
                        gameManager.checkYZRingToDestroy();
                        gameManager.checkXZRingToDestroy();
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

        //Debug.Log(leftDiagonalCount);
        //   Debug.Log(rightDiagonalCount);
        //   Debug.Log(verticalCount);
    }

    void CheckChildrenWorldX()
    {
        bool rightStarted = false, verticalStarted = false, leftStarted = false;
        foreach (Transform child in transform)
        {
            float worldX = child.position.x;
            if (worldX < 0f && !leftStarted) { StartCoroutine(moveLeftDiognal(child, leftDiagonalCount)); leftStarted = true; }

            //else if (worldX > 0f && !rightStarted) { StartCoroutine(moveRightDiognal(child, rightDiagonalCount)); rightStarted = true; }
            else if (worldX == 0f && !verticalStarted) { StartCoroutine(moveVertical(child, verticalCount)); verticalStarted = true; }
        }
    }


    void leftflagRadius(int i)
    {

        // 1. Check if Local XY Plane is the one aligned
        // The Normal of Local XY is Local Forward (Z)
        if (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, globalNormalZ)) > 0.99f)
        {
            Debug.Log("left Aligned Plane: Local XY (Axes: Right & Up) with XY");
            if (gameManager.plusXDimension[i] == null && gameManager.plusXDimension[i - 1] == null) { gameManager.plusXDimension[i] = leftChildObject[0]; gameManager.plusXDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.minusXDimension[i] == null && gameManager.minusXDimension[i - 1] == null) { gameManager.minusXDimension[i] = leftChildObject[0]; gameManager.minusXDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.minusYDimension[i] == null && gameManager.minusYDimension[i - 1] == null) { gameManager.minusYDimension[i] = leftChildObject[0]; gameManager.minusYDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.plusYDimension[i] == null && gameManager.plusYDimension[i] == null) { gameManager.plusYDimension[i] = leftChildObject[0]; gameManager.plusYDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.minusXplusYDimension[i] == null && gameManager.minusXplusYDimension[i - 1] == null) { gameManager.minusXplusYDimension[i] = leftChildObject[0]; gameManager.minusXplusYDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.plusXplusYDimension[i] == null && gameManager.plusXplusYDimension[i - 1] == null) { gameManager.plusXplusYDimension[i] = leftChildObject[0]; gameManager.plusXplusYDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.minusXminusYDimension[i] == null && gameManager.minusXminusYDimension[i - 1] == null) { gameManager.minusXminusYDimension[i] = leftChildObject[0]; gameManager.minusXminusYDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.plusXminusYDimension[i] == null && gameManager.plusXminusYDimension[i - 1] == null) { gameManager.plusXminusYDimension[i] = leftChildObject[0]; gameManager.plusXminusYDimension[i - 1] = leftChildObject[1]; }

        }

        // 2. Check if Local YZ Plane is the one aligned with XY
        // The Normal of Local YZ is Local Right (X)
        else if (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, globalNormalZ)) > 0.99f)
        {
            Debug.Log("left Aligned Plane: Local YZ (Axes: Up & Forward) with XY");
            if (gameManager.minusZDimension[i] == null && gameManager.minusZDimension[i - 1] == null) { gameManager.minusZDimension[i] = leftChildObject[0]; gameManager.minusZDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.minusYDimension[i] == null && gameManager.minusYDimension[i - 1] == null) { gameManager.minusYDimension[i] = leftChildObject[0]; gameManager.minusYDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.plusYDimension[i] == null && gameManager.plusYDimension[i - 1] == null) { gameManager.plusYDimension[i] = leftChildObject[0]; gameManager.plusYDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.plusZDimension[i] == null && gameManager.plusZDimension[i - 1] == null) { gameManager.plusZDimension[i] = leftChildObject[0]; gameManager.plusZDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.plusYplusZDimension[i] == null && gameManager.plusYplusZDimension[i - 1] == null) { gameManager.plusYplusZDimension[i] = leftChildObject[0]; gameManager.plusYplusZDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.plusYminusZDimension[i] == null && gameManager.plusYminusZDimension[i - 1] == null) { gameManager.plusYminusZDimension[i] = leftChildObject[0]; gameManager.plusYminusZDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.minusYplusZDimension[i] == null && gameManager.minusYplusZDimension[i - 1] == null) { gameManager.minusYplusZDimension[i] = leftChildObject[0]; gameManager.minusYplusZDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.minusYminusZDimension[i] == null && gameManager.minusYminusZDimension[i - 1] == null) { gameManager.minusYminusZDimension[i] = leftChildObject[0]; gameManager.minusYminusZDimension[i - 1] = leftChildObject[1]; }
        }


        // 3. Check if Local ZX Plane is the one aligned
        // The Normal of Local ZX is Local Up (Y)
        else if (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, globalNormalZ)) > 0.99f)
        {
            Debug.Log("left Aligned Plane: Local ZX (Axes: Right & Forward) with XY");
            if (gameManager.minusZDimension[i] == null && gameManager.minusZDimension[i - 1] == null) { gameManager.minusZDimension[i] = leftChildObject[0]; gameManager.minusZDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.plusXDimension[i] == null && gameManager.plusXDimension[i - 1] == null) { gameManager.plusXDimension[i] = leftChildObject[0]; gameManager.plusXDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.minusXDimension[i] == null && gameManager.minusXDimension[i - 1] == null) { gameManager.minusXDimension[i] = leftChildObject[0]; gameManager.minusXDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.plusZDimension[i] == null && gameManager.plusZDimension[i - 1] == null) { gameManager.plusZDimension[i] = leftChildObject[0]; gameManager.plusZDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.minusXminusZDimension[i] == null && gameManager.minusXminusZDimension[i - 1] == null) { gameManager.minusXminusZDimension[i] = leftChildObject[0]; gameManager.minusXminusZDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.minusXplusZDimension[i] == null && gameManager.minusXplusZDimension[i - 1] == null) { gameManager.minusXplusZDimension[i] = leftChildObject[0]; gameManager.minusXplusZDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.plusXminusZDimension[i] == null && gameManager.plusXminusZDimension[i - 1] == null) { gameManager.plusXminusZDimension[i] = leftChildObject[0]; gameManager.plusXminusZDimension[i - 1] = leftChildObject[1]; }
            else if (gameManager.plusXplusZDimension[i] == null && gameManager.plusXplusZDimension[i - 1] == null) { gameManager.plusXplusZDimension[i] = leftChildObject[0]; gameManager.plusXplusZDimension[i - 1] = leftChildObject[1]; }
        }




    }
    void verticalflagRadius(int i)
    {
        //local XY parallel against global XY


        if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, globalNormalZ)) > 0.99f) && (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, globalNormalX)) > 0.99f))
        {
            // add block in local XY and local zx plane ring

            //adding in XY ring 
            if (gameManager.plusXDimension[i] == null && gameManager.plusXDimension[i - 1] == null) { gameManager.plusXDimension[i] = verticalChildObject[0]; gameManager.plusXDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusYDimension[i] == null && gameManager.plusYDimension[i - 1] == null) { gameManager.plusYDimension[i] = verticalChildObject[0]; gameManager.plusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i] == null && gameManager.minusXDimension[i - 1] == null) { gameManager.minusXDimension[i] = verticalChildObject[0]; gameManager.minusXDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i] == null && gameManager.minusYDimension[i - 1] == null) { gameManager.minusYDimension[i] = verticalChildObject[0]; gameManager.minusYDimension[i - 1] = verticalChildObject[1]; }

            else if (gameManager.minusXplusYDimension[i] == null && gameManager.minusXplusYDimension[i - 1] == null) { gameManager.minusXplusYDimension[i] = verticalChildObject[0]; gameManager.minusXplusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusXplusYDimension[i] == null && gameManager.plusXplusYDimension[i - 1] == null) { gameManager.plusXplusYDimension[i] = verticalChildObject[0]; gameManager.plusXplusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusXminusYDimension[i] == null && gameManager.minusXminusYDimension[i - 1] == null) { gameManager.minusXminusYDimension[i] = verticalChildObject[0]; gameManager.minusXminusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusXminusYDimension[i] == null && gameManager.plusXminusYDimension[i - 1] == null) { gameManager.plusXminusYDimension[i] = verticalChildObject[0]; gameManager.plusXminusYDimension[i - 1] = verticalChildObject[1]; }


            // adding in ZX plane
            if (gameManager.plusXDimension[i] == null && gameManager.plusXDimension[i - 1] == null) { gameManager.plusXDimension[i] = verticalChildObject[0]; gameManager.plusXDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i] == null && gameManager.plusZDimension[i - 1] == null) { gameManager.plusZDimension[i] = verticalChildObject[0]; gameManager.plusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i] == null && gameManager.minusXDimension[i - 1] == null) { gameManager.minusXDimension[i] = verticalChildObject[0]; gameManager.minusXDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i] == null && gameManager.minusZDimension[i - 1] == null) { gameManager.minusZDimension[i] = verticalChildObject[0]; gameManager.minusZDimension[i - 1] = verticalChildObject[1]; }

            else if (gameManager.minusXminusZDimension[i] == null && gameManager.minusXminusZDimension[i - 1] == null) { gameManager.minusXminusZDimension[i] = verticalChildObject[0]; gameManager.minusXminusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusXplusZDimension[i] == null && gameManager.minusXplusZDimension[i - 1] == null) { gameManager.minusXplusZDimension[i] = verticalChildObject[0]; gameManager.minusXplusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusXminusZDimension[i] == null && gameManager.plusXminusZDimension[i - 1] == null) { gameManager.plusXminusZDimension[i] = verticalChildObject[0]; gameManager.plusXminusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusXplusZDimension[i] == null && gameManager.plusXplusZDimension[i - 1] == null) { gameManager.plusXplusZDimension[i] = verticalChildObject[0]; gameManager.plusXplusZDimension[i - 1] = verticalChildObject[1]; }

        }
        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, globalNormalZ)) > 0.99f) && (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, globalNormalX)) > 0.99f))
        {
            // add block in local XY and local yz  plane ring 

            //Block in XY plane
            if (gameManager.plusXDimension[i] == null && gameManager.plusXDimension[i - 1] == null) { gameManager.plusXDimension[i] = verticalChildObject[0]; gameManager.plusXDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusYDimension[i] == null && gameManager.plusYDimension[i - 1] == null) { gameManager.plusYDimension[i] = verticalChildObject[0]; gameManager.plusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i] == null && gameManager.minusXDimension[i - 1] == null) { gameManager.minusXDimension[i] = verticalChildObject[0]; gameManager.minusXDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i] == null && gameManager.minusYDimension[i - 1] == null) { gameManager.minusYDimension[i] = verticalChildObject[0]; gameManager.minusYDimension[i - 1] = verticalChildObject[1]; }

            else if (gameManager.minusXplusYDimension[i] == null && gameManager.minusXplusYDimension[i - 1] == null) { gameManager.minusXplusYDimension[i] = verticalChildObject[0]; gameManager.minusXplusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusXplusYDimension[i] == null && gameManager.plusXplusYDimension[i - 1] == null) { gameManager.plusXplusYDimension[i] = verticalChildObject[0]; gameManager.plusXplusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusXminusYDimension[i] == null && gameManager.minusXminusYDimension[i - 1] == null) { gameManager.minusXminusYDimension[i] = verticalChildObject[0]; gameManager.minusXminusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusXminusYDimension[i] == null && gameManager.plusXminusYDimension[i - 1] == null) { gameManager.plusXminusYDimension[i] = verticalChildObject[0]; gameManager.plusXminusYDimension[i - 1] = verticalChildObject[1]; }

            // Block in YZ local plane
            if (gameManager.plusYDimension[i] == null && gameManager.plusYDimension[i - 1] == null) { gameManager.plusYDimension[i] = verticalChildObject[0]; gameManager.plusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i] == null && gameManager.plusZDimension[i - 1] == null) { gameManager.plusZDimension[i] = verticalChildObject[0]; gameManager.plusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i] == null && gameManager.minusYDimension[i - 1] == null) { gameManager.minusYDimension[i] = verticalChildObject[0]; gameManager.minusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i] == null && gameManager.minusZDimension[i - 1] == null) { gameManager.minusZDimension[i] = verticalChildObject[0]; gameManager.minusZDimension[i - 1] = verticalChildObject[1]; }

            else if (gameManager.plusYplusZDimension[i] == null && gameManager.plusYplusZDimension[i - 1] == null) { gameManager.plusYplusZDimension[i] = verticalChildObject[0]; gameManager.plusYplusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusYminusZDimension[i] == null && gameManager.plusYminusZDimension[i - 1] == null) { gameManager.plusYminusZDimension[i] = verticalChildObject[0]; gameManager.plusYminusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusYplusZDimension[i] == null && gameManager.minusYplusZDimension[i - 1] == null) { gameManager.minusYplusZDimension[i] = verticalChildObject[0]; gameManager.minusYplusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusYminusZDimension[i] == null && gameManager.minusYminusZDimension[i - 1] == null) { gameManager.minusYminusZDimension[i] = verticalChildObject[0]; gameManager.minusYminusZDimension[i - 1] = verticalChildObject[1]; }

        }
        //


        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, globalNormalZ)) > 0.99f) && (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, globalNormalX)) > 0.99f))
        {
            // add block in local YZ and local XY plane ring 

            //Block in XY
            if (gameManager.plusXDimension[i] == null && gameManager.plusXDimension[i - 1] == null) { gameManager.plusXDimension[i] = verticalChildObject[0]; gameManager.plusXDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusYDimension[i] == null && gameManager.plusYDimension[i - 1] == null) { gameManager.plusYDimension[i] = verticalChildObject[0]; gameManager.plusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i] == null && gameManager.minusXDimension[i - 1] == null) { gameManager.minusXDimension[i] = verticalChildObject[0]; gameManager.minusXDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i] == null && gameManager.minusYDimension[i - 1] == null) { gameManager.minusYDimension[i] = verticalChildObject[0]; gameManager.minusYDimension[i - 1] = verticalChildObject[1]; }

            else if (gameManager.minusXplusYDimension[i] == null && gameManager.minusXplusYDimension[i - 1] == null) { gameManager.minusXplusYDimension[i] = verticalChildObject[0]; gameManager.minusXplusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusXplusYDimension[i] == null && gameManager.plusXplusYDimension[i - 1] == null) { gameManager.plusXplusYDimension[i] = verticalChildObject[0]; gameManager.plusXplusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusXminusYDimension[i] == null && gameManager.minusXminusYDimension[i - 1] == null) { gameManager.minusXminusYDimension[i] = verticalChildObject[0]; gameManager.minusXminusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusXminusYDimension[i] == null && gameManager.plusXminusYDimension[i - 1] == null) { gameManager.plusXminusYDimension[i] = verticalChildObject[0]; gameManager.plusXminusYDimension[i - 1] = verticalChildObject[1]; }

            //Block in YZ local
            if (gameManager.plusYDimension[i] == null && gameManager.plusYDimension[i - 1] == null) { gameManager.plusYDimension[i] = verticalChildObject[0]; gameManager.plusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i] == null && gameManager.plusZDimension[i - 1] == null) { gameManager.plusZDimension[i] = verticalChildObject[0]; gameManager.plusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i] == null && gameManager.minusYDimension[i - 1] == null) { gameManager.minusYDimension[i] = verticalChildObject[0]; gameManager.minusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i] == null && gameManager.minusZDimension[i - 1] == null) { gameManager.minusZDimension[i] = verticalChildObject[0]; gameManager.minusZDimension[i - 1] = verticalChildObject[1]; }

            else if (gameManager.plusYplusZDimension[i] == null && gameManager.plusYplusZDimension[i - 1] == null) { gameManager.plusYplusZDimension[i] = verticalChildObject[0]; gameManager.plusYplusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusYminusZDimension[i] == null && gameManager.plusYminusZDimension[i - 1] == null) { gameManager.plusYminusZDimension[i] = verticalChildObject[0]; gameManager.plusYminusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusYplusZDimension[i] == null && gameManager.minusYplusZDimension[i - 1] == null) { gameManager.minusYplusZDimension[i] = verticalChildObject[0]; gameManager.minusYplusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusYminusZDimension[i] == null && gameManager.minusYminusZDimension[i - 1] == null) { gameManager.minusYminusZDimension[i] = verticalChildObject[0]; gameManager.minusYminusZDimension[i - 1] = verticalChildObject[1]; }


        }


        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, globalNormalZ)) > 0.99f) && (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, globalNormalX)) > 0.99f))
        {
            // add block in local YZ and local XZ plane ring

            //Block in YZ
            if (gameManager.plusYDimension[i] == null && gameManager.plusYDimension[i - 1] == null) { gameManager.plusYDimension[i] = verticalChildObject[0]; gameManager.plusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i] == null && gameManager.plusZDimension[i - 1] == null) { gameManager.plusZDimension[i] = verticalChildObject[0]; gameManager.plusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i] == null && gameManager.minusYDimension[i - 1] == null) { gameManager.minusYDimension[i] = verticalChildObject[0]; gameManager.minusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i] == null && gameManager.minusZDimension[i - 1] == null) { gameManager.minusZDimension[i] = verticalChildObject[0]; gameManager.minusZDimension[i - 1] = verticalChildObject[1]; }

            else if (gameManager.plusYplusZDimension[i] == null && gameManager.plusYplusZDimension[i - 1] == null) { gameManager.plusYplusZDimension[i] = verticalChildObject[0]; gameManager.plusYplusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusYminusZDimension[i] == null && gameManager.plusYminusZDimension[i - 1] == null) { gameManager.plusYminusZDimension[i] = verticalChildObject[0]; gameManager.plusYminusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusYplusZDimension[i] == null && gameManager.minusYplusZDimension[i - 1] == null) { gameManager.minusYplusZDimension[i] = verticalChildObject[0]; gameManager.minusYplusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusYminusZDimension[i] == null && gameManager.minusYminusZDimension[i - 1] == null) { gameManager.minusYminusZDimension[i] = verticalChildObject[0]; gameManager.minusYminusZDimension[i - 1] = verticalChildObject[1]; }

            //Block in XZ ring
            if (gameManager.plusXDimension[i] == null && gameManager.plusXDimension[i - 1] == null) { gameManager.plusXDimension[i] = verticalChildObject[0]; gameManager.plusXDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i] == null && gameManager.plusZDimension[i - 1] == null) { gameManager.plusZDimension[i] = verticalChildObject[0]; gameManager.plusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i] == null && gameManager.minusXDimension[i - 1] == null) { gameManager.minusXDimension[i] = verticalChildObject[0]; gameManager.minusXDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i] == null && gameManager.minusZDimension[i - 1] == null) { gameManager.minusZDimension[i] = verticalChildObject[0]; gameManager.minusZDimension[i - 1] = verticalChildObject[1]; }

            else if (gameManager.minusXminusZDimension[i] == null && gameManager.minusXminusZDimension[i - 1] == null) { gameManager.minusXminusZDimension[i] = verticalChildObject[0]; gameManager.minusXminusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusXplusZDimension[i] == null && gameManager.minusXplusZDimension[i - 1] == null) { gameManager.minusXplusZDimension[i] = verticalChildObject[0]; gameManager.minusXplusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusXminusZDimension[i] == null && gameManager.plusXminusZDimension[i - 1] == null) { gameManager.plusXminusZDimension[i] = verticalChildObject[0]; gameManager.plusXminusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusXplusZDimension[i] == null && gameManager.plusXplusZDimension[i - 1] == null) { gameManager.plusXplusZDimension[i] = verticalChildObject[0]; gameManager.plusXplusZDimension[i - 1] = verticalChildObject[1]; }
        }
        //


        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, globalNormalZ)) > 0.99f) && (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.forward, globalNormalX)) > 0.99f))
        {
            // add block in  local XZ plane and local XY 1
            //Block in XZ plane 
            if (gameManager.plusXDimension[i] == null && gameManager.plusXDimension[i - 1] == null) { gameManager.plusXDimension[i] = verticalChildObject[0]; gameManager.plusXDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i] == null && gameManager.plusZDimension[i - 1] == null) { gameManager.plusZDimension[i] = verticalChildObject[0]; gameManager.plusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i] == null && gameManager.minusXDimension[i - 1] == null) { gameManager.minusXDimension[i] = verticalChildObject[0]; gameManager.minusXDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i] == null && gameManager.minusZDimension[i - 1] == null) { gameManager.minusZDimension[i] = verticalChildObject[0]; gameManager.minusZDimension[i - 1] = verticalChildObject[1]; }

            else if (gameManager.minusXminusZDimension[i] == null && gameManager.minusXminusZDimension[i - 1] == null) { gameManager.minusXminusZDimension[i] = verticalChildObject[0]; gameManager.minusXminusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusXplusZDimension[i] == null && gameManager.minusXplusZDimension[i - 1] == null) { gameManager.minusXplusZDimension[i] = verticalChildObject[0]; gameManager.minusXplusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusXminusZDimension[i] == null && gameManager.plusXminusZDimension[i - 1] == null) { gameManager.plusXminusZDimension[i] = verticalChildObject[0]; gameManager.plusXminusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusXplusZDimension[i] == null && gameManager.plusXplusZDimension[i - 1] == null) { gameManager.plusXplusZDimension[i] = verticalChildObject[0]; gameManager.plusXplusZDimension[i - 1] = verticalChildObject[1]; }

            //Block in XY
            if (gameManager.plusXDimension[i] == null && gameManager.plusXDimension[i - 1] == null) { gameManager.plusXDimension[i] = verticalChildObject[0]; gameManager.plusXDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusYDimension[i] == null && gameManager.plusYDimension[i - 1] == null) { gameManager.plusYDimension[i] = verticalChildObject[0]; gameManager.plusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i] == null && gameManager.minusXDimension[i - 1] == null) { gameManager.minusXDimension[i] = verticalChildObject[0]; gameManager.minusXDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i] == null && gameManager.minusYDimension[i - 1] == null) { gameManager.minusYDimension[i] = verticalChildObject[0]; gameManager.minusYDimension[i - 1] = verticalChildObject[1]; }

            else if (gameManager.minusXplusYDimension[i] == null && gameManager.minusXplusYDimension[i - 1] == null) { gameManager.minusXplusYDimension[i] = verticalChildObject[0]; gameManager.minusXplusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusXplusYDimension[i] == null && gameManager.plusXplusYDimension[i - 1] == null) { gameManager.plusXplusYDimension[i] = verticalChildObject[0]; gameManager.plusXplusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusXminusYDimension[i] == null && gameManager.minusXminusYDimension[i - 1] == null) { gameManager.minusXminusYDimension[i] = verticalChildObject[0]; gameManager.minusXminusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusXminusYDimension[i] == null && gameManager.plusXminusYDimension[i - 1] == null) { gameManager.plusXminusYDimension[i] = verticalChildObject[0]; gameManager.plusXminusYDimension[i - 1] = verticalChildObject[1]; }
        }
        else if ((Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.up, globalNormalZ)) > 0.99f) && (Mathf.Abs(Vector3.Dot(gameManager.motherPlatform.transform.right, globalNormalX)) > 0.99f))
        {
            // add block in local XZ and local YZ plane

            //Block in XZ plane
            if (gameManager.plusXDimension[i] == null && gameManager.plusXDimension[i - 1] == null) { gameManager.plusXDimension[i] = verticalChildObject[0]; gameManager.plusXDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i] == null && gameManager.plusZDimension[i - 1] == null) { gameManager.plusZDimension[i] = verticalChildObject[0]; gameManager.plusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusXDimension[i] == null && gameManager.minusXDimension[i - 1] == null) { gameManager.minusXDimension[i] = verticalChildObject[0]; gameManager.minusXDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i] == null && gameManager.minusZDimension[i - 1] == null) { gameManager.minusZDimension[i] = verticalChildObject[0]; gameManager.minusZDimension[i - 1] = verticalChildObject[1]; }

            else if (gameManager.minusXminusZDimension[i] == null && gameManager.minusXminusZDimension[i - 1] == null) { gameManager.minusXminusZDimension[i] = verticalChildObject[0]; gameManager.minusXminusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusXplusZDimension[i] == null && gameManager.minusXplusZDimension[i - 1] == null) { gameManager.minusXplusZDimension[i] = verticalChildObject[0]; gameManager.minusXplusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusXminusZDimension[i] == null && gameManager.plusXminusZDimension[i - 1] == null) { gameManager.plusXminusZDimension[i] = verticalChildObject[0]; gameManager.plusXminusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusXplusZDimension[i] == null && gameManager.plusXplusZDimension[i - 1] == null) { gameManager.plusXplusZDimension[i] = verticalChildObject[0]; gameManager.plusXplusZDimension[i - 1] = verticalChildObject[1]; }

            //Block in YZ plane

            if (gameManager.plusYDimension[i] == null && gameManager.plusYDimension[i - 1] == null) { gameManager.plusYDimension[i] = verticalChildObject[0]; gameManager.plusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusZDimension[i] == null && gameManager.plusZDimension[i - 1] == null) { gameManager.plusZDimension[i] = verticalChildObject[0]; gameManager.plusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusYDimension[i] == null && gameManager.minusYDimension[i - 1] == null) { gameManager.minusYDimension[i] = verticalChildObject[0]; gameManager.minusYDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusZDimension[i] == null && gameManager.minusZDimension[i - 1] == null) { gameManager.minusZDimension[i] = verticalChildObject[0]; gameManager.minusZDimension[i - 1] = verticalChildObject[1]; }

            else if (gameManager.plusYplusZDimension[i] == null && gameManager.plusYplusZDimension[i - 1] == null) { gameManager.plusYplusZDimension[i] = verticalChildObject[0]; gameManager.plusYplusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.plusYminusZDimension[i] == null && gameManager.plusYminusZDimension[i - 1] == null) { gameManager.plusYminusZDimension[i] = verticalChildObject[0]; gameManager.plusYminusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusYplusZDimension[i] == null && gameManager.minusYplusZDimension[i - 1] == null) { gameManager.minusYplusZDimension[i] = verticalChildObject[0]; gameManager.minusYplusZDimension[i - 1] = verticalChildObject[1]; }
            else if (gameManager.minusYminusZDimension[i] == null && gameManager.minusYminusZDimension[i - 1] == null) { gameManager.minusYminusZDimension[i] = verticalChildObject[0]; gameManager.minusYminusZDimension[i - 1] = verticalChildObject[1]; }
        }
        else
        {
            Debug.LogError("vertical flag radius failed");
        }



    }


}
