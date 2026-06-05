using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System;

public class TapInput : MonoBehaviour
{
    private TouchControl _controls;

    public static event Action<Vector2> OnTap;

    // deferred tap state
    private bool    _tapPending   = false;
    private Vector2 _tapScreenPos = Vector2.zero;

    void Awake()
    {
        _controls = new TouchControl();

        _controls.Touch.Tap.performed += ctx =>
        {
            _tapScreenPos = _controls.Touch.Position.ReadValue<Vector2>();
            _tapPending   = true;
        };
    }

    void Update()
    {
        if (!_tapPending) return;
        _tapPending = false;

        if (EventSystem.current.IsPointerOverGameObject()) return;

        OnTap?.Invoke(_tapScreenPos);
        Debug.Log($"Tap detected at: {_tapScreenPos}");
    }

    void OnEnable()  => _controls.Enable();
    void OnDisable() => _controls.Disable();
    void OnDestroy() => _controls.Dispose();
}