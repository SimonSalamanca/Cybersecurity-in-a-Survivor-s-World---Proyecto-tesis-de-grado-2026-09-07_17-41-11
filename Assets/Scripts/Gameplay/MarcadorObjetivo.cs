using UnityEngine;

/// <summary>
/// Señal flotante (flecha) que indica al jugador a qué puesto debe ir.
/// Flota y gira suavemente, y se oculta mientras el jugador está
/// sentado o usando ese puesto.
/// </summary>
public class MarcadorObjetivo : MonoBehaviour
{
    public PuestoInteractivo puesto;
    public Transform visual;
    public float amplitud = 0.12f;
    public float frecuencia = 1.6f;
    public float velocidadGiro = 70f;

    Vector3 posBase;

    void Start()
    {
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
        posBase = visual != null ? visual.localPosition : Vector3.zero;
    }

    void Update()
    {
        bool mostrar = puesto == null || puesto.Disponible;
        if (visual != null && visual.gameObject.activeSelf != mostrar) visual.gameObject.SetActive(mostrar);
        if (!mostrar || visual == null) return;
        visual.localPosition = posBase + Vector3.up * Mathf.Sin(Time.time * frecuencia * Mathf.PI) * amplitud;
        visual.Rotate(0f, velocidadGiro * Time.deltaTime, 0f, Space.World);
    }
}
