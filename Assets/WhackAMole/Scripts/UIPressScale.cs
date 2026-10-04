using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Feedback de botón: se encoge al pulsarlo y, opcionalmente, "respira" mientras está activo.</summary>
public class UIPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float pressedScale = 0.92f;
    [SerializeField] private float pulseAmount = 0f;
    [SerializeField] private float pulseSpeed = 4f;

    private float press = 1f;
    private float target = 1f;
    private Selectable selectable;

    private void Awake() { selectable = GetComponent<Selectable>(); }
    private void OnDisable() { target = 1f; press = 1f; transform.localScale = Vector3.one; }

    public void OnPointerDown(PointerEventData e) { target = pressedScale; }
    public void OnPointerUp(PointerEventData e) { target = 1f; }
    public void OnPointerExit(PointerEventData e) { target = 1f; }

    private void Update()
    {
        press = Mathf.Lerp(press, target, 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime));
        float pulse = 1f;
        if (pulseAmount > 0f && (selectable == null || selectable.interactable))
            pulse += pulseAmount * Mathf.Sin(Time.unscaledTime * pulseSpeed);
        float s = press * pulse;
        transform.localScale = new Vector3(s, s, 1f);
    }
}
