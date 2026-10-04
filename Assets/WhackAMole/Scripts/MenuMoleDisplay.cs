using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Topo 3D animado para el menú: se dibuja con una cámara propia sobre una RenderTexture
/// (que la UI muestra en un RawImage). Respira, parpadea, cambia de tipo (común/amarillo/rojo)
/// y reacciona cuando lo tocas.
/// </summary>
[DefaultExecutionOrder(-50)]
public class MenuMoleDisplay : MonoBehaviour
{
    [SerializeField] private DynamicMoleManager manager;

    [Header("Materiales (opcional)")]
    [SerializeField] private Material holeDirtMaterial;
    [SerializeField] private Material holeRimMaterial;
    [SerializeField] private Material moleBodyMaterial;
    [SerializeField] private Material moleNoseMaterial;
    [SerializeField] private Material moleEyeMaterial;

    [Header("Ajustes")]
    [SerializeField] private int renderSize = 512;
    [SerializeField] private float typeChangeInterval = 3.5f;
    [SerializeField] private string stageLayerName = "MenuMole";

    public RenderTexture Texture { get; private set; }
    public MoleTypeDef Current { get; private set; }
    public IReadOnlyList<MoleTypeDef> Types => types;
    public event Action<MoleTypeDef> TypeChanged;

    private const float HiddenY = -0.20f;
    private const float VisibleY = 0.04f;
    private const float EyeSize = 0.025f;

    private List<MoleTypeDef> types;
    private GameObject stage;
    private Camera cam;
    private Transform moleT, eyeL, eyeR;
    private int typeIndex;
    private int layerIdx;
    private bool visible = true;
    private bool busy;
    private float nextChange;
    private float nextBlink;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        types = (manager != null && manager.MoleTypes != null && manager.MoleTypes.Count > 0)
            ? new List<MoleTypeDef>(manager.MoleTypes)
            : MoleTypeDef.CreateDefaults();

        layerIdx = LayerMask.NameToLayer(stageLayerName);
        if (layerIdx < 0)
        {
            Debug.LogWarning("[WhackAMole] Falta la capa '" + stageLayerName + "'; el topo del menú podría verse en la escena AR.");
            layerIdx = 0;
        }

        // Escenario lejos de la zona de juego, girado 180° para que la luz de la escena le dé de frente.
        stage = new GameObject("MenuMoleStage");
        stage.transform.SetPositionAndRotation(new Vector3(0f, 800f, 0f), Quaternion.Euler(0f, 180f, 0f));

        GameObject hole = new GameObject("MenuHole");
        hole.transform.SetParent(stage.transform, false);
        ProceduralModelFactory.BuildHoleVisual(hole.transform, holeDirtMaterial, holeRimMaterial);
        GameObject mole = ProceduralModelFactory.BuildMole(hole.transform, moleBodyMaterial, moleNoseMaterial, moleEyeMaterial);
        moleT = mole.transform;
        eyeL = moleT.Find("EyeL");
        eyeR = moleT.Find("EyeR");

        GameObject camGo = new GameObject("MenuMoleCamera");
        camGo.transform.SetParent(stage.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 0.2f, 0.75f);
        camGo.transform.localRotation = Quaternion.Euler(8f, 180f, 0f);
        cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.cullingMask = 1 << layerIdx;
        cam.fieldOfView = 30f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 5f;
        cam.depth = -50f;

        Texture = new RenderTexture(renderSize, renderSize, 16, RenderTextureFormat.ARGB32);
        Texture.antiAliasing = 4;
        cam.targetTexture = Texture;

        ProceduralModelFactory.SetLayerRecursive(stage, layerIdx);

        typeIndex = 0;
        ApplyType(types[0]);
        SetY(VisibleY);
        nextChange = Time.unscaledTime + typeChangeInterval;
        nextBlink = Time.unscaledTime + 2f;
    }

    private void Start()
    {
        // La cámara AR no debe dibujar el escenario del menú.
        if (layerIdx > 0 && Camera.main != null) Camera.main.cullingMask &= ~(1 << layerIdx);
    }

    private void OnDestroy()
    {
        if (Texture != null) Texture.Release();
        if (stage != null) Destroy(stage);
    }

    public void SetVisible(bool value)
    {
        visible = value;
        if (cam != null) cam.enabled = value;
        if (stage != null) stage.SetActive(value);
        if (value) nextChange = Time.unscaledTime + typeChangeInterval;
    }

    /// <summary>Toque sobre el topo: se aplasta, se esconde y vuelve con otro tipo.</summary>
    public void Poke()
    {
        if (!visible || busy) return;
        StartCoroutine(PokeRoutine());
    }

    /// <summary>Saltitos de alegría (pantalla de Game Over).</summary>
    public void Cheer()
    {
        if (!visible || busy) return;
        StartCoroutine(CheerRoutine());
    }

    private void Update()
    {
        if (!visible || busy) return;
        float t = Time.unscaledTime;
        SetY(VisibleY + 0.012f * Mathf.Sin(t * 3f));
        moleT.localRotation = Quaternion.Euler(0f, 10f * Mathf.Sin(t * 1.3f), 5f * Mathf.Sin(t * 2f));
        if (t >= nextBlink)
        {
            StartCoroutine(Blink());
            nextBlink = t + UnityEngine.Random.Range(2.5f, 4.5f);
        }
        if (t >= nextChange) StartCoroutine(SwitchRoutine(NextType()));
    }

    private MoleTypeDef NextType()
    {
        typeIndex = (typeIndex + 1) % types.Count;
        return types[typeIndex];
    }

    private void ApplyType(MoleTypeDef t)
    {
        Current = t;
        ProceduralModelFactory.ApplyMoleType(moleT, t);
        baseScale = Vector3.one * t.scale;
        moleT.localScale = baseScale;
        TypeChanged?.Invoke(t);
    }

    private void SetY(float y)
    {
        Vector3 p = moleT.localPosition;
        p.y = y;
        moleT.localPosition = p;
    }

    private void SetEyes(float openness)
    {
        if (eyeL != null) eyeL.localScale = new Vector3(EyeSize, EyeSize * openness, EyeSize);
        if (eyeR != null) eyeR.localScale = new Vector3(EyeSize, EyeSize * openness, EyeSize);
    }

    private IEnumerator Blink()
    {
        SetEyes(0.15f);
        yield return new WaitForSecondsRealtime(0.1f);
        SetEyes(1f);
    }

    private static float EaseOutBack(float k)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
    }

    private IEnumerator MoveY(float from, float to, float time, bool overshoot)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            float e = overshoot ? EaseOutBack(k) : Mathf.SmoothStep(0f, 1f, k);
            SetY(Mathf.LerpUnclamped(from, to, e));
            yield return null;
        }
        SetY(to);
    }

    private IEnumerator SwitchRoutine(MoleTypeDef next)
    {
        busy = true;
        yield return MoveY(moleT.localPosition.y, HiddenY, 0.18f, false);
        ApplyType(next);
        yield return MoveY(HiddenY, VisibleY, 0.30f, true);
        busy = false;
        nextChange = Time.unscaledTime + typeChangeInterval;
    }

    private IEnumerator PokeRoutine()
    {
        busy = true;
        Vector3 squash = new Vector3(baseScale.x * 1.3f, baseScale.y * 0.65f, baseScale.z * 1.3f);
        float t = 0f, d = 0.07f;
        while (t < d) { t += Time.unscaledDeltaTime; moleT.localScale = Vector3.Lerp(baseScale, squash, t / d); yield return null; }
        t = 0f; d = 0.12f;
        while (t < d) { t += Time.unscaledDeltaTime; moleT.localScale = Vector3.Lerp(squash, baseScale, t / d); yield return null; }
        moleT.localScale = baseScale;
        yield return SwitchRoutine(NextType());
    }

    private IEnumerator CheerRoutine()
    {
        busy = true;
        float t = 0f, d = 0.9f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float hop = Mathf.Abs(Mathf.Sin(t / d * Mathf.PI * 2f));
            SetY(VisibleY + 0.09f * hop);
            moleT.localRotation = Quaternion.Euler(0f, 0f, 12f * Mathf.Sin(t * 14f));
            yield return null;
        }
        SetY(VisibleY);
        busy = false;
        nextChange = Time.unscaledTime + typeChangeInterval;
    }
}
