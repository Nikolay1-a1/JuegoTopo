using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Genera por código los modelos 3D con primitivas (hoyo, topo, martillo).
/// Cualquier material o prefab asignado en el Inspector sustituye al procedural.
/// </summary>
public static class ProceduralModelFactory
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static MaterialPropertyBlock block;
    private static readonly List<Renderer> tintBuffer = new List<Renderer>();

    public static Material Fallback(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material m = new Material(shader);
        m.color = color;
        return m;
    }

    private static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, Color fallbackColor)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        Collider col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat != null ? mat : Fallback(fallbackColor);
        return go;
    }

    /// <summary>Hoyo: disco de césped + apertura de tierra + anillo de bultos.</summary>
    public static void BuildHoleVisual(Transform root, Material dirt, Material rim)
    {
        Part(PrimitiveType.Cylinder, "HoleBase", root, new Vector3(0f, 0.005f, 0f), new Vector3(0.34f, 0.005f, 0.34f), rim, new Color(0.28f, 0.55f, 0.20f));
        Part(PrimitiveType.Cylinder, "HoleOpening", root, new Vector3(0f, 0.0115f, 0f), new Vector3(0.23f, 0.0005f, 0.23f), dirt, new Color(0.18f, 0.10f, 0.05f));
        const int bumps = 12;
        for (int i = 0; i < bumps; i++)
        {
            float a = i * Mathf.PI * 2f / bumps;
            Part(PrimitiveType.Sphere, "RimBump" + i, root, new Vector3(Mathf.Cos(a) * 0.135f, 0.02f, Mathf.Sin(a) * 0.135f), Vector3.one * 0.05f, rim, new Color(0.28f, 0.55f, 0.20f));
        }
    }

    /// <summary>Topo: cuerpo (cápsula), hocico, ojos, orejas y accesorios (halo / cuernos) que se activan según el tipo. Mira hacia +Z.</summary>
    public static GameObject BuildMole(Transform parent, Material body, Material nose, Material eye)
    {
        GameObject root = new GameObject("Mole");
        root.transform.SetParent(parent, false);
        Transform t = root.transform;
        Color brown = new Color(0.55f, 0.35f, 0.20f);
        Part(PrimitiveType.Capsule, "Body", t, Vector3.zero, new Vector3(0.16f, 0.11f, 0.16f), body, brown);
        Part(PrimitiveType.Sphere, "Nose", t, new Vector3(0f, 0.02f, 0.075f), new Vector3(0.05f, 0.04f, 0.05f), nose, new Color(0.95f, 0.55f, 0.60f));
        Part(PrimitiveType.Sphere, "EyeL", t, new Vector3(-0.035f, 0.06f, 0.065f), Vector3.one * 0.025f, eye, new Color(0.05f, 0.05f, 0.05f));
        Part(PrimitiveType.Sphere, "EyeR", t, new Vector3(0.035f, 0.06f, 0.065f), Vector3.one * 0.025f, eye, new Color(0.05f, 0.05f, 0.05f));
        Part(PrimitiveType.Sphere, "EarL", t, new Vector3(-0.065f, 0.105f, 0f), Vector3.one * 0.04f, body, brown);
        Part(PrimitiveType.Sphere, "EarR", t, new Vector3(0.065f, 0.105f, 0f), Vector3.one * 0.04f, body, brown);

        // Halo dorado (topo amarillo)
        GameObject halo = new GameObject("Accessory_Halo");
        halo.transform.SetParent(t, false);
        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.PI * 2f / 10f;
            Part(PrimitiveType.Sphere, "HaloBead" + i, halo.transform, new Vector3(Mathf.Cos(a) * 0.055f, 0.20f, Mathf.Sin(a) * 0.055f), Vector3.one * 0.022f, null, new Color(1f, 0.95f, 0.45f));
        }
        halo.SetActive(false);

        // Cuernos (topo rojo)
        GameObject horns = new GameObject("Accessory_Horns");
        horns.transform.SetParent(t, false);
        for (int s = -1; s <= 1; s += 2)
        {
            GameObject h = Part(PrimitiveType.Capsule, "Horn" + (s < 0 ? "L" : "R"), horns.transform, new Vector3(s * 0.05f, 0.14f, 0.02f), new Vector3(0.025f, 0.03f, 0.025f), null, new Color(0.96f, 0.92f, 0.78f));
            h.transform.localRotation = Quaternion.Euler(0f, 0f, -s * 25f);
        }
        horns.SetActive(false);
        return root;
    }

    public static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform c in root)
        {
            if (c.name == name) return c;
            Transform r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    private static bool IsAccessory(Transform t, Transform stopAt)
    {
        while (t != null && t != stopAt)
        {
            if (t.name.StartsWith("Accessory_")) return true;
            t = t.parent;
        }
        return false;
    }

    /// <summary>Tiñe el topo (cuerpo y orejas) y activa su accesorio según el tipo.</summary>
    public static void ApplyMoleType(Transform mole, MoleTypeDef type)
    {
        if (mole == null || type == null) return;
        if (block == null) block = new MaterialPropertyBlock();

        Renderer[] all = mole.GetComponentsInChildren<Renderer>(true);
        tintBuffer.Clear();
        foreach (Renderer r in all)
            if (r.name == "Body" || r.name == "EarL" || r.name == "EarR") tintBuffer.Add(r);

        bool procedural = tintBuffer.Count > 0;
        if (!procedural)
            foreach (Renderer r in all)
                if (!IsAccessory(r.transform, mole)) tintBuffer.Add(r);

        bool applyTint = procedural || type.tintCustomPrefab;
        foreach (Renderer r in tintBuffer)
        {
            if (!applyTint) { r.SetPropertyBlock(null); continue; }
            block.Clear();
            block.SetColor(BaseColorId, type.color);
            block.SetColor(ColorId, type.color);
            r.SetPropertyBlock(block);
        }

        Transform halo = FindDeep(mole, "Accessory_Halo");
        if (halo != null) halo.gameObject.SetActive(type.accessory == MoleAccessory.Halo);
        Transform horns = FindDeep(mole, "Accessory_Horns");
        if (horns != null) horns.gameObject.SetActive(type.accessory == MoleAccessory.Horns);
    }

    /// <summary>Martillo: el origen está en el punto de impacto (la cabeza), el mango sube por +Y.</summary>
    public static GameObject BuildHammer(Material wood, Material head)
    {
        GameObject root = new GameObject("Hammer");
        GameObject h = Part(PrimitiveType.Cylinder, "Head", root.transform, new Vector3(0f, 0.045f, 0f), new Vector3(0.07f, 0.075f, 0.07f), head, new Color(0.85f, 0.15f, 0.15f));
        h.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Part(PrimitiveType.Cylinder, "Handle", root.transform, new Vector3(0f, 0.185f, 0f), new Vector3(0.025f, 0.14f, 0.025f), wood, new Color(0.60f, 0.40f, 0.20f));
        return root;
    }

    public static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform c in go.transform) SetLayerRecursive(c.gameObject, layer);
    }

    /// <summary>Garantiza un collider para el raycast y pone todo el topo en la capa indicada.</summary>
    public static void EnsureHitbox(GameObject mole, int layer)
    {
        if (mole.GetComponentInChildren<Collider>(true) == null)
        {
            SphereCollider sc = mole.AddComponent<SphereCollider>();
            sc.radius = 0.13f;
            sc.center = new Vector3(0f, 0.03f, 0f);
        }
        SetLayerRecursive(mole, layer);
    }
}
