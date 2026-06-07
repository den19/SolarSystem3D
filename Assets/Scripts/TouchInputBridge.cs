using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using TouchPhase = UnityEngine.TouchPhase;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Unified touch input: Enhanced Touch (Input System) with legacy Input fallback for Android builds.
/// </summary>
public static class TouchInputBridge
{
    public struct TouchSample
    {
        public int fingerId;
        public Vector2 position;
        public Vector2 deltaPosition;
        public TouchPhase phase;
    }

    static bool _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        EnsureInitialized();
    }

    public static void EnsureInitialized()
    {
        if (_initialized)
            return;

        EnhancedTouchSupport.Enable();
        Input.multiTouchEnabled = true;
        Input.simulateMouseWithTouches = false;
        _initialized = true;
    }

    public static int touchCount
    {
        get
        {
            EnsureInitialized();
            if (EnhancedTouch.activeTouches.Count > 0)
                return EnhancedTouch.activeTouches.Count;
            return Input.touchCount;
        }
    }

    public static TouchSample GetTouch(int index)
    {
        EnsureInitialized();

        if (EnhancedTouch.activeTouches.Count > 0)
        {
            var t = EnhancedTouch.activeTouches[index];
            return new TouchSample
            {
                fingerId = t.touchId,
                position = t.screenPosition,
                deltaPosition = t.delta,
                phase = MapPhase(t.phase)
            };
        }

        UnityEngine.Touch legacy = Input.GetTouch(index);
        return new TouchSample
        {
            fingerId = legacy.fingerId,
            position = legacy.position,
            deltaPosition = legacy.deltaPosition,
            phase = legacy.phase
        };
    }

    static TouchPhase MapPhase(UnityEngine.InputSystem.TouchPhase phase)
    {
        switch (phase)
        {
            case UnityEngine.InputSystem.TouchPhase.Began:
                return TouchPhase.Began;
            case UnityEngine.InputSystem.TouchPhase.Moved:
                return TouchPhase.Moved;
            case UnityEngine.InputSystem.TouchPhase.Stationary:
                return TouchPhase.Stationary;
            case UnityEngine.InputSystem.TouchPhase.Ended:
                return TouchPhase.Ended;
            case UnityEngine.InputSystem.TouchPhase.Canceled:
                return TouchPhase.Canceled;
            default:
                return TouchPhase.Stationary;
        }
    }
}
