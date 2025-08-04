using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoldToRotateButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Button rotateButton;           // Drag your circular button here
    public Transform pedestal;            // Drag your pedestal GameObject here
    public int snapPoints = 12;           // 12 snap points = 30 degrees per snap
    public float snapInterval = 0.2f;     // Time delay between snaps (in seconds)

    private bool isHolding = false;
    private float snapTimer = 0f;
    private float snapAngle;

    void Start()
    {
        if (rotateButton == null || pedestal == null)
        {
            Debug.LogError("Assign Rotate Button and Pedestal in Inspector.");
            enabled = false;
            return;
        }

        snapAngle = 360f / snapPoints; // e.g., 30°
    }

    void Update()
    {
        if (isHolding)
        {
            snapTimer += Time.deltaTime;

            if (snapTimer >= snapInterval)
            {
                // Rotate globally around X axis
                pedestal.Rotate(Vector3.right, snapAngle, Space.World);
                snapTimer = 0f;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.pointerEnter == rotateButton.gameObject)
        {
            isHolding = true;
            snapTimer = snapInterval; // Snap immediately on press
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        snapTimer = 0f;
    }
}
