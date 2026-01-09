using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class TMovement : MonoBehaviour
{
    int leftDiagonalCount = 0;
    int rightDiagonalCount = 0;
    int verticalCount = 0;

    List<Vector3> leftDiagonalCoordinates = new List<Vector3>();
    List<Vector3> rightDiagonalCoordinates = new List<Vector3>();
    List<Vector3> verticalCoordinates = new List<Vector3>();


    List<GameObject> leftChildObject = new List<GameObject>();
    List<GameObject> rightChildObject = new List<GameObject>();
    List<GameObject> verticalChildObject = new List<GameObject>();

    
    //list of predefine coordinates for left diagonal, right diagonal and vertical and its working fine

    // void MakeChildrenAttachedToMother()
    // {
    //     foreach (Transform child in transform)
    //     {
    //         child.SetParent(mother, true);
    //     }
    // }


    public Transform mother;
    void Awake()
    {
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


    bool IsBlockAtPosition(Vector3 position)
    {
        float pointRadius = 0.51f;

        Collider[] hits = Physics.OverlapSphere(position, pointRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("block"))
                return true;
        }

        return false;
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

     

    IEnumerator moveLeftDiognal(Transform child, int childCount)
    {
        if (leftChildObject == null || leftChildObject.Count == 0)
        {
            yield break; // Exit the coroutine if there are no child objects
        }


        bool stopNextIteration = false;

        if (childCount == 1)
        {
            for (int i = 2; i < leftDiagonalCoordinates.Count; i++)
            {
                leftChildObject[0].transform.position = leftDiagonalCoordinates[i];

                // If previous iteration detected a block → stop now
                if (stopNextIteration)
                {
                    Debug.Log("Stopping movement after one extra step.");

                   
                    yield break;
                }

                try
                {
                    if (
                        IsBlockAtPosition(leftDiagonalCoordinates[i + 1]))
                    {
                        Debug.Log($"Block detected near position {leftDiagonalCoordinates[i + 1]}.");

                        // Do NOT stop immediately

                        stopNextIteration = true;
                      
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    
                    yield break;
                }

                Debug.Log($"child {child.position} list {leftDiagonalCoordinates[i]}");

                if (leftChildObject[0].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 1])
                {
                    leftChildObject[0].transform.SetParent(mother.transform, true);

                }
                yield return new WaitForSeconds(2f);
            }
        }

        if (childCount == 2)
        {
            for (int i = 2; i < leftDiagonalCoordinates.Count; i++)
            {
                leftChildObject[0].transform.position = leftDiagonalCoordinates[i];
                leftChildObject[1].transform.position = leftDiagonalCoordinates[i - 1];

            

                // If previous iteration detected a block → stop now
                if (stopNextIteration)
                {
                    Debug.Log("Stopping movement after one extra step.");

                   
                    yield break;
                }

                try
                {
                    if (
                        IsBlockAtPosition(leftDiagonalCoordinates[i + 1]))
                    {
                        Debug.Log($"Block detected near position {leftDiagonalCoordinates[i + 1]}.");

                        // Do NOT stop immediately
                        stopNextIteration = true;
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    yield break;
                }


                Debug.Log($"child {child.position} list {leftDiagonalCoordinates[i]}");
                if (leftChildObject[0].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 1] && leftChildObject[1].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 2])
                {
                    leftChildObject[0].transform.SetParent(mother.transform, true);
                    leftChildObject[1].transform.SetParent(mother.transform, true);

                }
                yield return new WaitForSeconds(2f);
            }

        }

        if (childCount == 3)
        {
            for (int i = 2; i < leftDiagonalCoordinates.Count; i++)
            {
                leftChildObject[0].transform.position = leftDiagonalCoordinates[i];
                leftChildObject[1].transform.position = leftDiagonalCoordinates[i - 1];
                leftChildObject[2].transform.position = leftDiagonalCoordinates[i - 2];

                // If previous iteration detected a block → stop now
                if (stopNextIteration)
                {
                    Debug.Log("Stopping movement after one extra step.");

                    
                    yield break;
                }

                try
                {
                    if (
                        IsBlockAtPosition(leftDiagonalCoordinates[i + 1]))
                    {
                        Debug.Log($"Block detected near position {leftDiagonalCoordinates[i + 1]}.");

                        // Do NOT stop immediately
                        stopNextIteration = true;
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    yield break;
                }


                Debug.Log($"child {child.position} list {leftDiagonalCoordinates[i]}");
                if (leftChildObject[0].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 1] && leftChildObject[1].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 2] && leftChildObject[2].transform.position == leftDiagonalCoordinates[leftDiagonalCoordinates.Count - 3])
                {
                    leftChildObject[0].transform.SetParent(mother.transform, true);
                    leftChildObject[1].transform.SetParent(mother.transform, true);
                    leftChildObject[2].transform.SetParent(mother.transform, true);

                }
                yield return new WaitForSeconds(2f);
            }

        }

    }


    IEnumerator moveRightDiognal(Transform child, int childCount)
    {
        if (rightChildObject == null || rightChildObject.Count == 0)
        {
            yield break; // Exit the coroutine if there are no child objects
        }
    
    bool stopNextIterationR = false;
        if (childCount == 1)
        {
            for (int i = 2; i < rightDiagonalCoordinates.Count; i++)
            {
                rightChildObject[0].transform.position = rightDiagonalCoordinates[i];

                // If previous iteration detected a block → stop now
                if (stopNextIterationR)
                {
                    Debug.Log("Stopping movement after one extra step.");

                   
                    yield break;
                }

                try
                {
                    if (
                        IsBlockAtPosition(rightDiagonalCoordinates[i + 1]))
                    {
                        Debug.Log($"Block detected near position {rightDiagonalCoordinates[i + 1]}.");

                        // Do NOT stop immediately
                        stopNextIterationR = true;
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    yield break;
                }

                if (rightChildObject[0].transform.position == rightDiagonalCoordinates[rightDiagonalCoordinates.Count - 1])
                {
                    rightChildObject[0].transform.SetParent(mother.transform, true);

                }
                yield return new WaitForSeconds(2f);
            }
        }
        if (childCount == 2)
        {
            for (int i = 2; i < rightDiagonalCoordinates.Count; i++)
            {
                rightChildObject[0].transform.position = rightDiagonalCoordinates[i];
                rightChildObject[1].transform.position = rightDiagonalCoordinates[i - 1];

                // If previous iteration detected a block → stop now
                if (stopNextIterationR)
                {
                    Debug.Log("Stopping movement after one extra step.");

                
                    yield break;
                }

                try
                {
                    if (
                        IsBlockAtPosition(rightDiagonalCoordinates[i + 1]))
                    {
                        Debug.Log($"Block detected near position {rightDiagonalCoordinates[i + 1]}.");

                        // Do NOT stop immediately
                        stopNextIterationR = true;
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    yield break;
                }

                if (rightChildObject[0].transform.position == rightDiagonalCoordinates[rightDiagonalCoordinates.Count - 1] && rightChildObject[1].transform.position == rightDiagonalCoordinates[rightDiagonalCoordinates.Count - 2])
                {
                    rightChildObject[0].transform.SetParent(mother.transform, true);
                    rightChildObject[1].transform.SetParent(mother.transform, true);

                }
                yield return new WaitForSeconds(2f);
            }
        }
        if (childCount == 3)
        {
            for (int i = 2; i < rightDiagonalCoordinates.Count; i++)
            {
                rightChildObject[0].transform.position = rightDiagonalCoordinates[i];
                rightChildObject[1].transform.position = rightDiagonalCoordinates[i - 1];
                rightChildObject[2].transform.position = rightDiagonalCoordinates[i - 2];

                // If previous iteration detected a block → stop now
                if (stopNextIterationR)
                {
                    Debug.Log("Stopping movement after one extra step.");

                    yield break;
                }

                try
                {
                    if (
                        IsBlockAtPosition(rightDiagonalCoordinates[i + 1]))
                    {
                        Debug.Log($"Block detected near position {rightDiagonalCoordinates[i + 1]}.");

                        // Do NOT stop immediately
                        stopNextIterationR = true;
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    yield break;
                }

                if (rightChildObject[0].transform.position == rightDiagonalCoordinates[rightDiagonalCoordinates.Count - 1] && rightChildObject[1].transform.position == rightDiagonalCoordinates[rightDiagonalCoordinates.Count - 2] && rightChildObject[2].transform.position == rightDiagonalCoordinates[rightDiagonalCoordinates.Count - 3])
                {
                    rightChildObject[0].transform.SetParent(mother.transform, true);
                    rightChildObject[1].transform.SetParent(mother.transform, true);
                    rightChildObject[2].transform.SetParent(mother.transform, true);

                }
                yield return new WaitForSeconds(2f);
            }
        }

    }


    IEnumerator moveVertical(Transform child, int childCount)
    {
        if (verticalChildObject == null || verticalChildObject.Count == 0)
        {
            yield break; // Exit the coroutine if there are no child objects
        }

        bool stopNextIterationV = false;
       
        if (childCount == 1)
        {
            for (int i = 2; i < verticalCoordinates.Count; i++)
            {
                verticalChildObject[0].transform.position = verticalCoordinates[i];

                // If previous iteration detected a block → stop now
                if (stopNextIterationV)
                {
                    Debug.Log("Stopping movement after one extra step.");

                   
                    yield break;
                }

                try
                {
                    if (
                        IsBlockAtPosition(verticalCoordinates[i + 1]))
                    {
                        Debug.Log($"Block detected near position {verticalCoordinates[i + 1]}.");

                        // Do NOT stop immediately
                        stopNextIterationV = true;
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    yield break;
                }


                if (verticalChildObject[0].transform.position == verticalCoordinates[verticalCoordinates.Count - 1])
                {
                    verticalChildObject[0].transform.SetParent(mother.transform, true);

                }
                yield return new WaitForSeconds(2f);
            }
        }

        if (childCount == 2)
        {
            for (int i = 2; i < verticalCoordinates.Count; i++)
            {
                verticalChildObject[0].transform.position = verticalCoordinates[i];
                verticalChildObject[1].transform.position = verticalCoordinates[i - 1];

                // If previous iteration detected a block → stop now
                if (stopNextIterationV)
                {
                    Debug.Log("Stopping movement after one extra step.");

                    
                    yield break;
                }

                try
                {
                    if (
                        IsBlockAtPosition(verticalCoordinates[i + 1]))
                    {
                        Debug.Log($"Block detected near position {verticalCoordinates[i + 1]}.");

                        // Do NOT stop immediately
                        stopNextIterationV = true;
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    yield break;
                }

                if (verticalChildObject[0].transform.position == verticalCoordinates[verticalCoordinates.Count - 1] && verticalChildObject[1].transform.position == verticalCoordinates[verticalCoordinates.Count - 2])
                {
                    verticalChildObject[0].transform.SetParent(mother.transform, true);
                    verticalChildObject[1].transform.SetParent(mother.transform, true);

                }
                yield return new WaitForSeconds(2f);
            }
        }

        if (childCount == 3)
        {
            for (int i = 2; i < verticalCoordinates.Count; i++)
            {
                verticalChildObject[0].transform.position = verticalCoordinates[i];
                verticalChildObject[1].transform.position = verticalCoordinates[i - 1];
                verticalChildObject[2].transform.position = verticalCoordinates[i - 2];

                // If previous iteration detected a block → stop now
                if (stopNextIterationV)
                {
                    Debug.Log("Stopping movement after one extra step.");

                 
                    yield break;
                }

                try
                {
                    if (
                        IsBlockAtPosition(verticalCoordinates[i + 1]))
                    {
                        Debug.Log($"Block detected near position {verticalCoordinates[i + 1]}.");

                        // Do NOT stop immediately
                        stopNextIterationV = true;
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    yield break;
                }


                if (verticalChildObject[0].transform.position == verticalCoordinates[verticalCoordinates.Count - 1] && verticalChildObject[1].transform.position == verticalCoordinates[verticalCoordinates.Count - 2] && verticalChildObject[2].transform.position == verticalCoordinates[verticalCoordinates.Count - 3])
                {
                    verticalChildObject[0].transform.SetParent(mother.transform, true);
                    verticalChildObject[1].transform.SetParent(mother.transform, true);
                    verticalChildObject[2].transform.SetParent(mother.transform, true);

                }
                yield return new WaitForSeconds(2f);
            }
        }

    }


    void countChildren()
    {
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