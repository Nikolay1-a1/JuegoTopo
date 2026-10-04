using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>Máquina de estados: Escaneo/colocación → Jugando → Game Over → (Reiniciar | Cambiar zona).</summary>
public class GameFlowController : MonoBehaviour
{
    private enum State { Scanning, Playing, GameOver }

    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARPlacementInput placement;
    [SerializeField] private ARPlaneBoundTracker tracker;
    [SerializeField] private DynamicMoleManager manager;
    [SerializeField] private HammerTouchInput hammerInput;
    [SerializeField] private WhackUIController ui;

    private State state;
    private int lastStatus = -1;

    private void OnEnable()
    {
        placement.ZonePlaced += OnZonePlaced;
        ui.PlayClicked += StartGame;
        ui.RestartClicked += StartGame;
        ui.ChangeZoneClicked += EnterScanning;
        manager.ScoreChanged += ui.SetScore;
        manager.ComboChanged += ui.SetCombo;
        manager.TimeChanged += ui.SetTime;
        manager.GameOver += OnGameOver;
    }

    private void OnDisable()
    {
        placement.ZonePlaced -= OnZonePlaced;
        ui.PlayClicked -= StartGame;
        ui.RestartClicked -= StartGame;
        ui.ChangeZoneClicked -= EnterScanning;
        manager.ScoreChanged -= ui.SetScore;
        manager.ComboChanged -= ui.SetCombo;
        manager.TimeChanged -= ui.SetTime;
        manager.GameOver -= OnGameOver;
    }

    private void Start()
    {
        EnterScanning();
    }

    private void Update()
    {
        if (state == State.Scanning) UpdateStatus();
    }

    private void EnterScanning()
    {
        state = State.Scanning;
        manager.StopGame();
        tracker.ClearZone();
        placement.HideMarker();
        hammerInput.InputEnabled = false;
        placement.InputEnabled = true;
        if (planeManager != null) planeManager.SetTrackablesActive(true);
        ui.ShowStart();
        ui.SetCanPlay(false);
        lastStatus = -1;
        UpdateStatus();
    }

    private void StartGame()
    {
        if (!tracker.HasZone) return;
        state = State.Playing;
        placement.InputEnabled = false;
        placement.HideMarker();
        if (planeManager != null) planeManager.SetTrackablesActive(false);
        hammerInput.InputEnabled = true;
        ui.ShowHud();
        manager.StartGame();
    }

    private void OnGameOver(int score, int bestCombo)
    {
        state = State.GameOver;
        hammerInput.InputEnabled = false;
        ui.ShowGameOver(score, bestCombo);
    }

    private void OnZonePlaced(Vector3 position, ARPlane plane)
    {
        ui.SetCanPlay(true);
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        int key;
        string msg;
        if (tracker.HasZone)
        {
            key = 3;
            msg = "Zona fijada. Pulsa ¡Jugar! o toca otro punto para moverla.";
        }
        else if (placement.UsesEditorFallback)
        {
            key = 2;
            msg = "Editor: haz clic en la pantalla para fijar la zona de juego.";
        }
        else if (planeManager != null && planeManager.trackables.count > 0)
        {
            key = 1;
            msg = "Toca una superficie detectada para fijar la zona de juego.";
        }
        else
        {
            key = 0;
            msg = "Mueve el móvil despacio para detectar una mesa o el suelo…";
        }
        if (key == lastStatus) return;
        lastStatus = key;
        ui.SetStatus(msg);
    }
}
