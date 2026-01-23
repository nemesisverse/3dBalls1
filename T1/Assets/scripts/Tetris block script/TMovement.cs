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
    float moveSpeed = 1f;

    List<Vector3> leftDiagonalCoordinates = new List<Vector3>();
    List<Vector3> rightDiagonalCoordinates = new List<Vector3>();
    List<Vector3> verticalCoordinates = new List<Vector3>();


    List<GameObject> leftChildObject = new List<GameObject>();
    List<GameObject> rightChildObject = new List<GameObject>();
    List<GameObject> verticalChildObject = new List<GameObject>();


    public GameManager gameManager;

    void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            // Note: use FindObjectOfType<GameManager>() if on older Unity versions
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


    void CheckChildrenWorldX()
    {
        //this for loop is iterationg through every child of the current game object's world position
        //Transform is a data type and transform is a collection variable that holds all the children of the current game object
        foreach (Transform child in transform)
        {
            float worldX = child.position.x; // WORLD position
            ////////////left///////////////
            if (worldX < 0f)
            {
                Debug.Log($"{child.name} is on the LEFT side (world X < 0): {worldX}");

                //iterate through some specific provided coordinates for left diognal
                StartCoroutine(moveLeftDiognal(child, leftDiagonalCount));


            }
            ////////////center///////////////
            else if (worldX == 0f)
            {
                Debug.Log($"{child.name} is at the CENTER (world X == 0): {worldX}");
                StartCoroutine(moveVertical(child, verticalCount));
            }


            ////////////right///////////////
            else if (worldX > 0f)
            {
                Debug.Log($"{child.name} is on the RIGHT side (world X >= 0): {worldX}");
                StartCoroutine(moveRightDiognal(child, rightDiagonalCount));
            }
        }

    }

    //bool stopNextIteration = false;
    //baksodi comm



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

                            if(gameManager.plusXDimension[i-1] == null)
                            {
                                gameManager.plusXDimension[i-1] = leftChildObject[0];
                                Debug.Log("Left block added to plusXDimension at index " + (i-1));
                            }

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
                                if(gameManager.plusXDimension[i-1] == null)
                            {
                                gameManager.plusXDimension[i-1] = leftChildObject[0];
                                Debug.Log("Left block added to plusXDimension at index " + (i-1));
                            }

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

                            // PARENT BOTH OBJECTS
                            verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
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

                                // PARENT BOTH OBJECTS
                                verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
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

                        // PARENT BOTH OBJECTS
                        verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
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
}