using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NPC de oficina con una máquina de estados sencilla:
///   Quieto  -> se queda parado unos segundos.
///   Caminar -> va a un punto aleatorio del NavMesh.
///   Sentado -> busca una silla libre (SillaNPC), se sienta, a veces usa la
///              computadora, y luego se levanta.
/// Pasa de un estado a otro al azar usando pesos configurables.
/// Usa los parámetros del Animator: Velocidad, Sentado, UsandoPC.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class NPCOficina : MonoBehaviour
{
    public enum Estado { Quieto, Caminar, Sentado }

    [Header("Referencias")]
    public Animator animator;

    [Header("Probabilidades al cambiar de estado (pesos)")]
    public float pesoQuieto = 1f;
    public float pesoCaminar = 2f;
    public float pesoSentarse = 1.5f;

    [Header("Tiempos (segundos)")]
    public Vector2 tiempoQuieto = new Vector2(3f, 8f);
    public Vector2 tiempoSentado = new Vector2(8f, 20f);
    [Range(0f, 1f)] public float probUsarPC = 0.75f;

    [Header("Zona de paseo (centro y tamaño en XZ)")]
    public Vector3 centroZona = Vector3.zero;
    public Vector2 tamanoZona = new Vector2(14f, 9f);

    [Header("Movimiento")]
    public float velocidad = 1.3f;
    public float velocidadAnimacionCaminar = 1.8f; // velocidad a la que la animación "caminar" se ve natural

    [Header("Depuración")]
    public Estado estadoActual;
    public bool empezarSentado;

    NavMeshAgent agente;
    SillaNPC sillaActual;

    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        agente.speed = velocidad;
        agente.angularSpeed = 360f;
        agente.acceleration = 6f;
        agente.stoppingDistance = 0.1f;
        agente.autoBraking = true;
    }

    void Start()
    {
        // Pequeño desfase para que no todos piensen a la vez.
        StartCoroutine(Cerebro(empezarSentado));
    }

    void Update()
    {
        if (animator == null) return;
        float v = agente.enabled ? agente.velocity.magnitude : 0f;
        animator.SetFloat("Velocidad", v > 0.05f ? 1f : 0f, 0.1f, Time.deltaTime);
        // Ajusta el ritmo de la animación de caminar a la velocidad real.
        animator.speed = (v > 0.05f) ? Mathf.Clamp(v / velocidadAnimacionCaminar * 1.35f, 0.6f, 1.3f) : 1f;
    }

    IEnumerator Cerebro(bool primeroSentarse)
    {
        yield return new WaitForSeconds(Random.Range(0f, 1.5f));
        bool forzarSentado = primeroSentarse;
        while (true)
        {
            Estado siguiente = forzarSentado ? Estado.Sentado : ElegirEstado();
            forzarSentado = false;
            estadoActual = siguiente;
            switch (siguiente)
            {
                case Estado.Quieto: yield return Quieto(); break;
                case Estado.Caminar: yield return Caminar(); break;
                case Estado.Sentado: yield return Sentarse(); break;
            }
        }
    }

    Estado ElegirEstado()
    {
        float total = pesoQuieto + pesoCaminar + pesoSentarse;
        float r = Random.value * total;
        if (r < pesoQuieto) return Estado.Quieto;
        if (r < pesoQuieto + pesoCaminar) return Estado.Caminar;
        return Estado.Sentado;
    }

    // ---------------- Estados ----------------

    IEnumerator Quieto()
    {
        agente.ResetPath();
        // Mira hacia una dirección al azar mientras espera.
        float giro = Random.Range(-120f, 120f);
        Quaternion objetivo = Quaternion.Euler(0, transform.eulerAngles.y + giro, 0);
        float fin = Time.time + Random.Range(tiempoQuieto.x, tiempoQuieto.y);
        while (Time.time < fin)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, objetivo, 90f * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator Caminar()
    {
        if (!PuntoAleatorio(out Vector3 destino)) { yield return Quieto(); yield break; }
        yield return IrA(destino, 25f);
    }

    IEnumerator Sentarse()
    {
        var silla = SillaNPC.ReservarLibre(this, transform.position);
        if (silla == null) { yield return Caminar(); yield break; }
        sillaActual = silla;

        bool llego = false;
        yield return IrA(silla.PuntoAcceso, 30f, r => llego = r);
        if (!llego) { silla.Liberar(this); sillaActual = null; yield break; }

        // Se coloca en la silla.
        agente.ResetPath();
        agente.enabled = false;
        Vector3 p0 = transform.position; Quaternion r0 = transform.rotation;
        Quaternion r1 = Quaternion.Euler(0, silla.YawEscritorio, 0);
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.5f)
        {
            float s = Mathf.SmoothStep(0, 1, t);
            transform.SetPositionAndRotation(Vector3.Lerp(p0, silla.PuntoSentado, s), Quaternion.Slerp(r0, r1, s));
            yield return null;
        }
        transform.SetPositionAndRotation(silla.PuntoSentado, r1);
        animator.SetBool("Sentado", true);
        yield return new WaitForSeconds(2.1f);

        float duracion = Random.Range(tiempoSentado.x, tiempoSentado.y);
        if (Random.value < probUsarPC)
        {
            animator.SetBool("UsandoPC", true);
            silla.EncenderPantalla(true);
            yield return new WaitForSeconds(duracion);
            animator.SetBool("UsandoPC", false);
            silla.EncenderPantalla(false);
            yield return new WaitForSeconds(0.4f);
        }
        else yield return new WaitForSeconds(duracion);

        // Se levanta y vuelve al punto de acceso.
        animator.SetBool("Sentado", false);
        yield return new WaitForSeconds(2.0f);
        p0 = transform.position;
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.5f)
        {
            transform.position = Vector3.Lerp(p0, silla.PuntoAcceso, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        agente.enabled = true;
        agente.Warp(silla.PuntoAcceso);
        silla.Liberar(this);
        sillaActual = null;
    }

    // ---------------- Utilidades ----------------

    IEnumerator IrA(Vector3 destino, float tiempoMax, System.Action<bool> resultado = null)
    {
        if (!agente.enabled) agente.enabled = true;
        agente.isStopped = false;
        if (!agente.SetDestination(destino)) { resultado?.Invoke(false); yield break; }
        float limite = Time.time + tiempoMax;
        while (agente.pathPending) yield return null;
        while (Time.time < limite)
        {
            if (!agente.pathPending && agente.remainingDistance <= agente.stoppingDistance + 0.05f
                && (!agente.hasPath || agente.velocity.sqrMagnitude < 0.01f))
            {
                resultado?.Invoke(true);
                yield break;
            }
            yield return null;
        }
        agente.ResetPath();
        resultado?.Invoke(false);
    }

    bool PuntoAleatorio(out Vector3 punto)
    {
        for (int i = 0; i < 15; i++)
        {
            Vector3 p = centroZona + new Vector3(Random.Range(-tamanoZona.x, tamanoZona.x) * 0.5f, 0f,
                                                 Random.Range(-tamanoZona.y, tamanoZona.y) * 0.5f);
            if (NavMesh.SamplePosition(p, out var hit, 1.0f, NavMesh.AllAreas) && hit.position.y < 0.4f)
            {
                var camino = new NavMeshPath();
                if (agente.CalculatePath(hit.position, camino) && camino.status == NavMeshPathStatus.PathComplete
                    && Vector3.Distance(hit.position, transform.position) > 2f)
                {
                    punto = hit.position;
                    return true;
                }
            }
        }
        punto = transform.position;
        return false;
    }

    void OnDisable()
    {
        if (sillaActual != null) sillaActual.Liberar(this);
    }
}
