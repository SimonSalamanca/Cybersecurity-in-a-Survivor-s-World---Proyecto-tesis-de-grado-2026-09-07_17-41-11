using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Guía de tutorial en la esquina superior izquierda.
/// Cada indicación desaparece (con un pequeño desvanecido) cuando el jugador
/// realiza la acción. Las indicaciones de la computadora aparecen solo
/// después de sentarse por primera vez. Al completar todo, el panel se oculta.
/// </summary>
public class TutorialHUD : MonoBehaviour
{
    public JugadorPrimeraPersona jugador;

    [Header("Ajustes")]
    public float umbralRaton = 250f;      // píxeles acumulados para dar por aprendido "mirar"
    public float tiempoCorriendo = 0.6f;  // segundos corriendo para completar "correr"
    public float duracionDesvanecer = 0.6f;

    class Paso
    {
        public string id;
        public GameObject fila;
        public Text texto;
        public bool completado;
    }

    readonly List<Paso> pasos = new List<Paso>();
    RectTransform lista;
    CanvasGroup grupoPanel;
    InputAction accMover, accMirar, accCorrer;
    PuestoInteractivo[] puestos;

    // Progreso
    bool w, a, s, d;
    float ratonAcumulado, corriendoAcumulado;
    bool pasosPCMostrados;

    void Start()
    {
        if (jugador == null) jugador = FindFirstObjectByType<JugadorPrimeraPersona>();
        if (jugador != null && jugador.acciones != null)
        {
            var mapa = jugador.acciones.FindActionMap("Player");
            accMover = mapa.FindAction("Move");
            accMirar = mapa.FindAction("Look");
            accCorrer = mapa.FindAction("Sprint");
        }
        puestos = FindObjectsByType<PuestoInteractivo>(FindObjectsSortMode.None);
        ConstruirPanel();
        AgregarPaso("mover", TextoMover());
        AgregarPaso("mirar", "Mueve el RATON para mirar alrededor");
        AgregarPaso("correr", "Manten [SHIFT] mientras caminas para correr");
        AgregarPaso("sentarse", "Ve al puesto del centro junto a la ventana y pulsa [E] en la silla");
        AgregarPaso("pausa", "Pulsa [ESC] o [P] para pausar el juego");
    }

    void Update()
    {
        if (Time.timeScale == 0f)
        {
            Completar("pausa");
            return;
        }

        // --- Movimiento: cada dirección se marca por separado ---
        if (accMover != null && !Completado("mover"))
        {
            Vector2 m = accMover.ReadValue<Vector2>();
            bool cambio = false;
            if (m.y > 0.5f && !w) { w = true; cambio = true; }
            if (m.y < -0.5f && !s) { s = true; cambio = true; }
            if (m.x < -0.5f && !a) { a = true; cambio = true; }
            if (m.x > 0.5f && !d) { d = true; cambio = true; }
            if (cambio) Texto("mover", TextoMover());
            if (w && a && s && d) Completar("mover");
        }

        // --- Mirar con el ratón ---
        if (accMirar != null && !Completado("mirar"))
        {
            ratonAcumulado += accMirar.ReadValue<Vector2>().magnitude;
            if (ratonAcumulado > umbralRaton) Completar("mirar");
        }

        // --- Correr ---
        if (accCorrer != null && accMover != null && !Completado("correr"))
        {
            bool corriendo = accCorrer.IsPressed() && accMover.ReadValue<Vector2>().y > 0.1f && !jugador.ControlBloqueado;
            if (corriendo) corriendoAcumulado += Time.deltaTime;
            if (corriendoAcumulado > tiempoCorriendo) Completar("correr");
        }

        // --- Interacciones con el puesto ---
        bool sentado = false, usando = false;
        foreach (var p in puestos)
        {
            if (p == null) continue;
            sentado |= p.EstaSentado;
            usando |= p.UsandoComputadora;
        }
        if (sentado)
        {
            Completar("sentarse");
            if (!pasosPCMostrados)
            {
                pasosPCMostrados = true;
                AgregarPaso("usarpc", "Sentado: pulsa [E] para usar la computadora");
                AgregarPaso("levantarse", "Pulsa [Q] para levantarte de la silla");
            }
        }
        if (usando) Completar("usarpc");
        if (pasosPCMostrados && !sentado && Completado("sentarse") && jugador != null && !jugador.ControlBloqueado)
            Completar("levantarse");
    }

    string TextoMover()
    {
        // Las teclas ya pulsadas se muestran apagadas.
        string K(string k, bool hecho) => hecho ? "<color=#2E5E3A>" + k + "</color>" : k;
        return "Muevete con " + K("[W]", w) + " " + K("[A]", a) + " " + K("[S]", s) + " " + K("[D]", d);
    }

    // ---------------- UI ----------------

    void ConstruirPanel()
    {
        var canvasGo = new GameObject("Canvas_Tutorial");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40; // debajo de la pausa (100)
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = new GameObject("Panel_Guia");
        panel.transform.SetParent(canvasGo.transform, false);
        var prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0f, 1f);
        prt.anchoredPosition = new Vector2(32f, -32f);
        var fondo = panel.AddComponent<Image>();
        fondo.color = new Color(0.01f, 0.04f, 0.02f, 0.72f);
        fondo.raycastTarget = false;
        grupoPanel = panel.AddComponent<CanvasGroup>();
        grupoPanel.blocksRaycasts = false;
        var vl = panel.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(22, 22, 16, 18);
        vl.spacing = 8f;
        vl.childControlWidth = true; vl.childControlHeight = true;
        vl.childForceExpandWidth = false; vl.childForceExpandHeight = false;
        var fit = panel.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var titulo = CrearTexto(panel.transform, "C:\\SURVIVOR\\guia> ", 22, TerminalTheme.Blanco);
        titulo.name = "Titulo";
        lista = panel.GetComponent<RectTransform>();
    }

    Text CrearTexto(Transform padre, string contenido, int tam, Color color)
    {
        var t = new GameObject("Texto").AddComponent<Text>();
        t.transform.SetParent(padre, false);
        t.font = TerminalTheme.Fuente();
        t.fontSize = tam;
        t.color = color;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.alignment = TextAnchor.MiddleLeft;
        t.raycastTarget = false;
        t.text = contenido;
        var sombra = t.gameObject.AddComponent<Shadow>();
        sombra.effectColor = new Color(0, 0, 0, 0.8f);
        return t;
    }

    void AgregarPaso(string id, string contenido)
    {
        var fila = new GameObject("Paso_" + id);
        fila.transform.SetParent(lista, false);
        fila.AddComponent<RectTransform>();
        fila.AddComponent<CanvasGroup>();
        var hl = fila.AddComponent<HorizontalLayoutGroup>();
        hl.childControlWidth = true; hl.childControlHeight = true;
        hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
        var t = CrearTexto(fila.transform, "> " + contenido, 24, TerminalTheme.Verde);
        pasos.Add(new Paso { id = id, fila = fila, texto = t });
        if (grupoPanel != null) grupoPanel.alpha = 1f;
        lista.gameObject.SetActive(true);
    }

    Paso Buscar(string id) => pasos.Find(p => p.id == id);
    bool Completado(string id) { var p = Buscar(id); return p == null || p.completado; }

    void Texto(string id, string contenido)
    {
        var p = Buscar(id);
        if (p != null && !p.completado) p.texto.text = "> " + contenido;
    }

    void Completar(string id)
    {
        var p = Buscar(id);
        if (p == null || p.completado) return;
        p.completado = true;
        StartCoroutine(Desvanecer(p));
    }

    IEnumerator Desvanecer(Paso p)
    {
        p.texto.text = "<color=#7CFF9B>[OK]</color> " + p.texto.text.Substring(2);
        var cg = p.fila.GetComponent<CanvasGroup>();
        yield return new WaitForSecondsRealtime(0.35f);
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / duracionDesvanecer)
        {
            cg.alpha = 1f - t;
            yield return null;
        }
        Destroy(p.fila);
        yield return null;
        // Si ya no queda nada pendiente, oculta el panel completo.
        bool quedan = pasos.Exists(x => !x.completado);
        if (!quedan && (pasosPCMostrados || !pasos.Exists(x => x.id == "sentarse")))
            lista.gameObject.SetActive(false);
    }
}
