using UnityEngine;

public class LeftDiagnol : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CheckChildrenWorldX();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void CheckChildrenWorldX()
    {
        //this for loop is iterationg through every child of the current game object's world position
        foreach (Transform child in transform)
        {
            float worldX = child.position.x; // WORLD position

            if (worldX < 0f)
            {
                Debug.Log($"{child.name} is on the LEFT side (world X < 0): {worldX}");
            }
            else if(worldX == 0f)
            {
                Debug.Log($"{child.name} is at the CENTER (world X == 0): {worldX}");
            }
            else
            {
                Debug.Log($"{child.name} is on the RIGHT side (world X >= 0): {worldX}");
            }
        }
    }
}
