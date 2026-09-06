using UnityEngine;
using UnityEngine.EventSystems;

// Attach to the SAME GameObject as your Fixed Joystick (the one with the raycast-target Image).
// uGUI dispatches pointer events to every handler on the pressed object, so this coexists
// with the joystick's own pointer handling without touching the Joystick Pack scripts.
public class JoystickTouchGate : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public static bool Active { get; private set; }

    public void OnPointerDown(PointerEventData eventData) => Active = true;
    public void OnPointerUp(PointerEventData eventData) => Active = false;

    void OnDisable() => Active = false; // safety on scene change / focus loss
}