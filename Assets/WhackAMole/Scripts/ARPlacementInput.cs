using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Fase de inicio: un toque (o clic) sobre un plano horizontal detectado fija la zona de juego.
/// En el Editor, si no hay planos simulados, usa un plano matemático a la altura del XR Origin.
/// </summary>
public class ARPlacementInput : MonoBehaviour
{
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private Camera arCamera;
    [SerializeField] private Transform originTransform;
    [SerializeField] private ARPlaneBoundTracker tracker;
    [SerializeField] private Material markerMaterial;
    [SerializeField] private bool editorFallbackPlane = true;
    [SerializeField] private float markerHeightOffset = 0.01f;

    public bool InputEnabled { get; set; }
    public event Action<Vector3, ARPlane> ZonePlaced;

    public bool UsesEditorFallback =>
        Application.isEditor && editorFallbackPlane && (planeManager == null || planeManager.trackables.count == 0);

    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private readonly List<Vector2> presses = new List<Vector2>();
    private LineRenderer marker;

    private void Awake()
    {
        GameObject go = new GameObject("ZoneMarker");
        go.transform.SetParent(transform, false);
        marker = go.AddComponent<LineRenderer>();
        marker.loop = true;
        marker.useWorldSpace = true;
        marker.widthMultiplier = 0.012f;
        marker.positionCount = 64;
        marker.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Material m = markerMaterial;
        if (m == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Sprites/Default");
            m = new Material(s);
            m.color = new Color(0.3f, 1f, 0.4f);
        }
        marker.sharedMaterial = m;
        marker.enabled = false;
    }

    private void Update()
    {
        if (!InputEnabled) return;
        PointerInput.GetPressesThisFrame(presses);
        for (int i = 0; i < presses.Count; i++)
        {
            if (PointerInput.IsOverButton(presses[i])) continue;
            if (TryPlace(presses[i])) break;
        }
    }

    public void HideMarker()
    {
        if (marker != null) marker.enabled = false;
    }

    private bool TryPlace(Vector2 screenPos)
    {
        if (raycastManager != null && raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            for (int i = 0; i < hits.Count; i++)
            {
                ARPlane p = planeManager != null ? planeManager.GetPlane(hits[i].trackableId) : null;
                if (p != null && p.alignment == PlaneAlignment.HorizontalUp)
                {
                    Place(hits[i].pose.position, p);
                    return true;
                }
            }
        }

        if (UsesEditorFallback && arCamera != null)
        {
            Vector3 origin = originTransform != null ? originTransform.position : Vector3.zero;
            Plane virtualPlane = new Plane(Vector3.up, origin);
            Ray ray = arCamera.ScreenPointToRay(screenPos);
            if (virtualPlane.Raycast(ray, out float dist))
            {
                Place(ray.GetPoint(dist), null);
                return true;
            }
        }
        return false;
    }

    private void Place(Vector3 position, ARPlane plane)
    {
        tracker.SetZone(plane, position);
        DrawMarker(position);
        ZonePlaced?.Invoke(position, plane);
    }

    private void DrawMarker(Vector3 center)
    {
        float r = tracker.PlayRadius;
        for (int i = 0; i < marker.positionCount; i++)
        {
            float a = i * Mathf.PI * 2f / marker.positionCount;
            marker.SetPosition(i, center + new Vector3(Mathf.Cos(a) * r, markerHeightOffset, Mathf.Sin(a) * r));
        }
        marker.enabled = true;
    }
}
