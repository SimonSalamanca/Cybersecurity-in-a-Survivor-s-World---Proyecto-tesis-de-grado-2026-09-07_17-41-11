using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Silla que pueden usar los NPCs. Calcula automáticamente dónde pararse
/// (punto de acceso, detrás de la silla) y dónde quedar sentado,
/// a partir de la silla y el computador de su cubículo.
/// Lleva un registro global de sillas libres/ocupadas.
/// </summary>
public class SillaNPC : MonoBehaviour
{
    public Transform silla;
    public Transform computador;
    [Tooltip("Distancia hacia el escritorio respecto al centro de la silla (la animación lleva la cadera hacia atrás).")]
    public float adelantoSentado = 0.18f;
    public float distanciaAcceso = 0.7f;
    public GameObject pantalla;

    static readonly List<SillaNPC> todas = new List<SillaNPC>();
    NPCOficina ocupante;

    public Vector3 PuntoSentado { get; private set; }
    public Vector3 PuntoAcceso { get; private set; }
    public float YawEscritorio { get; private set; }
    public bool Libre => ocupante == null;

    void OnEnable() { if (!todas.Contains(this)) todas.Add(this); Calcular(); EncenderPantalla(false); }
    void OnDisable() { todas.Remove(this); }

    void Calcular()
    {
        if (silla == null || computador == null) return;
        Vector3 cs = Centro(silla), cc = Centro(computador);
        Vector3 dir = cc - cs; dir.y = 0f; dir.Normalize();
        YawEscritorio = Quaternion.LookRotation(dir).eulerAngles.y;
        PuntoSentado = new Vector3(cs.x, 0f, cs.z) + dir * adelantoSentado;
        PuntoAcceso = new Vector3(cs.x, 0f, cs.z) - dir * distanciaAcceso;
    }

    static Vector3 Centro(Transform t)
    {
        var r = t.GetComponent<Renderer>();
        return r != null ? r.bounds.center : t.position;
    }

    public static SillaNPC ReservarLibre(NPCOficina npc, Vector3 desde)
    {
        var libres = todas.FindAll(s => s.Libre);
        if (libres.Count == 0) return null;
        var s = libres[Random.Range(0, libres.Count)];
        s.ocupante = npc;
        return s;
    }

    public void Liberar(NPCOficina npc)
    {
        if (ocupante == npc) ocupante = null;
        EncenderPantalla(false);
    }

    public void EncenderPantalla(bool on)
    {
        if (pantalla != null) pantalla.SetActive(on);
    }

    void OnDrawGizmosSelected()
    {
        Calcular();
        Gizmos.color = Color.green; Gizmos.DrawWireSphere(PuntoSentado, 0.15f);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(PuntoAcceso, 0.15f);
    }
}
