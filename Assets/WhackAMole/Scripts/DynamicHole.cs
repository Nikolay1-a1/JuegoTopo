using System.Collections;
using UnityEngine;

/// <summary>
/// Un hoyo del pool. Ciclo: aparece (escala 0→1) → asoma el topo → espera → se esconde (golpeado o no)
/// → el hoyo colapsa (escala 1→0) → pide al manager una nueva posición aleatoria.
/// Cada aparición usa un tipo de topo (común, amarillo, rojo...).
/// </summary>
public class DynamicHole : MonoBehaviour
{
    [Header("Referencias (opcionales)")]
    [SerializeField] private Transform moleTransform;
    [SerializeField] private ParticleSystem hitFX;
    [SerializeField] private ParticleSystem spawnFX;

    [Header("Animación")]
    [SerializeField] private float hiddenY = -0.20f;
    [SerializeField] private float visibleY = 0.04f;
    [SerializeField] private float holeAnimTime = 0.25f;
    [SerializeField] private float moleAnimTime = 0.15f;
    [SerializeField] private Vector2 prePopDelay = new Vector2(0.1f, 0.5f);
    [SerializeField] private Vector2 visibleTime = new Vector2(1.2f, 2.5f);

    public bool IsActive { get; private set; }
    public bool IsMoleUp { get; private set; }
    public MoleTypeDef CurrentType { get; private set; }
    public int LastPoints { get; private set; }

    private DynamicMoleManager manager;
    private Vector3 moleOriginalScale = Vector3.one;
    private Vector3 moleBaseScale = Vector3.one;
    private bool isHit;
    private Coroutine cycle;

    public void Init(DynamicMoleManager mgr, Transform mole)
    {
        manager = mgr;
        if (mole != null) moleTransform = mole;
        if (moleTransform != null)
        {
            moleOriginalScale = moleTransform.localScale;
            moleBaseScale = moleOriginalScale;
        }
        HideMoleImmediate();
        transform.localScale = Vector3.zero;
        IsActive = false;
    }

    public void SpawnAtPosition(Vector3 worldPos, Quaternion rotation, float timeScale, MoleTypeDef type = null)
    {
        if (cycle != null) StopCoroutine(cycle);
        gameObject.SetActive(true);
        transform.SetPositionAndRotation(worldPos, rotation);
        transform.localScale = Vector3.zero;

        CurrentType = type;
        moleBaseScale = moleOriginalScale;
        if (type != null)
        {
            ProceduralModelFactory.ApplyMoleType(moleTransform, type);
            moleBaseScale = moleOriginalScale * type.scale;
            timeScale *= type.visibleTimeFactor;
        }

        HideMoleImmediate();
        isHit = false;
        IsMoleUp = false;
        IsActive = true;
        cycle = StartCoroutine(Cycle(timeScale));
    }

    /// <summary>Llamado al tocar el topo. Devuelve true si el golpe cuenta.</summary>
    public bool OnHit()
    {
        if (!IsMoleUp || isHit) return false;
        isHit = true;
        if (hitFX != null) hitFX.Play();
        LastPoints = manager != null ? manager.NotifyMoleHit(this) : 0;
        return true;
    }

    /// <summary>Colapsa el hoyo sin pedir reubicación (fin de partida).</summary>
    public void CollapseNow()
    {
        if (!IsActive) return;
        if (cycle != null) StopCoroutine(cycle);
        IsMoleUp = false;
        cycle = StartCoroutine(ShutdownRoutine());
    }

    private IEnumerator Cycle(float timeScale)
    {
        if (spawnFX != null) spawnFX.Play();
        yield return ScaleHole(0f, 1f, holeAnimTime);
        yield return new WaitForSeconds(Random.Range(prePopDelay.x, prePopDelay.y) * timeScale);

        if (moleTransform != null) moleTransform.gameObject.SetActive(true);
        IsMoleUp = true;
        yield return MoveMole(hiddenY, visibleY, moleAnimTime);

        float show = Random.Range(visibleTime.x, visibleTime.y) * timeScale;
        float t = 0f;
        while (t < show && !isHit)
        {
            t += Time.deltaTime;
            yield return null;
        }

        IsMoleUp = false;
        if (isHit)
        {
            yield return SquashMole();
        }
        else
        {
            if (manager != null) manager.NotifyMoleEscaped(this);
            yield return MoveMole(visibleY, hiddenY, moleAnimTime);
        }
        HideMoleImmediate();

        yield return ScaleHole(1f, 0f, holeAnimTime);
        IsActive = false;
        if (manager != null) manager.RequestNewHoleLocation(this);
        gameObject.SetActive(false);
    }

    private IEnumerator ShutdownRoutine()
    {
        HideMoleImmediate();
        float from = transform.localScale.x;
        yield return ScaleHole(from, 0f, holeAnimTime * 0.8f);
        IsActive = false;
        gameObject.SetActive(false);
    }

    private IEnumerator SquashMole()
    {
        if (moleTransform == null) yield break;
        Vector3 from = moleBaseScale;
        Vector3 to = new Vector3(moleBaseScale.x * 1.25f, moleBaseScale.y * 0.35f, moleBaseScale.z * 1.25f);
        float t = 0f, d = 0.08f;
        while (t < d)
        {
            t += Time.deltaTime;
            moleTransform.localScale = Vector3.Lerp(from, to, t / d);
            yield return null;
        }
        yield return MoveMole(moleTransform.localPosition.y, hiddenY, 0.1f);
    }

    private IEnumerator ScaleHole(float a, float b, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t / time));
            yield return null;
        }
        transform.localScale = Vector3.one * b;
    }

    private IEnumerator MoveMole(float fromY, float toY, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            SetMoleY(Mathf.Lerp(fromY, toY, Mathf.SmoothStep(0f, 1f, t / time)));
            yield return null;
        }
        SetMoleY(toY);
    }

    private void SetMoleY(float y)
    {
        if (moleTransform == null) return;
        Vector3 p = moleTransform.localPosition;
        p.y = y;
        moleTransform.localPosition = p;
    }

    private void HideMoleImmediate()
    {
        if (moleTransform == null) return;
        moleTransform.localScale = moleBaseScale;
        SetMoleY(hiddenY);
        moleTransform.gameObject.SetActive(false);
    }
}
