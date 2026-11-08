using UnityEditor;
using UnityEngine;

/// <summary>
/// Los NPCs se crearon a partir del modelo de la protagonista, así que los hombres
/// heredaron el busto. Al importar Assets/Models/NPCs/NPC_Hombre_*.fbx se aplana
/// esa zona del pecho (los modelos de mujer no se tocan).
/// Se hace en la importación para no alterar huesos, pesos ni animaciones, y para
/// que el arreglo se mantenga si el FBX se vuelve a exportar desde Blender.
///
/// Coordenadas de la malla (ejes de Blender): Z arriba, -Y hacia el frente, X a los lados.
/// El busto ocupa z ~1.20-1.36 y |x| ~0.03-0.12, y sobresale hasta y = -0.141
/// cuando el torso plano está en y ~ -0.11.
/// </summary>
public class PechoPlanoNPC : AssetPostprocessor
{
    const float ZMin = 1.16f, ZMax = 1.38f;      // franja afectada (con transición en los bordes)
    const float ZPlenoMin = 1.20f, ZPlenoMax = 1.34f;
    const float XPleno = 0.10f, XMax = 0.145f;   // transición lateral hacia las axilas
    const float FrenteBase = -0.115f;            // profundidad máxima del pecho plano
    const float Pectoral = 0.005f;               // pequeño relieve de pectoral alrededor de z = 1.28

    void OnPostprocessModel(GameObject raiz)
    {
        string ruta = assetPath.Replace('\\', '/');
        if (!ruta.Contains("/Models/NPCs/NPC_Hombre_")) return;

        foreach (var smr in raiz.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (smr.sharedMesh != null) Aplanar(smr.sharedMesh);
    }

    static void Aplanar(Mesh malla)
    {
        var v = malla.vertices;
        var n = malla.normals;
        bool hayNormales = n != null && n.Length == v.Length;
        var frente = new Vector3(0f, -1f, 0f);
        int cambiados = 0;

        for (int i = 0; i < v.Length; i++)
        {
            var p = v[i];
            if (p.y >= 0f || p.z <= ZMin || p.z >= ZMax) continue;
            float ax = Mathf.Abs(p.x);
            if (ax >= XMax) continue;

            float wz = p.z < ZPlenoMin ? Mathf.SmoothStep(0f, 1f, (p.z - ZMin) / (ZPlenoMin - ZMin))
                     : p.z > ZPlenoMax ? Mathf.SmoothStep(0f, 1f, (ZMax - p.z) / (ZMax - ZPlenoMax)) : 1f;
            float wx = ax <= XPleno ? 1f : Mathf.SmoothStep(1f, 0f, (ax - XPleno) / (XMax - XPleno));
            float w = wz * wx;
            if (w <= 0f) continue;

            float relieve = Mathf.Clamp01(1f - Mathf.Abs(p.z - 1.28f) / 0.06f);
            float tope = FrenteBase - Pectoral * relieve;
            if (p.y >= tope) continue;

            v[i].y = Mathf.Lerp(p.y, tope, w);
            if (hayNormales) n[i] = Vector3.Slerp(n[i], frente, w * 0.8f).normalized;
            cambiados++;
        }

        malla.vertices = v;
        if (hayNormales) malla.normals = n;
        malla.RecalculateBounds();
        if (cambiados > 0) Debug.Log($"[PechoPlanoNPC] {malla.name}: {cambiados} vertices del pecho aplanados.");
    }
}
