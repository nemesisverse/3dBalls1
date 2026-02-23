using System.Collections;
using System.Collections.Generic;
using Unity.Android.Gradle;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
public class T1Movement : MonoBehaviour
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
    // Start is called once before the first execution of Update after the MonoBehaviour is created


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

     int stop = -1;
    int stopperID = 0;


IEnumerator moveRightDiognal(Transform child, int childCount)
    {
        if (rightChildObject == null || rightChildObject.Count == 0) yield break;
        if (childCount == 1)
        {
            for (int i = 2; i < rightDiagonalCoordinates.Count; i++)
            {
               // ResetSwipePermissions();
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
                           //rightflagRadius(i - 1);
                            rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            //ResetSwipePermissions();
                            // ResetSliderPermissions(); // Enable slider when done
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
                                //rightflagRadius(i - 1);
                                rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                //ResetSwipePermissions();
                                // ResetSliderPermissions();
                                yield break;
                            }
                            yield return null;
                        }
                    }
                }

                rightChildObject[0].transform.position = rightDiagonalCoordinates[i-1];
                //rightRotationStopper(i); // check for swipe permissions at new position

                try { if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, rightDiagonalCoordinates[i + 1])) { if (stop == -1) { stop = i; stopperID = 2; } } }
                catch (System.ArgumentOutOfRangeException)
                {
                    if (rightChildObject[0].transform.position == rightDiagonalCoordinates[rightDiagonalCoordinates.Count - 2])
                    {
                        //rightflagRadius(i);
                        rightChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        //ResetSwipePermissions();
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
        if (childCount == 3)
        {
            for (int i = 2; i < verticalCoordinates.Count; i++)
            {
                 //ResetSwipePermissions();
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
                            //verticalflagRadius(i - 1);
                            verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                            verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                            verticalChildObject[2].transform.SetParent(gameManager.motherPlatform.transform, true);

                            //ResetSwipePermissions();
                            //ResetSliderPermissions(); // Enable slider when done
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
                                //verticalflagRadius(i - 1);
                                verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                                verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                                verticalChildObject[2].transform.SetParent(gameManager.motherPlatform.transform, true);

                                //ResetSwipePermissions();
                                //ResetSliderPermissions();
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
                verticalChildObject[2].transform.position = verticalCoordinates[i - 2];
                //verticalRotationStopper(i); // check for swipe permissions at new position

                // --- YOUR LOGIC: Check & Lock Slider Directions ---

                // --------------------------------------------------

                try { if (gameManager.HasChildAtPosition(gameManager.motherPlatform.transform, verticalCoordinates[i + 1])) { if (stop == -1) { stop = i; stopperID = 3; } } }
                catch (System.ArgumentOutOfRangeException)
                {
                    if (verticalChildObject[0].transform.position == verticalCoordinates[verticalCoordinates.Count - 1] &&
                        verticalChildObject[1].transform.position == verticalCoordinates[verticalCoordinates.Count - 2] &&
                        verticalChildObject[2].transform.position == verticalCoordinates[verticalCoordinates.Count - 3])
                    {
                        //verticalflagRadius(i);
                        verticalChildObject[0].transform.SetParent(gameManager.motherPlatform.transform, true);
                        verticalChildObject[1].transform.SetParent(gameManager.motherPlatform.transform, true);
                        verticalChildObject[2].transform.SetParent(gameManager.motherPlatform.transform, true);
                        //ResetSwipePermissions();
                        //ResetSliderPermissions(); // Enable slider when done
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

    // Update is called once per frame
    void countChildren()
    {
        leftDiagonalCount = 0; rightDiagonalCount = 0; verticalCount = 0;
        foreach (Transform child in transform) { if (child.position.x < 0f) { leftDiagonalCount++; leftChildObject.Add(child.gameObject); } }
        foreach (Transform child in transform) { if (child.position.x > 0f) { rightDiagonalCount++; rightChildObject.Add(child.gameObject); } }
        foreach (Transform child in transform) { if (child.position.x == 0f) { verticalCount++; verticalChildObject.Add(child.gameObject); } }

        // Debug.Log(leftDiagonalCount);
        // Debug.Log(rightDiagonalCount);
        // Debug.Log(verticalCount);
    }

        void CheckChildrenWorldX()
    {
        bool leftStarted = false, rightStarted = false, verticalStarted = false;
        foreach (Transform child in transform)
        {
            float worldX = child.position.x;
            //if (worldX < 0f && !leftStarted) { StartCoroutine(moveLeftDiognal(child, leftDiagonalCount)); leftStarted = true; }
            
             if (worldX > 0f && !rightStarted) { StartCoroutine(moveRightDiognal(child, rightDiagonalCount)); rightStarted = true; }
             else if (worldX == 0f && !verticalStarted) { StartCoroutine(moveVertical(child, verticalCount)); verticalStarted = true; }
        }
    }



}

