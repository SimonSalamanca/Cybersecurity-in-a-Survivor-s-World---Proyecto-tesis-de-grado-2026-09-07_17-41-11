using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Caja de diálogo con los NPCs (parte inferior de la pantalla).
/// Flujo: saludo -> menú de temas -> líneas del tema -> menú ... -> despedida.
/// Las líneas se escriben letra a letra; [E] / [ESPACIO] / clic completan la línea
/// o pasan a la siguiente. En el menú se elige con clic o con las teclas 1..9
/// ([Q] = despedirse). La entrada la reenvía JugadorPrimeraPersona cada frame.
/// </summary>
public class DialogoHUD : MonoBehaviour
{
    public JugadorPrimeraPersona jugador;
    public float caracteresPorSegundo = 50f;

    public bool Activo => npc != null;
    public DialogoNPC Interlocutor => npc;
    public int ConversacionesIniciadas { get; private set; }

    static readonly Color ColorJugador = new Color(1f, 0.72f, 0.2f, 1f);
    static readonly Color ColorSistema = new Color(1f, 0.32f, 0.28f, 1f);

    DialogoNPC npc;
    GameObject raiz;
    Outline bordeCaja;
    Text nombre, cuerpo, pista;
    RectTransform opciones;
    readonly List<Button> botones = new List<Button>();
    int frameInicio;
    bool enMenu, pulsoAvanzar;
    int eleccion = -1;

    void Awake() => Construir();

    public void Iniciar(DialogoNPC n)
    {
        if (Activo || n == null) return;
        npc = n;
        ConversacionesIniciadas++;
        frameInicio = Time.frameCount;
        raiz.SetActive(true);
        npc.AlEmpezar(jugador != null ? jugador.transform : null);
        StartCoroutine(Conversacion());
    }

    /// <summary>Llamado por el jugador cada frame mientras el diálogo está activo.</summary>
    public void ProcesarEntrada(bool pulsoInteractuar)
    {
        if (!Activo || Time.frameCount == frameInicio) return;
        var kb = Keyboard.current;
        var raton = Mouse.current;
        if (enMenu)
        {
            if (kb == null) return;
            for (int i = 0; i < botones.Count && i < 9; i++)
                if (kb[Key.Digit1 + i].wasPressedThisFrame || kb[Key.Numpad1 + i].wasPressedThisFrame) eleccion = i;
            if (kb.qKey.wasPressedThisFrame) eleccion = botones.Count - 1;
            return;
        }
        if (pulsoInteractuar
            || (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
            || (raton != null && raton.leftButton.wasPressedThisFrame))
            pulsoAvanzar = true;
    }

    // ---------------- Flujo ----------------

    IEnumerator Conversacion()
    {
        var p = npc.Perfil;
        bool primeraVez = npc.Conversaciones == 0;
        npc.Conversaciones++;
        yield return Lineas(primeraVez ? p.saludo : p.saludoRepetido);
        while (true)
        {
            yield return Menu(p);
            if (eleccion >= p.temas.Length) break;
            int tema = eleccion;
            yield return Lineas(p.temas[tema].lineas);
            npc.MarcarVisto(tema);
        }
        yield return Lineas(p.despedida);
        Terminar();
    }

    IEnumerator Lineas(LineaDialogo[] lineas)
    {
        if (lineas == null) yield break;
        foreach (var l in lineas) yield return Linea(l);
    }

    IEnumerator Linea(LineaDialogo l)
    {
        MostrarHablante(l.hablante);
        opciones.gameObject.SetActive(false);
        cuerpo.gameObject.SetActive(true);
        pista.text = "";
        yield return null; // que la pulsación que abrió esta línea no la salte
        pulsoAvanzar = false;

        string s = l.texto;
        float n = 0f;
        while (n < s.Length)
        {
            if (pulsoAvanzar) { pulsoAvanzar = false; break; }
            n += Time.deltaTime * caracteresPorSegundo;
            int k = Mathf.Min(s.Length, (int)n);
            // El resto va transparente para que las líneas no salten al ajustarse.
            cuerpo.text = s.Substring(0, k) + "<color=#00000000>" + s.Substring(k) + "</color>";
            yield return null;
        }
        cuerpo.text = s;
        pista.text = "[E] Continuar";
        while (!pulsoAvanzar) yield return null;
        pulsoAvanzar = false;
    }

    IEnumerator Menu(PerfilNPC p)
    {
        MostrarHablante(Hablante.NPC);
        cuerpo.gameObject.SetActive(false);
        pista.text = "[1-" + (p.temas.Length + 1) + "] o clic para elegir     [Q] Despedirse";
        UIPC.Vaciar(opciones);
        botones.Clear();
        for (int i = 0; i <= p.temas.Length; i++)
        {
            int indice = i;
            bool despedida = i == p.temas.Length;
            string etiqueta = (i + 1) + ". " + (despedida ? "Despedirse" : p.temas[i].titulo);
            if (!despedida && npc.TemaVisto(i)) etiqueta = "<color=#5E8C6C>" + etiqueta + "   (ya hablado)</color>";
            var b = UIPC.Boton("Opcion_" + i, opciones, etiqueta, 22, () => eleccion = indice);
            b.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
            b.gameObject.AddComponent<LayoutElement>().preferredHeight = 42;
            botones.Add(b);
        }
        opciones.gameObject.SetActive(true);
        eleccion = -1;
        enMenu = true;
        while (eleccion < 0) yield return null;
        enMenu = false;
    }

    void Terminar()
    {
        var n = npc;
        npc = null;
        enMenu = false;
        raiz.SetActive(false);
        n.AlTerminar();
        if (jugador != null) jugador.FinDialogo();
    }

    void MostrarHablante(Hablante h)
    {
        var p = npc.Perfil;
        switch (h)
        {
            case Hablante.Jugador:
                nombre.text = "TU";
                nombre.color = cuerpo.color = ColorJugador;
                bordeCaja.effectColor = TerminalTheme.VerdeTenue;
                break;
            case Hablante.Sistema:
                nombre.text = "[IMPLANTE NEURAL]  //  registro de " + p.nombre;
                nombre.color = cuerpo.color = ColorSistema;
                bordeCaja.effectColor = ColorSistema;
                break;
            default:
                nombre.text = p.nombre.ToUpper() + "  //  " + p.cargo;
                nombre.color = TerminalTheme.Verde;
                cuerpo.color = TerminalTheme.Blanco;
                bordeCaja.effectColor = TerminalTheme.VerdeTenue;
                break;
        }
    }

    // ---------------- UI ----------------

    void Construir()
    {
        raiz = new GameObject("Canvas_Dialogo");
        raiz.transform.SetParent(transform, false);
        var canvas = raiz.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60; // sobre el HUD y el tutorial, debajo de la pausa (100)
        var scaler = raiz.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        raiz.AddComponent<GraphicRaycaster>();

        var caja = UIPC.Panel("Caja", raiz.transform, new Color(0.008f, 0.03f, 0.016f, 1f)); // opaca: el Outline copia el fondo y lo teñiría
        caja.anchorMin = caja.anchorMax = caja.pivot = new Vector2(0.5f, 0f);
        caja.anchoredPosition = new Vector2(0, 36);
        caja.sizeDelta = new Vector2(1320, 300);
        bordeCaja = caja.gameObject.AddComponent<Outline>();
        bordeCaja.effectColor = TerminalTheme.VerdeTenue;
        bordeCaja.effectDistance = new Vector2(2, -2);

        var barra = UIPC.FilaArriba(UIPC.Panel("Barra_Nombre", caja, TerminalTheme.BarraTitulo), 0, 46);
        nombre = UIPC.Texto("Nombre", barra, "", 22, TerminalTheme.Verde, TextAnchor.MiddleLeft);
        UIPC.Llenar(nombre.rectTransform).offsetMin = new Vector2(22, 0);
        nombre.fontStyle = FontStyle.Bold;

        cuerpo = UIPC.Texto("Cuerpo", caja, "", 26, TerminalTheme.Blanco);
        UIPC.Anclar(cuerpo.rectTransform, Vector2.zero, Vector2.one, new Vector2(30, 48), new Vector2(-30, -64));
        cuerpo.lineSpacing = 1.1f;

        opciones = UIPC.Panel("Opciones", caja, Color.clear);
        UIPC.Anclar(opciones, Vector2.zero, Vector2.one, new Vector2(24, 44), new Vector2(-24, -58));
        var vlg = opciones.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        pista = UIPC.Texto("Pista", caja, "", 18, TerminalTheme.Gris, TextAnchor.MiddleRight);
        UIPC.Anclar(pista.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(24, 8), new Vector2(-24, 40));

        raiz.SetActive(false);
    }
}
