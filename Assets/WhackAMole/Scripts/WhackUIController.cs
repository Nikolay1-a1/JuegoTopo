using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pantallas: inicio (topo animado + tipos de topo + fijar zona), HUD (puntos, combo, tiempo)
/// y Game Over (puntuación con cuenta animada y reinicio). Incluye pequeñas animaciones.
/// </summary>
public class WhackUIController : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Inicio")]
    [SerializeField] private RectTransform titleRect;
    [SerializeField] private RectTransform startCard;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text legendText;
    [SerializeField] private TMP_Text moleLabel;
    [SerializeField] private RawImage startMoleImage;
    [SerializeField] private Button moleButton;
    [SerializeField] private Button playButton;

    [Header("Topo del menú")]
    [SerializeField] private MenuMoleDisplay menuMole;

    [Header("HUD")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private TMP_Text timerText;

    [Header("Game Over")]
    [SerializeField] private RectTransform gameOverCard;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text bestComboText;
    [SerializeField] private RawImage overMoleImage;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button changeZoneButton;

    public event Action PlayClicked;
    public event Action RestartClicked;
    public event Action ChangeZoneClicked;

    private int lastSeconds = -1;
    private int lastScore;
    private int lastCombo;
    private Coroutine scorePunch, comboPunch, timerPunch, countUp;

    private void Awake()
    {
        if (playButton != null) playButton.onClick.AddListener(() => PlayClicked?.Invoke());
        if (restartButton != null) restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
        if (changeZoneButton != null) changeZoneButton.onClick.AddListener(() => ChangeZoneClicked?.Invoke());
    }

    private void Start()
    {
        if (menuMole == null) return;
        if (startMoleImage != null) startMoleImage.texture = menuMole.Texture;
        if (overMoleImage != null) overMoleImage.texture = menuMole.Texture;
        if (moleButton != null) moleButton.onClick.AddListener(menuMole.Poke);
        menuMole.TypeChanged += OnMoleTypeChanged;
        OnMoleTypeChanged(menuMole.Current);
        BuildLegend();
    }

    private void OnDestroy()
    {
        if (menuMole != null) menuMole.TypeChanged -= OnMoleTypeChanged;
    }

    private void Update()
    {
        if (titleRect != null && startPanel != null && startPanel.activeInHierarchy)
        {
            float t = Time.unscaledTime;
            titleRect.localScale = Vector3.one * (1f + 0.03f * Mathf.Sin(t * 2.2f));
            titleRect.localRotation = Quaternion.Euler(0f, 0f, 1.5f * Mathf.Sin(t * 1.4f));
        }
    }

    // ---------- Pantallas ----------
    public void ShowStart()
    {
        startPanel.SetActive(true);
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        if (menuMole != null) menuMole.SetVisible(true);
        Pop(startCard);
    }

    public void ShowHud()
    {
        startPanel.SetActive(false);
        hudPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        if (menuMole != null) menuMole.SetVisible(false);
        lastSeconds = -1;
        lastScore = 0;
        lastCombo = 0;
    }

    public void ShowGameOver(int score, int bestCombo)
    {
        startPanel.SetActive(false);
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(true);
        bestComboText.text = "Mejor combo: " + bestCombo;
        if (menuMole != null)
        {
            menuMole.SetVisible(true);
            menuMole.Cheer();
        }
        Pop(gameOverCard);
        if (countUp != null) StopCoroutine(countUp);
        countUp = StartCoroutine(CountUp(score));
    }

    // ---------- Textos ----------
    public void SetStatus(string message) { if (statusText != null) statusText.text = message; }
    public void SetCanPlay(bool canPlay) { if (playButton != null) playButton.interactable = canPlay; }

    public void SetScore(int score)
    {
        if (scoreText == null) return;
        scoreText.text = "Puntos: " + score;
        if (score > lastScore) Punch(ref scorePunch, scoreText.rectTransform, 1.22f);
        lastScore = score;
    }

    public void SetCombo(int combo, int multiplier)
    {
        if (comboText == null) return;
        comboText.text = "Combo: " + combo + "  (x" + multiplier + ")";
        comboText.color = multiplier >= 4 ? new Color(1f, 0.35f, 0.25f) : multiplier == 3 ? new Color(1f, 0.65f, 0.2f) : multiplier == 2 ? new Color(1f, 0.9f, 0.3f) : Color.white;
        if (combo > lastCombo && combo > 1) Punch(ref comboPunch, comboText.rectTransform, 1.25f);
        lastCombo = combo;
    }

    public void SetTime(float seconds)
    {
        int s = Mathf.CeilToInt(seconds);
        if (s == lastSeconds || timerText == null) return;
        lastSeconds = s;
        timerText.text = "Tiempo: " + s;
        bool urgent = s <= 10;
        timerText.color = urgent ? new Color(1f, 0.3f, 0.3f) : Color.white;
        if (urgent) Punch(ref timerPunch, timerText.rectTransform, 1.18f);
    }

    // ---------- Topo del menú ----------
    private void OnMoleTypeChanged(MoleTypeDef t)
    {
        if (t == null || moleLabel == null) return;
        moleLabel.text = "Topo " + t.name.ToLower() + "  +" + t.points;
        moleLabel.color = Brighten(t.color);
    }

    private void BuildLegend()
    {
        if (legendText == null || menuMole == null) return;
        var sb = new StringBuilder();
        var types = menuMole.Types;
        for (int i = 0; i < types.Count; i++)
        {
            if (i > 0) sb.Append("   ");
            sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(Brighten(types[i].color))).Append(">")
              .Append(types[i].name).Append("</color> +").Append(types[i].points);
        }
        legendText.text = sb.ToString();
    }

    private static Color Brighten(Color c) { return Color.Lerp(c, Color.white, 0.25f); }

    // ---------- Animaciones ----------
    private void Pop(RectTransform r)
    {
        if (r != null) StartCoroutine(PopRoutine(r));
    }

    private IEnumerator PopRoutine(RectTransform r)
    {
        float t = 0f, d = 0.35f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / d);
            float e = 1f + 2.70158f * Mathf.Pow(k - 1f, 3f) + 1.70158f * Mathf.Pow(k - 1f, 2f);
            r.localScale = Vector3.one * Mathf.LerpUnclamped(0.75f, 1f, e);
            yield return null;
        }
        r.localScale = Vector3.one;
    }

    private void Punch(ref Coroutine slot, RectTransform r, float peak)
    {
        if (r == null || !r.gameObject.activeInHierarchy) return;
        if (slot != null) StopCoroutine(slot);
        slot = StartCoroutine(PunchRoutine(r, peak));
    }

    private IEnumerator PunchRoutine(RectTransform r, float peak)
    {
        float t = 0f, d = 0.22f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / d);
            r.localScale = Vector3.one * Mathf.Lerp(peak, 1f, 1f - (1f - k) * (1f - k));
            yield return null;
        }
        r.localScale = Vector3.one;
    }

    private IEnumerator CountUp(int target)
    {
        float duration = Mathf.Clamp(0.4f + target * 0.004f, 0.4f, 1.6f);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            finalScoreText.text = "Puntuación final: " + Mathf.RoundToInt(target * (1f - (1f - k) * (1f - k)));
            yield return null;
        }
        finalScoreText.text = "Puntuación final: " + target;
    }
}
