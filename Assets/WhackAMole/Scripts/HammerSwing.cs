using System.Collections;
using UnityEngine;

/// <summary>Anima el golpe del martillo (cae sobre el punto de impacto) y se autodestruye.</summary>
public class HammerSwing : MonoBehaviour
{
    public void Play(Vector3 target, Transform cam)
    {
        Vector3 fwd = cam != null ? Vector3.ProjectOnPlane(cam.forward, Vector3.up) : Vector3.forward;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        Quaternion yaw = Quaternion.LookRotation(fwd.normalized, Vector3.up);
        StartCoroutine(Run(target, yaw));
    }

    private IEnumerator Run(Vector3 target, Quaternion yaw)
    {
        Vector3 startPos = target + Vector3.up * 0.2f;
        Quaternion startRot = yaw * Quaternion.Euler(-50f, 0f, 0f);
        Quaternion endRot = yaw * Quaternion.Euler(-20f, 0f, 0f);
        transform.SetPositionAndRotation(startPos, startRot);

        float t = 0f, d = 0.10f;
        while (t < d)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / d);
            k *= k;
            transform.SetPositionAndRotation(Vector3.Lerp(startPos, target, k), Quaternion.Slerp(startRot, endRot, k));
            yield return null;
        }
        transform.SetPositionAndRotation(target, endRot);
        yield return new WaitForSeconds(0.18f);

        Vector3 s = transform.localScale;
        t = 0f;
        d = 0.12f;
        while (t < d)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(s, Vector3.zero, t / d);
            yield return null;
        }
        Destroy(gameObject);
    }
}
