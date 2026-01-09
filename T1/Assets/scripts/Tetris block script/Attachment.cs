using UnityEngine;

public class Attachment : MonoBehaviour
{
    public Transform mother;
    void SetParent(Transform newParent)
    {
        transform.SetParent(newParent);
    }
}
