using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GameCore.UI
{
    public class UniversalInputField : TMP_InputField
    {
        public override void OnUpdateSelected(BaseEventData eventData)
        {
            base.OnUpdateSelected(eventData);

            if (!isFocused)
                return;

            if (WantToLeave())
            {
                DeactivateInputField();
            }
        }

        public bool WantToLeave() =>
        (Gamepad.current != null && (Gamepad.current.aButton.wasPressedThisFrame || Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.dpad.up.wasPressedThisFrame || Gamepad.current.dpad.down.wasPressedThisFrame)) ||
        (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame));
    }
}
