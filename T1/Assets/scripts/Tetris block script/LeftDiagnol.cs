using System.Collections;
using System.Threading;
using UnityEngine;

public class LeftDiagnol : MonoBehaviour
{
    int leftDiagonalCount = 0;
     int rightDiagonalCount = 0;
     int verticalCount = 0;
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
            if (worldX < 0f && leftDiagonalCount == 1)
            {
                Debug.Log($"{child.name} is on the LEFT side (world X < 0): {worldX}");

                //iterate through some specific provided coordinates for left diognal
                StartCoroutine(moveLeftDiognal(child));

            }
            ////////////center///////////////
            else if(worldX == 0f && verticalCount == 1)
            {
                Debug.Log($"{child.name} is at the CENTER (world X == 0): {worldX}");
                StartCoroutine(moveVertical(child));
            }


            ////////////right///////////////
            else if(worldX > 0f && rightDiagonalCount == 1)
            {
                Debug.Log($"{child.name} is on the RIGHT side (world X >= 0): {worldX}");
                StartCoroutine(moveRightDiognal(child));
            }
        }

    }

    IEnumerator moveLeftDiognal(Transform child)
    {
        // left diognal running this loop after every 2 seconds
        for (float v = 8.837f; v >= 1.767f - 0.0001f; v -= 0.707f)
        {
            child.position = new Vector3(-v, v, 0f);
            yield return new WaitForSeconds(2f);
        }
        
    }

        IEnumerator moveRightDiognal(Transform child)
    {
        // right diognal running this loop after every 2 seconds
        for (float v = 8.837f; v >= 1.767f - 0.0001f; v -= 0.707f)
        {
            child.position = new Vector3(v, v, 0f);
            yield return new WaitForSeconds(2f);
        }
        
    }

    IEnumerator moveVertical(Transform child)
    {
    // vertical column running this loop after every 2 seconds
        for (float v = 12.5f; v >= 2.5f; v -= 1f)
        {
            child.position = new Vector3(0f, v, 0f);
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