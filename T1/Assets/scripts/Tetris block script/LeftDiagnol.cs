using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class LeftDiagnol : MonoBehaviour
{
    int leftDiagonalCount = 0;
     int rightDiagonalCount = 0;
     int verticalCount = 0;

    List<Vector3> leftDiagonalCoordinates = new List<Vector3>();
    List<Vector3> rightDiagonalCoordinates = new List<Vector3>();
    List<Vector3> verticalCoordinates = new List<Vector3>();

    //list of predefine coordinates for left diagonal, right diagonal and vertical and its working fine
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

    void CheckChildrenWorldX()
    {
        //this for loop is iterationg through every child of the current game object's world position
        //Transform is a data type and transform is a collection variable that holds all the children of the current game object
        foreach (Transform child in transform)
        {
            float worldX = child.position.x; // WORLD position
            ////////////left///////////////
            if (worldX < 0f )
            {
                Debug.Log($"{child.name} is on the LEFT side (world X < 0): {worldX}");

                //iterate through some specific provided coordinates for left diognal
                StartCoroutine(moveLeftDiognal(child, leftDiagonalCount));

            }
            ////////////center///////////////
            else if(worldX == 0f)
            {
                Debug.Log($"{child.name} is at the CENTER (world X == 0): {worldX}");
                StartCoroutine(moveVertical(child, verticalCount));
            }


            ////////////right///////////////
            else if(worldX > 0f)
            {
                Debug.Log($"{child.name} is on the RIGHT side (world X >= 0): {worldX}");
                StartCoroutine(moveRightDiognal(child, rightDiagonalCount));
            }
        }

    }

    IEnumerator moveLeftDiognal(Transform child, int childCount)
    {
        for (int i = 0; i < leftDiagonalCoordinates.Count; i++)
        {
            child.position = leftDiagonalCoordinates[i];
            Debug.Log($"child {child.position} list {leftDiagonalCoordinates[i]}");
            yield return new WaitForSeconds(2f);
        }
    }


    IEnumerator moveRightDiognal(Transform child, int childCount)
    {
       for (int i = 0; i < rightDiagonalCoordinates.Count; i++)
       {
           child.position = rightDiagonalCoordinates[i];
           yield return new WaitForSeconds(2f);
       }
    } 


    IEnumerator moveVertical(Transform child, int childCount)
    {
        for (int i = 0; i < verticalCoordinates.Count; i++)
        {
            child.position = verticalCoordinates[i];
            yield return new WaitForSeconds(2f);
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
            }
        }
        Debug.Log($"Number of children on the vertical: {verticalCount}");
    }
}