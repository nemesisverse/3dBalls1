using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System;

public class TapInput : MonoBehaviour
{
    private TouchControl _controls;

    public static event Action<Vector2> OnTap;

    void Awake()
    {
        _controls = new TouchControl();

        _controls.Touch.Tap.performed += ctx =>
        {
            Vector2 tapPosition = _controls.Touch.Position.ReadValue<Vector2>();

            if (EventSystem.current.IsPointerOverGameObject()) return;

            OnTap?.Invoke(tapPosition);
            Debug.Log($"Tap detected at: {tapPosition}");
        };
    }

    void OnEnable()  => _controls.Enable();
    void OnDisable() => _controls.Disable();
    void OnDestroy() => _controls.Dispose();
}