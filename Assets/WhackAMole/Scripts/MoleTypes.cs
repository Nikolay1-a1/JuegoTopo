using System;
using System.Collections.Generic;
using UnityEngine;

public enum MoleAccessory { None, Halo, Horns }

/// <summary>Definición de un tipo de topo. Se edita en el Inspector (DynamicMoleManager → Mole Types).</summary>
[Serializable]
public class MoleTypeDef
{
    public string name = "Común";
    public Color color = new Color(0.55f, 0.35f, 0.20f);
    [Min(0)] public int points = 10;
    [Tooltip("Probabilidad relativa de aparecer.")]
    [Min(0f)] public float weight = 1f;
    [Tooltip("Multiplica el tiempo que el topo permanece fuera (menor = más rápido).")]
    public float visibleTimeFactor = 1f;
    public float scale = 1f;
    public MoleAccessory accessory = MoleAccessory.None;
    [Tooltip("Si usas tu propio prefab de topo, tiñe el modelo con el color (déjalo apagado para respetar tus texturas).")]
    public bool tintCustomPrefab = false;

    public static List<MoleTypeDef> CreateDefaults()
    {
        return new List<MoleTypeDef>
        {
            new MoleTypeDef { name = "Común", color = new Color(0.55f, 0.35f, 0.20f), points = 10, weight = 70f, visibleTimeFactor = 1f, scale = 1f, accessory = MoleAccessory.None, tintCustomPrefab = false },
            new MoleTypeDef { name = "Amarillo", color = new Color(1f, 0.82f, 0.12f), points = 30, weight = 22f, visibleTimeFactor = 0.8f, scale = 0.92f, accessory = MoleAccessory.Halo, tintCustomPrefab = true },
            new MoleTypeDef { name = "Rojo", color = new Color(0.90f, 0.12f, 0.12f), points = 50, weight = 8f, visibleTimeFactor = 0.6f, scale = 0.85f, accessory = MoleAccessory.Horns, tintCustomPrefab = true },
        };
    }
}
