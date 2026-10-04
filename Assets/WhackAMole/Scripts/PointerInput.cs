using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Utilidades de entrada con el Input System: toques (móvil) y clic izquierdo (Editor).</summary>
public static class PointerInput
{
    private static readonly List<RaycastResult> uiResults = new List<RaycastResult>();

    public static void GetPressesThisFrame(List<Vector2> buffer)
    {
        buffer.Clear();
        var ts = Touchscreen.current;
        if (ts != null)
        {
            var touches = ts.touches;
            for (int i = 0; i < touches.Count; i++)
                if (touches[i].press.wasPressedThisFrame)
                    buffer.Add(touches[i].position.ReadValue());
        }
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            buffer.Add(mouse.position.ReadValue());
    }

    /// <summary>True si la posición de pantalla cae sobre un botón (u otro Selectable) de la UI.</summary>
    public static bool IsOverButton(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        var data = new PointerEventData(EventSystem.current) { position = screenPos };
        uiResults.Clear();
        EventSystem.current.RaycastAll(data, uiResults);
        for (int i = 0; i < uiResults.Count; i++)
            if (uiResults[i].gameObject.GetComponentInParent<Selectable>() != null) return true;
        return false;
    }
}
// recompile-trigger
