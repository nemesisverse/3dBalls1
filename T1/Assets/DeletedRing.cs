using UnityEngine;

// there should be no children
public class DeletedRing : MonoBehaviour
{
    void Update()
    {
        if (transform.childCount == 0) return;

        foreach (Transform child in transform)
            Destroy(child.gameObject);
    }
}