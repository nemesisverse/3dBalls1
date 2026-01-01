using UnityEngine;

public class CoordinatesIterator : MonoBehaviour
{
void IterateCoordinates()
{
    // ---------- 1. Vertical Column (Reversed) ----------
    for (float y = 12.5f; y >= 2.5f; y -= 1f)
    {
        Debug.Log(new Vector3(0f, y, 0f));
    }

    // ---------- 2. Right Diagonal (Reversed) ----------
    for (float v = 8.837f; v >= 1.767f - 0.0001f; v -= 0.707f)
    {
        Debug.Log(new Vector3(v, v, 0f));
    }

    // ---------- 3. Left Diagonal (Reversed) ----------
    for (float v = 8.837f; v >= 1.767f - 0.0001f; v -= 0.707f)
    {
        Debug.Log(new Vector3(-v, v, 0f));
    }
}

}
