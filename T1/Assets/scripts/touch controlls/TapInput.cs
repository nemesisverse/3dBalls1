using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System;

public class TapInput : MonoBehaviour
{
    private TouchControl _controls;
    private bool _tapPerformed;
    private Vector2 _tapPosition;

    public static event Action<Vector2> OnTap;

    void Awake()
    {
        _controls = new TouchControl();

        _controls.Touch.Tap.performed += ctx =>
        {
            _tapPosition = _controls.Touch.Position.ReadValue<Vector2>();
            _tapPerformed = true;
        };
    }

    void Update()
    {
        if (!_tapPerformed) return;
        _tapPerformed = false;

        if (Time.timeScale == 0f) return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        OnTap?.Invoke(_tapPosition);
        Debug.Log($"Tap detected at: {_tapPosition}");
    }

    void OnEnable()  => _controls.Enable();
    void OnDisable() => _controls.Disable();
    void OnDestroy() => _controls.Dispose();
}