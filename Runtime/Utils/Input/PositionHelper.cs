using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GGemCo2DCore
{
    public static class PositionHelper
    {
        public static Vector3 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            // 마우스 우선, 터치/패드 포인터 폴백
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return Touchscreen.current.primaryTouch.position.ReadValue();
            // 게임패드엔 기본 포인터가 없으므로 마지막 마우스 위치 폴백
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Input.mousePosition;
#else
            return Input.mousePosition;
#endif
        }
    }
}