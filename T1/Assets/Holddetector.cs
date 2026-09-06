using UnityEngine;
using UnityEngine.InputSystem;

// ================================================================
//  HoldDetector — singleton that tracks whether the player is
//  currently holding their finger on the screen long enough to
//  trigger fast-fall.
//
//  ► Drop this MonoBehaviour on any persistent GameObject in the
//    scene (e.g. GameManager, a dedicated "InputManager" object).
//
//  ► Movement coroutines query:
//      HoldDetector.Instance.isHolding
//    to switch between normal speed and fastMoveSpeed.
//
//  ► Hold is distinguished from Tap automatically by holdThreshold:
//    - Tap:  touch lifts before holdThreshold → isHolding stays false
//              → tap action in SwipeInput fires normally, unaffected
//    - Hold: touch stays down past holdThreshold → isHolding = true
//              → movement coroutines use fastMoveSpeed until release
//
//  ► Uses Touchscreen.current directly so it does NOT depend on
//    any generated Input Action asset class — zero changes needed
//    to TouchControl or SwipeInput.
// ================================================================

public class HoldDetector : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────
    public static HoldDetector Instance { get; private set; }

    // ── Settings ─────────────────────────────────────────────────
    [Tooltip("How long (seconds) the touch must be held before fast-fall activates. " +
             "Taps that lift off before this value never trigger isHolding.")]
    public float holdThreshold = 0.25f;

    // ── State ─────────────────────────────────────────────────────
    /// <summary>
    /// True while the player is holding their finger down past holdThreshold.
    /// Read this from every movement coroutine.
    /// </summary>
    public bool isHolding { get; private set; }

    private float pressStartTime = -1f;
    private bool  wasTouching    = false;

    // ── Unity lifecycle ───────────────────────────────────────────

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        bool isTouching = false;

        // Read primary touch state from Unity's new Input System.
        // Falls back gracefully to false when no touchscreen is present
        // (e.g. in the Editor without touch simulation enabled).
        var ts = Touchscreen.current;
        if (ts != null)
            isTouching = ts.primaryTouch.press.isPressed;

        // Joystick owns the touch — never fast-fall while orbiting the camera.
        // Clears any hold already in progress the instant the joystick is grabbed.
        if (JoystickTouchGate.Active)
        {
            isHolding      = false;
            pressStartTime = -1f;
            wasTouching    = isTouching;
            return;
        }

        // ── Finger just landed ────────────────────────────────────
        if (isTouching && !wasTouching)
        {
            pressStartTime = Time.time;
            isHolding      = false;
        }
        // ── Finger just lifted ────────────────────────────────────
        else if (!isTouching && wasTouching)
        {
            pressStartTime = -1f;
            isHolding      = false;
        }

        // ── Promote to hold once threshold is exceeded ────────────
        // Active on genuine play-area holds; suppressed while the
        // joystick is held (handled by the early return above).
        if (isTouching && !isHolding && pressStartTime >= 0f)
        {
            if (Time.time - pressStartTime >= holdThreshold)
                isHolding = true;
        }

        wasTouching = isTouching;
    }
}