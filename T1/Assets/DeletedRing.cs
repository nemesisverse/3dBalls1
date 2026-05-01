using UnityEngine;

// Destroys all children each frame.
// GameManager.Update() polls childCount to know when deletion is done.
public class DeletedRing : MonoBehaviour
{
    void Update()
    {
        if (transform.childCount == 0) return;

        foreach (Transform child in transform)
            Destroy(child.gameObject);
    }
}