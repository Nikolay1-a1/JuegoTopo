using System.Collections.Generic;
using UnityEngine;

/// <summary>Durante la partida: toque/clic → raycast a la capa Mole → golpe + animación del martillo.</summary>
public class HammerTouchInput : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private LayerMask moleLayer;
    [SerializeField] private float maxDistance = 20f;

    [Header("Martillo propio (opcional)")]
    [Tooltip("Origen en el punto de impacto (la cabeza); el mango sube por +Y.")]
    [SerializeField] private GameObject hammerPrefab;
    [SerializeField] private Material hammerWoodMaterial;
    [SerializeField] private Material hammerHeadMaterial;

    public bool InputEnabled { get; set; }

    private readonly List<Vector2> presses = new List<Vector2>();

    private void Update()
    {
        if (!InputEnabled) return;
        PointerInput.GetPressesThisFrame(presses);
        for (int i = 0; i < presses.Count; i++)
        {
            if (PointerInput.IsOverButton(presses[i])) continue;
            Ray ray = arCamera.ScreenPointToRay(presses[i]);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, moleLayer, QueryTriggerInteraction.Collide))
            {
                DynamicHole hole = hit.collider.GetComponentInParent<DynamicHole>();
                if (hole != null && hole.OnHit()) SpawnHammer(hit.point);
            }
        }
    }

    private void SpawnHammer(Vector3 point)
    {
        GameObject hammer = hammerPrefab != null
            ? Instantiate(hammerPrefab)
            : ProceduralModelFactory.BuildHammer(hammerWoodMaterial, hammerHeadMaterial);
        HammerSwing swing = hammer.AddComponent<HammerSwing>();
        swing.Play(point, arCamera.transform);
    }
}
