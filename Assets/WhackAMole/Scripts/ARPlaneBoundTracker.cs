using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Guarda la zona de juego (punto fijado sobre un ARPlane) y entrega puntos aleatorios que caen
/// DENTRO del polígono del plano, lejos del borde y separados de otros hoyos.
/// Sin plano (pruebas en Editor) usa un círculo libre alrededor del centro.
/// </summary>
public class ARPlaneBoundTracker : MonoBehaviour
{
    [SerializeField] private float playRadius = 0.8f;
    [SerializeField] private float edgeMargin = 0.08f;
    [SerializeField] private int maxAttempts = 40;

    private ARPlane plane;
    private Vector3 center;

    public bool HasZone { get; private set; }
    public Vector3 Center => center;
    public float PlayRadius => playRadius;

    public void SetZone(ARPlane p, Vector3 worldCenter)
    {
        plane = p;
        center = worldCenter;
        HasZone = true;
    }

    public void ClearZone()
    {
        plane = null;
        HasZone = false;
    }

    public Vector3 GetRandomPoint(List<Vector3> avoid, float minSeparation)
    {
        if (plane != null && plane.subsumedBy != null) plane = plane.subsumedBy;

        for (int pass = 0; pass < 2; pass++)
        {
            float sep = pass == 0 ? minSeparation : minSeparation * 0.5f;
            float margin = pass == 0 ? edgeMargin : 0f;
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 c = Random.insideUnitCircle * playRadius;
                Vector3 candidate = ToWorld(c);
                if (!InsidePlane(candidate, margin)) continue;
                if (TooClose(candidate, avoid, sep)) continue;
                return candidate;
            }
        }
        return center;
    }

    private Vector3 ToWorld(Vector2 c)
    {
        Vector3 right = plane != null ? plane.transform.right : Vector3.right;
        Vector3 fwd = plane != null ? plane.transform.forward : Vector3.forward;
        return center + right * c.x + fwd * c.y;
    }

    private static bool TooClose(Vector3 p, List<Vector3> avoid, float minSep)
    {
        float sq = minSep * minSep;
        for (int i = 0; i < avoid.Count; i++)
        {
            Vector3 d = avoid[i] - p;
            d.y = 0f;
            if (d.sqrMagnitude < sq) return true;
        }
        return false;
    }

    private bool InsidePlane(Vector3 world, float margin)
    {
        if (plane == null) return true;
        var boundary = plane.boundary;
        if (!boundary.IsCreated || boundary.Length < 3) return true;

        Vector3 local = plane.transform.InverseTransformPoint(world);
        Vector2 p = new Vector2(local.x, local.z);

        bool inside = false;
        float minDist = float.MaxValue;
        for (int i = 0, j = boundary.Length - 1; i < boundary.Length; j = i++)
        {
            Vector2 a = boundary[i];
            Vector2 b = boundary[j];
            if (((a.y > p.y) != (b.y > p.y)) && (p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x))
                inside = !inside;
            minDist = Mathf.Min(minDist, DistToSegment(p, a, b));
        }
        return inside && (margin <= 0f || minDist >= margin);
    }

    private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 1e-8f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
        return Vector2.Distance(p, a + ab * t);
    }
}
