using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using UnityEngine;



public class TMovement : MonoBehaviour
{
    int leftDiagonalCount = 0;
    int rightDiagonalCount = 0;
    int verticalCount = 0;
    float moveSpeed = 2f;

    List<Vector3> leftDiagonalCoordinates = new List<Vector3>();
    List<Vector3> rightDiagonalCoordinates = new List<Vector3>();
    List<Vector3> verticalCoordinates = new List<Vector3>();


    List<GameObject> leftChildObject = new List<GameObject>();
    List<GameObject> rightChildObject = new List<GameObject>();
    List<GameObject> verticalChildObject = new List<GameObject>();


    public GameManager gameManager;
    public SwipeInput swipeInput;

    void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            // Note: use FindObjectOfType<GameManager>() if on older Unity versions
        }
        if (swipeInput == null)
        {
            swipeInput = FindFirstObjectByType<SwipeInput>();
            // Note: use FindObjectOfType<SwipeInput>() if on older Unity versions
        }
        // Populate left diagonal coordinates
        for (float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f)
        {
            leftDiagonalCoordinates.Add(new Vector3(-v, v, 0f));

        }

        // Populate right diagonal coordinates
        for (float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f)
        {
            rightDiagonalCoordinates.Add(new Vector3(v, v, 0f));
        }

        // Populate vertical coordinates
        for (float v = 14.5f; v >= 2.5f; v -= 1f)
        {
            verticalCoordinates.Add(new Vector3(0f, v, 0f));
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        countChildren();
        CheckChildrenWorldX();
    }

    // Update is called once per frame
    void Update()
    {

    }


    void leftflagRadius(int i)
    {


        if (gameManager.plusXDimension[i - 1] == null)
        {
            gameManager.plusXDimension[i - 1] = leftChildObject[0];
            Debug.Log("Left block added to plusXDimension at index " + (i - 1));
        }
        //
        else if (gameManager.plusYDimension[i - 1] == null)
        {
            gameManager.plusYDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to plusYDimension at index " + (i - 1));
        }
        else if (gameManager.plusZDimension[i - 1] == null)
        {
            gameManager.plusZDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to plusZDimension at index " + (i - 1));
        }



        else if (gameManager.minusXDimension[i - 1] == null)
        {
            gameManager.minusXDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to minusXDimension at index " + (i - 1));
        }
        else if (gameManager.minusYDimension[i - 1] == null)
        {
            gameManager.minusYDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to minusYDimension at index " + (i - 1));
        }
        else if (gameManager.minusZDimension[i - 1] == null)
        {
            gameManager.minusZDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to minusZDimension at index " + (i - 1));
        }


        else if (gameManager.plusYplusZDimension[i - 1] == null)
        {
            gameManager.plusYplusZDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to plusYplusZDimension at index " + (i - 1));
        }
        else if (gameManager.plusYminusZDimension[i - 1] == null)
        {
            gameManager.plusYminusZDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to plusYminusZDimension at index " + (i - 1));
        }
        else if (gameManager.minusYplusZDimension[i - 1] == null)
        {
            gameManager.minusYplusZDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to minusYplusZDimension at index " + (i - 1));
        }
        else if (gameManager.minusYminusZDimension[i - 1] == null)
        {
            gameManager.minusYminusZDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to minusYminusZDimension at index " + (i - 1));
        }




        else if (gameManager.minusXminusZDimension[i - 1] == null)
        {
            gameManager.minusXminusZDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to minusXminusZDimension at index " + (i - 1));
        }
        else if (gameManager.minusXplusZDimension[i - 1] == null)
        {
            gameManager.minusXplusZDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to minusXplusZDimension at index " + (i - 1));
        }
        else if (gameManager.plusXminusZDimension[i - 1] == null)
        {
            gameManager.plusXminusZDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to plusXminusZDimension at index " + (i - 1));
        }
        else if (gameManager.plusXplusZDimension[i - 1] == null)
        {
            gameManager.plusXplusZDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to plusXplusZDimension at index " + (i - 1));
        }



        else if (gameManager.minusXplusYDimension[i - 1] == null)
        {
            gameManager.minusXplusYDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to minusXplusYDimension at index " + (i - 1));
        }
        else if (gameManager.plusXplusYDimension[i - 1] == null)
        {
            gameManager.plusXplusYDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to plusXplusYDimension at index " + (i - 1));
        }
        else if (gameManager.minusXminusYDimension[i - 1] == null)
        {
            gameManager.minusXminusYDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to minusXminusYDimension at index " + (i - 1));
        }
        else if (gameManager.plusXminusYDimension[i - 1] == null)
        {
            gameManager.plusXminusYDimension[i - 1] = leftChildObject[0];
            Debug.Log("left block added to plusXminusYDimension at index " + (i - 1));
        }

    }


    void rightflagRadius(int i)
    {
        // 1. Single Axes (Faces)
        if (gameManager.plusXDimension[i - 1] == null)
        {
            gameManager.plusXDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to plusXDimension at index " + (i - 1));
        }
        else if (gameManager.plusYDimension[i - 1] == null)
        {
            gameManager.plusYDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to plusYDimension at index " + (i - 1));
        }
        else if (gameManager.plusZDimension[i - 1] == null)
        {
            gameManager.plusZDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to plusZDimension at index " + (i - 1));
        }
        else if (gameManager.minusXDimension[i - 1] == null)
        {
            gameManager.minusXDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to minusXDimension at index " + (i - 1));
        }
        else if (gameManager.minusYDimension[i - 1] == null)
        {
            gameManager.minusYDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to minusYDimension at index " + (i - 1));
        }
        else if (gameManager.minusZDimension[i - 1] == null)
        {
            gameManager.minusZDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to minusZDimension at index " + (i - 1));
        }

        // 2. Combined Axes (Y and Z)
        else if (gameManager.plusYplusZDimension[i - 1] == null)
        {
            gameManager.plusYplusZDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to plusYplusZDimension at index " + (i - 1));
        }
        else if (gameManager.plusYminusZDimension[i - 1] == null)
        {
            gameManager.plusYminusZDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to plusYminusZDimension at index " + (i - 1));
        }
        else if (gameManager.minusYplusZDimension[i - 1] == null)
        {
            gameManager.minusYplusZDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to minusYplusZDimension at index " + (i - 1));
        }
        else if (gameManager.minusYminusZDimension[i - 1] == null)
        {
            gameManager.minusYminusZDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to minusYminusZDimension at index " + (i - 1));
        }

        // 3. Combined Axes (X and Z)
        else if (gameManager.minusXminusZDimension[i - 1] == null)
        {
            gameManager.minusXminusZDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to minusXminusZDimension at index " + (i - 1));
        }
        else if (gameManager.minusXplusZDimension[i - 1] == null)
        {
            gameManager.minusXplusZDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to minusXplusZDimension at index " + (i - 1));
        }
        else if (gameManager.plusXminusZDimension[i - 1] == null)
        {
            gameManager.plusXminusZDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to plusXminusZDimension at index " + (i - 1));
        }
        else if (gameManager.plusXplusZDimension[i - 1] == null)
        {
            gameManager.plusXplusZDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to plusXplusZDimension at index " + (i - 1));
        }

        // 4. Combined Axes (X and Y)
        else if (gameManager.minusXplusYDimension[i - 1] == null)
        {
            gameManager.minusXplusYDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to minusXplusYDimension at index " + (i - 1));
        }
        else if (gameManager.plusXplusYDimension[i - 1] == null)
        {
            gameManager.plusXplusYDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to plusXplusYDimension at index " + (i - 1));
        }
        else if (gameManager.minusXminusYDimension[i - 1] == null)
        {
            gameManager.minusXminusYDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to minusXminusYDimension at index " + (i - 1));
        }
        else if (gameManager.plusXminusYDimension[i - 1] == null)
        {
            gameManager.plusXminusYDimension[i - 1] = rightChildObject[0];
            Debug.Log("Right block added to plusXminusYDimension at index " + (i - 1));
        }
        // Optional: Final Else if NO space is found
        else
        {
            Debug.Log("No empty space found for Right block at index " + (i - 1));
        }
    }


    void verticalflagRadius(int i)
    {
        // 1. Single Axes (Faces)
        if (gameManager.plusXDimension[i - 1] == null && gameManager.plusXDimension[i - 2] == null)
        {
            gameManager.plusXDimension[i - 1] = verticalChildObject[0];
            gameManager.plusXDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to plusXDimension at index " + (i - 1) + ", " + (i - 2));
        }
        else if (gameManager.plusYDimension[i - 1] == null && gameManager.plusYDimension[i - 2] == null)
        {
            gameManager.plusYDimension[i - 1] = verticalChildObject[0];
            gameManager.plusYDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to plusYDimension at index " + (i - 1) + ", " + (i - 2));
        }
        else if (gameManager.plusZDimension[i - 1] == null && gameManager.plusZDimension[i - 2] == null)
        {
            gameManager.plusZDimension[i - 1] = verticalChildObject[0];
            gameManager.plusZDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to plusZDimension at index " + (i - 1) + ", " + (i - 2));
        }
        else if (gameManager.minusXDimension[i - 1] == null && gameManager.minusXDimension[i - 2] == null)
        {
            gameManager.minusXDimension[i - 1] = verticalChildObject[0];
            gameManager.minusXDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to minusXDimension at index " + (i - 1) + ", " + (i - 2));
        }
        else if (gameManager.minusYDimension[i - 1] == null && gameManager.minusYDimension[i - 2] == null)
        {
            gameManager.minusYDimension[i - 1] = verticalChildObject[0];
            gameManager.minusYDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to minusYDimension at index " + (i - 1) + ", " + (i - 2));
        }
        else if (gameManager.minusZDimension[i - 1] == null && gameManager.minusZDimension[i - 2] == null)
        {
            gameManager.minusZDimension[i - 1] = verticalChildObject[0];
            gameManager.minusZDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to minusZDimension at index " + (i - 1) + ", " + (i - 2));
        }

        // 2. Combined Axes (Y and Z)
        else if (gameManager.plusYplusZDimension[i - 1] == null && gameManager.plusYplusZDimension[i - 2] == null)
        {
            gameManager.plusYplusZDimension[i - 1] = verticalChildObject[0];
            gameManager.plusYplusZDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to plusYplusZDimension at index " + (i - 1) + ", " + (i - 2));
        }
        else if (gameManager.plusYminusZDimension[i - 1] == null && gameManager.plusYminusZDimension[i - 2] == null)
        {
            gameManager.plusYminusZDimension[i - 1] = verticalChildObject[0];
            gameManager.plusYminusZDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to plusYminusZDimension at index " + (i - 1) + ", " + (i - 2));
        }
        else if (gameManager.minusYplusZDimension[i - 1] == null && gameManager.minusYplusZDimension[i - 2] == null)
        {
            gameManager.minusYplusZDimension[i - 1] = verticalChildObject[0];
            gameManager.minusYplusZDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to minusYplusZDimension at index " + (i - 1) + ", " + (i - 2));
        }
        else if (gameManager.minusYminusZDimension[i - 1] == null && gameManager.minusYminusZDimension[i - 2] == null)
        {
            gameManager.minusYminusZDimension[i - 1] = verticalChildObject[0];
            gameManager.minusYminusZDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to minusYminusZDimension at index " + (i - 1) + ", " + (i - 2));
        }

        // 3. Combined Axes (X and Z)
        else if (gameManager.minusXminusZDimension[i - 1] == null && gameManager.minusXminusZDimension[i - 2] == null)
        {
            gameManager.minusXminusZDimension[i - 1] = verticalChildObject[0];
            gameManager.minusXminusZDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to minusXminusZDimension at index " + (i - 1) + ", " + (i - 2));
        }
        else if (gameManager.minusXplusZDimension[i - 1] == null && gameManager.minusXplusZDimension[i - 2] == null)
        {
            gameManager.minusXplusZDimension[i - 1] = verticalChildObject[0];
            gameManager.minusXplusZDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to minusXplusZDimension at index " + (i - 1) + ", " + (i - 2));
        }
        else if (gameManager.plusXminusZDimension[i - 1] == null && gameManager.plusXminusZDimension[i - 2] == null)
        {
            gameManager.plusXminusZDimension[i - 1] = verticalChildObject[0];
            gameManager.plusXminusZDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to plusXminusZDimension at index " + (i - 1) + ", " + (i - 2));
        }
        else if (gameManager.plusXplusZDimension[i - 1] == null && gameManager.plusXplusZDimension[i - 2] == null)
        {
            gameManager.plusXplusZDimension[i - 1] = verticalChildObject[0];
            gameManager.plusXplusZDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to plusXplusZDimension at index " + (i - 1) + ", " + (i - 2));
        }

        // 4. Combined Axes (X and Y)
        else if (gameManager.minusXplusYDimension[i - 1] == null && gameManager.minusXplusYDimension[i - 2] == null)
        {
            gameManager.minusXplusYDimension[i - 1] = verticalChildObject[0];
            gameManager.minusXplusYDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to minusXplusYDimension at index " + (i - 1) + ", " + (i - 2));
        }
        else if (gameManager.plusXplusYDimension[i - 1] == null && gameManager.plusXplusYDimension[i - 2] == null)
        {
            gameManager.plusXplusYDimension[i - 1] = verticalChildObject[0];
            gameManager.plusXplusYDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to plusXplusYDimension at index " + (i - 1) + ", " + (i - 2));
        }
        else if (gameManager.minusXminusYDimension[i - 1] == null && gameManager.minusXminusYDimension[i - 2] == null)
        {
            gameManager.minusXminusYDimension[i - 1] = verticalChildObject[0];
            gameManager.minusXminusYDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to minusXminusYDimension at index " + (i - 1) + ", " + (i - 2));
        }
        else if (gameManager.plusXminusYDimension[i - 1] == null && gameManager.plusXminusYDimension[i - 2] == null)
        {
            gameManager.plusXminusYDimension[i - 1] = verticalChildObject[0];
            gameManager.plusXminusYDimension[i - 2] = verticalChildObject[1];
            Debug.Log("Vertical block added to plusXminusYDimension at index " + (i - 1) + ", " + (i - 2));
        }
    }





    // void CheckChildrenWorldX()
    // {
    //     //this for loop is iterationg through every child of the current game object's world position
    //     //Transform is a data type and transform is a collection variable that holds all the children of the current game object
    //     foreach (Transform child in transform)
    //     {
    //         float worldX = child.position.x; // WORLD position
    //         ////////////left///////////////
    //         if (worldX < 0f)
    //         {
    //             Debug.Log($"{child.name} is on the LEFT side (world X < 0): {worldX}");

    //             //iterate through some specific provided coordinates for left diognal
    //             StartCoroutine(moveLeftDiognal(child, leftDiagonalCount));


    //         }
    //         ////////////center///////////////
    //         else if (worldX == 0f)
    //         {
    //             Debug.Log($"{child.name} is at the CENTER (world X == 0): {worldX}");
    //             StartCoroutine(moveVertical(child, verticalCount));
    //         }


    //         ////////////right///////////////
    //         else if (worldX > 0f)
    //         {
    //             Debug.Log($"{child.name} is on the RIGHT side (world X >= 0): {worldX}");
    //             StartCoroutine(moveRightDiognal(child, rightDiagonalCount));
    //         }
    //     }

    // }

    //bool stopNextIteration = false;
    //baksodi comm



    void CheckChildrenWorldX()
    {
        // Flags to ensure we only start each coroutine ONCE per group
        bool leftStarted = false;
        bool rightStarted = false;
        bool verticalStarted = false;

        foreach (Transform child in transform)
        {
            float worldX = child.position.x;

            //////////// Left ///////////////
            if (worldX < 0f)
            {
                // Only start if we haven't started the Left group yet
                if (!leftStarted)
                {
                    Debug.Log($"Starting Left Diagonal Movement");
                    StartCoroutine(moveLeftDiognal(child, leftDiagonalCount));
                    leftStarted = true;
                }
            }
            //////////// Center (Vertical) ///////////////
            else if (worldX == 0f)
            {
                // Only start if we haven't started the Vertical group yet
                if (!verticalStarted)
                {
                    Debug.Log($"Starting Vertical Movement");
                    StartCoroutine(moveVertical(child, verticalCount));
                    verticalStarted = true;
                }
            }
            //////////// Right ///////////////
            else if (worldX > 0f)
            {
                // Only start if we haven't started the Right group yet
                if (!rightStarted)
                {
                    Debug.Log($"Starting Right Diagonal Movement");
                    StartCoroutine(moveRightDiognal(child, rightDiagonalCount));
                    rightStarted = true;
                }
            }
        }
    }



    int stop = -1;

    int stopperID = 0;

    IEnumerator moveLeftDiognal(Transform child, int childCount)
    {
        if (leftChildObject == null || leftChildObject.Count == 0)
        {
            yield break; // Exit the coroutine if there are no child objects
        }




        if (childCount == 1)
        {


            for (int i = 2; i < leftDiagonalCoordinates.Count; i++)
            {


                // -------------------------------------------------------------
                // DOUBLE CHECK LOGIC (With Identity Check)
                // -------------------------------------------------------------
                if (stop != -1 && i > stop)
                {
                    // CASE A: I was the one who stopped the movement (stopperID == 1)
                    if (stopperID == 1)
                    {
                        // Check if MY path is still blocked
                        bool isStillBlocked = false;
                        try
                        {
                            isStillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[i]);
                        }
                        catch { isStillBlocked = false; }

                        if (isStillBlocked)
                        {
                            Debug.Log("Left confirmed blocked. Stopping all.");

                            leftflagRadius(i);

                            leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            enabled = false;
                            yield break;
                        }
                        else
                        {
                            // My path cleared! I can reset the global stop.
                            Debug.Log("Left block cleared. Resuming.");

                            stop = -1;
                            stopperID = 0;
                        }
                    }
                    // CASE B: Someone ELSE stopped the movement (Right or Vertical)
                    else
                    {
                        Debug.Log("Left Waiting synchronously for path to clear...");

                        // --- WAITING LOGIC ---
                        while (stop != -1 && stopperID != 1)
                        {
                            if (!enabled)
                            {
                                leftflagRadius(i);

                                leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                yield break;
                            }
                            yield return null; // Wait frame by frame
                        }
                        // Resume automatically when loop breaks
                    }
                }

                // -------------------------------------------------------------
                // MOVE
                // -------------------------------------------------------------
                leftChildObject[0].transform.position = leftDiagonalCoordinates[i];
                

                // -------------------------------------------------------------
                // DETECTION
                // -------------------------------------------------------------
                try
                {
                    if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, leftDiagonalCoordinates[i + 1]))
                    {
                        if (stop == -1)
                        {
                            stop = i;
                            stopperID = 1; // Mark that LEFT caused the stop
                        }
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    if (leftChildObject[0].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 1])
                    {
                        leftflagRadius(i + 1);

                        leftChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                    }
                    yield break;
                }

                yield return new WaitForSeconds(moveSpeed);
            }
        }





    }


    IEnumerator moveRightDiognal(Transform child, int childCount)
    {
        if (rightChildObject == null || rightChildObject.Count == 0)
        {
            yield break; // Exit the coroutine if there are no child objects
        }


        if (childCount == 1)
        {
            for (int i = 2; i < rightDiagonalCoordinates.Count; i++)
            {
                // 1. SYNC CHECK (Stop Logic)
                if (stop != -1 && i > stop)
                {
                    // CASE A: I (Right) caused the stop (ID == 2)
                    if (stopperID == 2)
                    {
                        bool isStillBlocked = false;
                        try
                        {
                            // Check if the blockage at the target position [i] is still there
                            isStillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[i]);
                        }
                        catch { isStillBlocked = false; }

                        if (isStillBlocked)
                        {
                            Debug.Log("Right confirmed blocked. Stopping all.");
                            rightflagRadius(i);
                            rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            enabled = false;
                            yield break;
                        }
                        else
                        {
                            // Path cleared! Reset global variables.
                            Debug.Log("Right block cleared. Resuming.");
                            stop = -1;
                            stopperID = 0;
                        }
                    }
                    // CASE B: Someone ELSE stopped the movement (Left or Vertical)
                    else
                    {
                        Debug.Log("Waiting synchronously for path to clear...");

                        // --- THE FIX STARTS HERE ---
                        // Do NOT yield break. Instead, wait in a loop.
                        while (stop != -1 && stopperID != 2)
                        {
                            // Safety check: If the script gets disabled (permanent stop by owner), quit.
                            if (!enabled)
                            {
                                rightflagRadius(i);
                                rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                yield break;
                            }
                            yield return null; // Wait for next frame
                        }
                        // If we exit this loop, it means stop became -1. We automatically resume!
                        // --- THE FIX ENDS HERE ---
                    }
                }

                // 2. MOVE
                rightChildObject[0].transform.position = rightDiagonalCoordinates[i];

                // 3. DETECTION (Look Ahead i+1)
                try
                {
                    if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[i + 1]))
                    {
                        if (stop == -1)
                        {
                            stop = i;
                            stopperID = 2; // Set as RIGHT stopper
                        }
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    // Check if object reached final position (End of List)
                    if (rightChildObject[0].transform.position == rightDiagonalCoordinates[rightDiagonalCoordinates.Count - 1])
                    {
                        rightflagRadius(i + 1);
                        rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                    }
                    yield break;
                }

                yield return new WaitForSeconds(moveSpeed);
            }
        }




    }


    IEnumerator moveVertical(Transform child, int childCount)
    {
        if (verticalChildObject == null || verticalChildObject.Count == 0)
        {
            yield break; // Exit the coroutine if there are no child objects
        }




        if (childCount == 2)
        {
            for (int i = 2; i < verticalCoordinates.Count; i++)
            {
                if (swipeInput != null)
                {
                    swipeInput.canSwipeDown = true;
                }
                // -------------------------------------------------------------
                // 1. DOUBLE CHECK LOGIC (With Identity Check ID = 2)
                // -------------------------------------------------------------
                if (stop != -1 && i > stop)
                {
                    // CASE A: I (Vertical) caused the stop (ID == 2)
                    if (stopperID == 3)
                    {
                        bool isStillBlocked = false;
                        try
                        {
                            // Check CURRENT position [i] (Movement Target)
                            isStillBlocked = gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, verticalCoordinates[i]);
                        }
                        catch { isStillBlocked = false; }

                        if (isStillBlocked)
                        {
                            Debug.Log("Vertical confirmed blocked. Stopping all.");
                            verticalflagRadius(i);
                            // PARENT BOTH OBJECTS
                            verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                            if (swipeInput != null) swipeInput.canSwipeDown = true;
                            enabled = false;
                            yield break;
                        }
                        else
                        {
                            // Path cleared! Reset global variables.
                            Debug.Log("Vertical block cleared. Resuming.");
                            stop = -1;
                            stopperID = 0;
                        }
                    }
                    // CASE B: Someone else stopped it
                    else
                    {
                        Debug.Log("Vertical Waiting synchronously for path to clear...");

                        // --- WAITING LOGIC ---
                        while (stop != -1 && stopperID != 3)
                        {
                            if (!enabled)
                            {
                                verticalflagRadius(i);
                                // PARENT BOTH OBJECTS
                                verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                                if (swipeInput != null) swipeInput.canSwipeDown = true;
                                yield break;
                            }
                            yield return null; // Wait frame by frame
                        }
                        // Resume automatically
                    }
                }

                // -------------------------------------------------------------
                // 2. MOVE (Move Both Objects)
                // -------------------------------------------------------------
                verticalChildObject[0].transform.position = verticalCoordinates[i];
                verticalChildObject[1].transform.position = verticalCoordinates[i - 1];

                if (CheckAndAssignDimension(i))
                {
                    if (swipeInput != null)
                    {
                        swipeInput.canSwipeDown = false;
                    }
                }

                // -------------------------------------------------------------
                // 3. DETECTION (Look Ahead i+1)
                // -------------------------------------------------------------
                try
                {
                    if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, verticalCoordinates[i + 1]))
                    {
                        if (stop == -1)
                        {
                            stop = i;
                            stopperID = 3; // MARK AS VERTICAL STOP
                        }
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    // Check if both reached their final positions
                    if (verticalChildObject[0].transform.position == verticalCoordinates[verticalCoordinates.Count - 1] &&
                        verticalChildObject[1].transform.position == verticalCoordinates[verticalCoordinates.Count - 2])
                    {
                        verticalflagRadius(i + 1);
                        // PARENT BOTH OBJECTS
                        verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                        if (swipeInput != null) swipeInput.canSwipeDown = true;

                        
                            
                    }
                    yield break;
                }
                yield return new WaitForSeconds(moveSpeed);
            }
        }


    }


    void countChildren()
    {
        // leftChildObject.Clear();
        // rightChildObject.Clear();
        // verticalChildObject.Clear();


        // so that counts are reset each time function is called
        leftDiagonalCount = 0;
        rightDiagonalCount = 0;
        verticalCount = 0;
        //left diagonal count of children

        foreach (Transform child in transform)
        {
            float worldX = child.position.x; // WORLD position  
            if (worldX < 0f)
            {
                leftDiagonalCount++;
                //add left child object to list
                leftChildObject.Add(child.gameObject);
            }
        }
        Debug.Log($"Number of children on the left diagonal: {leftDiagonalCount}");

        //right diagonal count of children

        foreach (Transform child in transform)
        {
            float worldX = child.position.x; // WORLD position  
            if (worldX > 0f)
            {
                rightDiagonalCount++;
                //add right child object to list
                rightChildObject.Add(child.gameObject);
            }
        }
        Debug.Log($"Number of children on the right diagonal: {rightDiagonalCount}");

        //vertical count of children

        foreach (Transform child in transform)
        {
            float worldX = child.position.x; // WORLD position  
            if (worldX == 0f)
            {
                verticalCount++;
                //add vertical child object to list
                verticalChildObject.Add(child.gameObject);
            }
        }
        Debug.Log($"Number of children on the vertical: {verticalCount}");
    }











    bool CheckAndAssignDimension(int i)
    {
        // Safety Checks
        if (leftChildObject == null || leftChildObject.Count == 0) return false;


        if (i < 0) return false;

        if (swipeInput != null)
        {
            swipeInput.canSwipeDown = true;
        }

        // FIX: Change 'GameObject[][]' to 'List<List<GameObject>>'
        List<List<GameObject>> allDimensions = new List<List<GameObject>>
    {
        gameManager.plusXDimension,
        gameManager.plusYDimension,
        gameManager.plusZDimension,
        gameManager.minusXDimension,
        gameManager.minusYDimension,
        gameManager.minusZDimension,

        gameManager.plusYplusZDimension,
        gameManager.plusYminusZDimension,
        gameManager.minusYplusZDimension,
        gameManager.minusYminusZDimension,

        gameManager.minusXminusZDimension,
        gameManager.minusXplusZDimension,
        gameManager.plusXminusZDimension,
        gameManager.plusXplusZDimension,

        gameManager.minusXplusYDimension,
        gameManager.plusXplusYDimension,
        gameManager.minusXminusYDimension,
        gameManager.plusXminusYDimension
    };

        // Optional: Matching names for Debugging
        string[] dimNames = {
        "plusX", "plusY", "plusZ", "minusX", "minusY", "minusZ",
        "plusYplusZ", "plusYminusZ", "minusYplusZ", "minusYminusZ",
        "minusXminusZ", "minusXplusZ", "plusXminusZ", "plusXplusZ",
        "minusXplusY", "plusXplusY", "minusXminusY", "plusXminusY"
    };

        // Iterate through the lists
        for (int d = 0; d < allDimensions.Count; d++)
        {
            // Safety: Ensure the specific List is actually big enough to have this index
            if (i < allDimensions[d].Count)
            {
                if (allDimensions[d][i] != null && allDimensions[d][i].transform.position.z > 0f && allDimensions[d][i].transform.position.z > 0f)
                {
                    Debug.Log(allDimensions[d][i].transform.position);
                    return true;



                }
            }
        }

        return false;
    }


}