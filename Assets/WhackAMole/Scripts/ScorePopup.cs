using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>Texto flotante en el mundo ("+30") que sube, crece un poco, se desvanece y mira a la cámara.</summary>
public class ScorePopup : MonoBehaviour
{
    public static void Spawn(Vector3 position, string text, Color color, Camera cam)
    {
        GameObject go = new GameObject("ScorePopup");
        go.transform.position = position;
        go.AddComponent<ScorePopup>().Begin(text, color, cam);
    }

    private void Begin(string text, Color color, Camera cam)
    {
        TextMeshPro tmp = gameObject.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 1.6f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.rectTransform.sizeDelta = new Vector2(2f, 0.6f);
        tmp.sortingOrder = 100;
        StartCoroutine(Animate(tmp, cam));
    }

    private IEnumerator Animate(TextMeshPro tmp, Camera cam)
    {
        Vector3 start = transform.position;
        float duration = 0.9f, t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            transform.position = start + Vector3.up * (0.30f * (1f - (1f - k) * (1f - k)));
            float pop = k < 0.2f ? Mathf.Lerp(0.5f, 1.15f, k / 0.2f) : (k < 0.4f ? Mathf.Lerp(1.15f, 1f, (k - 0.2f) / 0.2f) : 1f);
            transform.localScale = Vector3.one * pop;
            Color c = tmp.color;
            c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            tmp.color = c;
            if (cam != null)
            {
                Vector3 away = transform.position - cam.transform.position;
                if (away.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(away);
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
