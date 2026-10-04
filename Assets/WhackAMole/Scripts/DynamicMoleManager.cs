using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pool de hoyos itinerantes + puntuación, combo y temporizador.
/// Cada hoyo que colapsa (golpeado o no) reaparece en otro punto aleatorio del plano,
/// con un tipo de topo elegido al azar según su peso (común, amarillo, rojo...).
/// </summary>
public class DynamicMoleManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private ARPlaneBoundTracker tracker;
    [SerializeField] private Camera arCamera;
    [SerializeField] private Transform holesRoot;

    [Header("Modelos propios (opcional: sustituyen a los procedurales)")]
    [Tooltip("Prefab del hoyo. Si no tiene DynamicHole, se le añade.")]
    [SerializeField] private GameObject holePrefab;
    [Tooltip("Prefab del topo. Su origen debe estar en el centro del cuerpo; se sube/baja en Y.")]
    [SerializeField] private GameObject molePrefab;

    [Header("Materiales de los modelos procedurales (opcional)")]
    [SerializeField] private Material holeDirtMaterial;
    [SerializeField] private Material holeRimMaterial;
    [SerializeField] private Material moleBodyMaterial;
    [SerializeField] private Material moleNoseMaterial;
    [SerializeField] private Material moleEyeMaterial;

    [Header("Tipos de topo (el primero es el común)")]
    [SerializeField] private List<MoleTypeDef> moleTypes = MoleTypeDef.CreateDefaults();
    [Tooltip("Los tipos especiales se vuelven más frecuentes hacia el final de la partida (0 = igual todo el tiempo).")]
    [SerializeField] private float specialRamp = 0.8f;
    [SerializeField] private bool showScorePopups = true;

    [Header("Partida")]
    [SerializeField] private int maxSimultaneousHoles = 6;
    [SerializeField] private float minSeparation = 0.28f;
    [SerializeField] private float gameTime = 60f;
    [Tooltip("Puntos si un topo no tiene tipo asignado.")]
    [SerializeField] private int basePoints = 10;
    [Tooltip("Cada N aciertos seguidos sube el multiplicador en +1.")]
    [SerializeField] private int comboStep = 5;
    [SerializeField] private Vector2 relocateDelay = new Vector2(0.2f, 0.8f);
    [Tooltip("Los tiempos de exposición se reducen hasta este factor al final de la partida.")]
    [SerializeField, Range(0.3f, 1f)] private float finalSpeedFactor = 0.6f;
    [SerializeField] private bool faceCamera = true;

    public int Score { get; private set; }
    public int Combo { get; private set; }
    public int BestCombo { get; private set; }
    public int Multiplier => 1 + Combo / Mathf.Max(1, comboStep);
    public float TimeRemaining { get; private set; }
    public bool IsPlaying { get; private set; }
    public IReadOnlyList<MoleTypeDef> MoleTypes => moleTypes;

    public event Action<int> ScoreChanged;
    public event Action<int, int> ComboChanged;        // combo, multiplicador
    public event Action<float> TimeChanged;
    public event Action<int, int> GameOver;            // puntuación final, mejor combo
    public event Action<int, MoleTypeDef> MoleHitScored; // puntos ganados, tipo

    private readonly List<DynamicHole> pool = new List<DynamicHole>();
    private readonly List<Vector3> avoidBuffer = new List<Vector3>();

    public void StartGame()
    {
        StopAllCoroutines();
        EnsurePool();
        foreach (DynamicHole h in pool) h.CollapseNow();

        Score = 0;
        Combo = 0;
        BestCombo = 0;
        TimeRemaining = gameTime;
        IsPlaying = true;
        ScoreChanged?.Invoke(Score);
        ComboChanged?.Invoke(Combo, Multiplier);
        TimeChanged?.Invoke(TimeRemaining);

        for (int i = 0; i < pool.Count; i++)
            StartCoroutine(SpawnAfter(pool[i], i * 0.25f + UnityEngine.Random.Range(0f, 0.15f)));
    }

    /// <summary>Detiene la partida sin mostrar Game Over (p. ej. al cambiar de zona).</summary>
    public void StopGame()
    {
        IsPlaying = false;
        StopAllCoroutines();
        foreach (DynamicHole h in pool) h.CollapseNow();
    }

    private void Update()
    {
        if (!IsPlaying) return;
        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            TimeChanged?.Invoke(0f);
            EndGame();
            return;
        }
        TimeChanged?.Invoke(TimeRemaining);
    }

    private void EndGame()
    {
        IsPlaying = false;
        StopAllCoroutines();
        foreach (DynamicHole h in pool) h.CollapseNow();
        GameOver?.Invoke(Score, BestCombo);
    }

    /// <summary>Un topo fue golpeado. Devuelve los puntos ganados (tipo × multiplicador de combo).</summary>
    public int NotifyMoleHit(DynamicHole hole)
    {
        if (!IsPlaying) return 0;
        Combo++;
        if (Combo > BestCombo) BestCombo = Combo;

        MoleTypeDef type = hole != null ? hole.CurrentType : null;
        int points = (type != null ? type.points : basePoints) * Multiplier;
        Score += points;

        ScoreChanged?.Invoke(Score);
        ComboChanged?.Invoke(Combo, Multiplier);
        MoleHitScored?.Invoke(points, type);

        if (showScorePopups && hole != null)
        {
            Color c = type != null ? Color.Lerp(type.color, Color.white, 0.35f) : Color.white;
            ScorePopup.Spawn(hole.transform.position + Vector3.up * 0.25f, "+" + points, c, arCamera);
        }
        return points;
    }

    public void NotifyMoleEscaped(DynamicHole hole)
    {
        if (!IsPlaying || Combo == 0) return;
        Combo = 0;
        ComboChanged?.Invoke(Combo, Multiplier);
    }

    /// <summary>El hoyo acaba de colapsar: reaparece en otro punto aleatorio del plano.</summary>
    public void RequestNewHoleLocation(DynamicHole hole)
    {
        if (!IsPlaying) return;
        StartCoroutine(SpawnAfter(hole, UnityEngine.Random.Range(relocateDelay.x, relocateDelay.y)));
    }

    private IEnumerator SpawnAfter(DynamicHole hole, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!IsPlaying) yield break;

        avoidBuffer.Clear();
        foreach (DynamicHole h in pool)
            if (h != hole && h.IsActive) avoidBuffer.Add(h.transform.position);

        Vector3 pos = tracker.GetRandomPoint(avoidBuffer, minSeparation);

        Quaternion rot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        if (faceCamera && arCamera != null)
        {
            Vector3 d = arCamera.transform.position - pos;
            d.y = 0f;
            if (d.sqrMagnitude > 0.001f) rot = Quaternion.LookRotation(d, Vector3.up);
        }

        float progress = gameTime > 0f ? Mathf.Clamp01(1f - TimeRemaining / gameTime) : 0f;
        float timeScale = Mathf.Lerp(1f, finalSpeedFactor, progress);
        hole.SpawnAtPosition(pos, rot, timeScale, PickType(progress));
    }

    private MoleTypeDef PickType(float progress)
    {
        if (moleTypes == null || moleTypes.Count == 0) return null;
        float total = 0f;
        for (int i = 0; i < moleTypes.Count; i++) total += TypeWeight(i, progress);
        if (total <= 0f) return moleTypes[0];
        float r = UnityEngine.Random.value * total;
        for (int i = 0; i < moleTypes.Count; i++)
        {
            r -= TypeWeight(i, progress);
            if (r <= 0f) return moleTypes[i];
        }
        return moleTypes[0];
    }

    private float TypeWeight(int i, float progress)
    {
        float w = Mathf.Max(0f, moleTypes[i].weight);
        return i == 0 ? w : w * (1f + specialRamp * progress);
    }

    private void EnsurePool()
    {
        while (pool.Count < maxSimultaneousHoles) pool.Add(CreateHole(pool.Count));
    }

    private DynamicHole CreateHole(int index)
    {
        Transform parent = holesRoot != null ? holesRoot : transform;
        GameObject root;
        if (holePrefab != null)
        {
            root = Instantiate(holePrefab, parent);
        }
        else
        {
            root = new GameObject();
            root.transform.SetParent(parent, false);
            ProceduralModelFactory.BuildHoleVisual(root.transform, holeDirtMaterial, holeRimMaterial);
        }
        root.name = "Hole " + (index + 1);

        GameObject mole = molePrefab != null
            ? Instantiate(molePrefab, root.transform)
            : ProceduralModelFactory.BuildMole(root.transform, moleBodyMaterial, moleNoseMaterial, moleEyeMaterial);
        mole.name = "Mole";
        mole.transform.localPosition = Vector3.zero;

        int layer = LayerMask.NameToLayer("Mole");
        if (layer < 0)
        {
            Debug.LogWarning("[WhackAMole] No existe la capa 'Mole'; se usa Default.");
            layer = 0;
        }
        ProceduralModelFactory.EnsureHitbox(mole, layer);

        DynamicHole hole = root.GetComponent<DynamicHole>();
        if (hole == null) hole = root.AddComponent<DynamicHole>();
        hole.Init(this, mole.transform);
        root.SetActive(false);
        return hole;
    }
}
