using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Permite hablar con un NPC: el jugador lo mira y pulsa [E].
/// El contenido (saludo, temas y despedida) sale de DialogosOficina según idPerfil.
/// Recuerda qué temas ya se hablaron y cuántas veces se conversó con él.
/// Mientras dura la conversación el NPC se detiene y mira al jugador (NPCOficina).
/// </summary>
public class DialogoNPC : MonoBehaviour
{
    [Tooltip("Perfil de DialogosOficina (Diana, Julian, Elena, Mateo). Vacío = según el nombre del objeto.")]
    public string idPerfil;
    [Tooltip("Hacia dónde mira la cámara del jugador al conversar. Vacío = hueso Head del modelo.")]
    public Transform cabeza;

    PerfilNPC perfil;
    NPCOficina npc;
    readonly HashSet<int> temasVistos = new HashSet<int>();

    public PerfilNPC Perfil => perfil ??= DialogosOficina.Obtener(idPerfil, name);
    public int Conversaciones { get; set; }
    public bool TemaVisto(int i) => temasVistos.Contains(i);
    public void MarcarVisto(int i) => temasVistos.Add(i);

    /// <summary>Punto al que mira la cámara (los ojos del NPC).</summary>
    public Vector3 PuntoMirada => cabeza != null ? cabeza.position + Vector3.up * 0.06f : transform.position + Vector3.up * 1.55f;

    void Awake()
    {
        npc = GetComponent<NPCOficina>();
        if (cabeza == null)
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == "Head" || t.name.EndsWith(":Head")) { cabeza = t; break; }
    }

    public void AlEmpezar(Transform jugador) { if (npc != null) npc.Conversar(jugador); }
    public void AlTerminar() { if (npc != null) npc.Conversar(null); }
}
