using System.Collections;
using UnityEngine;

/// <summary>
/// Puesto de trabajo interactivo (silla + computadora).
/// Flujo: mirar la silla o el computador -> [E] Sentarse.
/// Sentado: [E] Usar computadora / [Q] Levantarse.
/// Usando: [E] Dejar de usar / [Q] Levantarse.
/// Las animaciones se controlan con los parámetros del Animator
/// "Sentado" y "UsandoPC" (ver Protagonista.controller).
/// </summary>
public class PuestoInteractivo : MonoBehaviour
{
    [Header("Dónde queda el jugador al sentarse (posición de los pies, mirando al escritorio)")]
    public Transform anclaSentado;
    [Header("Pantalla del computador (se enciende al usarlo)")]
    public Light luzPantalla;
    public GameObject pantallaEncendida;
    [Header("Objetos que activan la interacción al mirarlos (silla y computador)")]
    public Collider[] objetivos;

    [Header("Ajustes")]
    public float tiempoAcercarse = 0.35f;
    public float duracionSentarse = 1.95f;
    public float duracionLevantarse = 1.95f;
    public float rangoMiradaSentado = 75f;

    enum Estado { Libre, Sentandose, Sentado, UsandoPC, Levantandose }
    Estado estado = Estado.Libre;
    Vector3 posAntes;
    Quaternion rotAntes;

    public bool Disponible => estado == Estado.Libre;
    public bool EstaSentado => estado == Estado.Sentado || estado == Estado.UsandoPC;
    public bool UsandoComputadora => estado == Estado.UsandoPC;

    public bool EsObjetivo(Collider c)
    {
        if (objetivos == null || objetivos.Length == 0) return true;
        foreach (var o in objetivos) if (o == c) return true;
        return false;
    }

    void Start() { EncenderPantalla(false); }

    public string TextoPrompt()
    {
        switch (estado)
        {
            case Estado.Sentado: return "[E] Usar computadora     [Q] Levantarse";
            case Estado.UsandoPC: return "[E] Dejar de usar     [Q] Levantarse";
            default: return null;
        }
    }

    public void Sentarse(JugadorPrimeraPersona j)
    {
        if (estado != Estado.Libre || anclaSentado == null) return;
        StartCoroutine(RutinaSentarse(j));
    }

    public void AlternarComputadora(JugadorPrimeraPersona j)
    {
        if (estado == Estado.Sentado)
        {
            estado = Estado.UsandoPC;
            j.animator.SetBool("UsandoPC", true);
            j.ApuntarCamara(anclaSentado.eulerAngles.y, 18f);
            EncenderPantalla(true);
        }
        else if (estado == Estado.UsandoPC)
        {
            estado = Estado.Sentado;
            j.animator.SetBool("UsandoPC", false);
            EncenderPantalla(false);
        }
    }

    public void Levantarse(JugadorPrimeraPersona j)
    {
        if (estado != Estado.Sentado && estado != Estado.UsandoPC) return;
        StartCoroutine(RutinaLevantarse(j));
    }

    IEnumerator RutinaSentarse(JugadorPrimeraPersona j)
    {
        estado = Estado.Sentandose;
        float yawAncla = anclaSentado.eulerAngles.y;
        j.EntrarEnPuesto(this, yawAncla, rangoMiradaSentado);
        posAntes = j.transform.position;
        rotAntes = j.transform.rotation;

        // Se acerca y gira hacia el escritorio.
        Vector3 p0 = j.transform.position; Quaternion r0 = j.transform.rotation;
        Quaternion r1 = Quaternion.Euler(0f, yawAncla, 0f);
        for (float t = 0; t < 1f; t += Time.deltaTime / tiempoAcercarse)
        {
            float s = Mathf.SmoothStep(0, 1, t);
            j.transform.SetPositionAndRotation(Vector3.Lerp(p0, anclaSentado.position, s), Quaternion.Slerp(r0, r1, s));
            yield return null;
        }
        j.transform.SetPositionAndRotation(anclaSentado.position, r1);
        j.ApuntarCamara(yawAncla, 12f);

        j.animator.SetBool("Sentado", true);
        yield return new WaitForSeconds(duracionSentarse);
        estado = Estado.Sentado;
    }

    IEnumerator RutinaLevantarse(JugadorPrimeraPersona j)
    {
        estado = Estado.Levantandose;
        EncenderPantalla(false);
        j.animator.SetBool("UsandoPC", false);
        j.animator.SetBool("Sentado", false);
        yield return new WaitForSeconds(duracionLevantarse);

        // Vuelve al lugar desde donde se sentó (fuera de la silla) para no quedar atrapado en su colisión.
        Vector3 p0 = j.transform.position;
        for (float t = 0; t < 1f; t += Time.deltaTime / tiempoAcercarse)
        {
            j.transform.position = Vector3.Lerp(p0, posAntes, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        j.transform.position = posAntes;
        j.SalirDePuesto();
        estado = Estado.Libre;
    }

    void EncenderPantalla(bool on)
    {
        if (luzPantalla != null) luzPantalla.enabled = on;
        if (pantallaEncendida != null) pantallaEncendida.SetActive(on);
    }
}
