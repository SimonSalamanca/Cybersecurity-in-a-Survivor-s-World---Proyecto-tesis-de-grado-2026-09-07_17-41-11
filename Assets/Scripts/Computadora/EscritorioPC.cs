using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Escritorio simulado del computador del protagonista (canvas World Space
/// sobre el monitor). Fondo con la empresa, iconos de aplicaciones, ventanas
/// con pestaña en la barra de tareas, reloj y notificaciones emergentes.
/// En la Oficina (tutorial) el Correo es funcional; CMD y Estado se pueden
/// abrir para conocerlos pero están bloqueados, y el Bestiario es de consulta.
/// Se construye por código la primera vez que se enciende la pantalla.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class EscritorioPC : MonoBehaviour
{
    public const float Ancho = 1280f, Alto = 728f, AltoBarra = 44f;
    public const string IdCorreo = "Correo", IdCMD = "CMD", IdEstado = "Estado", IdBestiario = "Bestiario";

    [Header("Identidad del equipo")]
    public string empresa = "VIGIA CONSULTORES";
    public string lema = "Auditoria de ingenieria social";
    public string usuario = "auditor";

    [Header("Notificaciones")]
    public float duracionNotificacion = 5f;

    public AppCorreo Correo { get; private set; }

    class Ventana
    {
        public GameObject ventana, pestana;
        public RectTransform cuerpo;
    }

    readonly Dictionary<string, Ventana> ventanas = new Dictionary<string, Ventana>();
    RectTransform fondo, capaVentanas, pestanas;
    GameObject notificacion, insignia;
    Text reloj, textoInsignia, tituloNotificacion, textoNotificacion;
    float ocultarNotificacionEn, siguienteIconoY = -16f;
    System.Action alClicNotificacion;

    void Awake()
    {
        // Lienzo de 1280x728 conservando el ancho físico de la pantalla.
        var rt = (RectTransform)transform;
        if (!Mathf.Approximately(rt.sizeDelta.x, Ancho))
        {
            rt.localScale *= rt.sizeDelta.x / Ancho;
            rt.sizeDelta = new Vector2(Ancho, Alto);
        }
        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 2f;
        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

        // El texto fijo de arranque se reemplaza por el escritorio.
        UIPC.Vaciar(transform);
        Construir();

        CrearApp(IdCorreo, "Correo", "@", "C:\\SURVIVOR\\correo> Bandeja de entrada", "@ Correo");
        CrearApp(IdCMD, "CMD", ">_", "C:\\SURVIVOR\\cmd> Modulo de ataque", ">_ CMD");
        CrearApp(IdEstado, "Estado", "%", "C:\\SURVIVOR\\estado> Componentes del equipo", "% Estado");
        CrearApp(IdBestiario, "Bestiario", "<!>", "C:\\SURVIVOR\\bestiario> Amenazas conocidas", "<!> Bestiario");
        CrearInsigniaCorreo();

        Correo = gameObject.AddComponent<AppCorreo>();
        Correo.Inicializar(this, ventanas[IdCorreo].cuerpo);
        gameObject.AddComponent<AppCMD>().Inicializar(ventanas[IdCMD].cuerpo);
        gameObject.AddComponent<AppEstado>().Inicializar(ventanas[IdEstado].cuerpo);
        gameObject.AddComponent<AppBestiario>().Inicializar(ventanas[IdBestiario].cuerpo);
    }

    void Update()
    {
        reloj.text = System.DateTime.Now.ToString("HH:mm") + "   " + usuario;
        if (notificacion.activeSelf && Time.time >= ocultarNotificacionEn) notificacion.SetActive(false);
    }

    // ---------------- API ----------------

    /// <summary>Abre la ventana (o la trae al frente) y muestra su pestaña.</summary>
    public void Abrir(string id)
    {
        var v = ventanas[id];
        v.ventana.SetActive(true);
        v.ventana.transform.SetAsLastSibling();
        v.pestana.SetActive(true);
        if (id == IdCorreo) Correo.AlAbrir();
    }

    /// <summary>Cierra la ventana y quita su pestaña.</summary>
    public void Cerrar(string id)
    {
        ventanas[id].ventana.SetActive(false);
        ventanas[id].pestana.SetActive(false);
    }

    // La pestaña minimiza la ventana si ya está al frente; si no, la trae al frente.
    void AlternarDesdePestana(string id)
    {
        var v = ventanas[id];
        bool alFrente = v.ventana.activeSelf && v.ventana.transform.GetSiblingIndex() == capaVentanas.childCount - 1;
        if (alFrente) v.ventana.SetActive(false);
        else Abrir(id);
    }

    public void Notificar(string titulo, string texto, System.Action alClic)
    {
        tituloNotificacion.text = titulo;
        textoNotificacion.text = texto;
        alClicNotificacion = alClic;
        notificacion.SetActive(true);
        ocultarNotificacionEn = Time.time + duracionNotificacion;
    }

    public void ActualizarInsignia(int noLeidos)
    {
        insignia.SetActive(noLeidos > 0);
        textoInsignia.text = noLeidos.ToString();
    }

    // ---------------- Construcción ----------------

    void Construir()
    {
        fondo = UIPC.Llenar(UIPC.Panel("Fondo", transform, new Color(0.012f, 0.03f, 0.02f, 1f)));

        var logo = UIPC.Texto("Logo", fondo, empresa, 64, new Color(0.2f, 1f, 0.4f, 0.14f), TextAnchor.MiddleCenter);
        UIPC.Anclar(logo.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(150, 10), new Vector2(0, 90));
        var subt = UIPC.Texto("Lema", fondo, lema.ToUpper(), 22, new Color(0.2f, 1f, 0.4f, 0.12f), TextAnchor.MiddleCenter);
        UIPC.Anclar(subt.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(150, -40), new Vector2(0, 0));

        capaVentanas = UIPC.Anclar(UIPC.Panel("Ventanas", transform, Color.clear), Vector2.zero, Vector2.one, new Vector2(0, AltoBarra), Vector2.zero);
        CrearBarraTareas();
        CrearNotificacion();
    }

    void CrearApp(string id, string etiqueta, string glifo, string titulo, string textoPestana)
    {
        CrearIcono(id, etiqueta, glifo);

        var v = UIPC.Panel("Ventana_" + id, capaVentanas, TerminalTheme.FondoVentana);
        UIPC.Anclar(v, Vector2.zero, Vector2.one, new Vector2(146, 12), new Vector2(-12, -12));
        UIPC.Borde(v, TerminalTheme.Verde);
        var barra = UIPC.FilaArriba(UIPC.Panel("BarraTitulo", v, TerminalTheme.BarraTitulo), 0, 36);
        var t = UIPC.Texto("Titulo", barra, titulo, 18, TerminalTheme.Verde, TextAnchor.MiddleLeft);
        UIPC.Anclar(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 0), new Vector2(-60, 0));
        var cerrar = UIPC.Boton("Cerrar", barra, "X", 18, () => Cerrar(id));
        UIPC.Anclar((RectTransform)cerrar.transform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-46, 4), new Vector2(-6, -4));
        var cuerpo = UIPC.Anclar(UIPC.Panel("Cuerpo", v, Color.clear), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -36));
        v.gameObject.SetActive(false);

        var pestana = UIPC.Boton("Pestana_" + id, pestanas, textoPestana, 16, () => AlternarDesdePestana(id));
        pestana.gameObject.AddComponent<LayoutElement>().preferredWidth = 172;
        pestana.gameObject.SetActive(false);

        ventanas[id] = new Ventana { ventana = v.gameObject, pestana = pestana.gameObject, cuerpo = cuerpo };
    }

    void CrearIcono(string id, string etiqueta, string glifo)
    {
        var icono = UIPC.Panel("Icono_" + id, fondo, Color.clear);
        icono.gameObject.AddComponent<Image>().color = Color.clear; // zona de clic transparente
        icono.anchorMin = icono.anchorMax = icono.pivot = new Vector2(0, 1);
        icono.anchoredPosition = new Vector2(16, siguienteIconoY);
        icono.sizeDelta = new Vector2(116, 136);
        siguienteIconoY -= 146f;

        var caja = UIPC.Panel("Caja", icono, Color.white); // el tono lo da el ColorTint del botón
        caja.anchorMin = caja.anchorMax = new Vector2(0.5f, 1f);
        caja.pivot = new Vector2(0.5f, 1f);
        caja.anchoredPosition = new Vector2(0, -6);
        caja.sizeDelta = new Vector2(74, 74);
        UIPC.Borde(caja, TerminalTheme.Verde);
        UIPC.Llenar(UIPC.Texto("Glifo", caja, glifo, glifo.Length > 2 ? 30 : 40, TerminalTheme.Verde, TextAnchor.MiddleCenter).rectTransform);

        var texto = UIPC.Texto("Etiqueta", icono, etiqueta, 16, TerminalTheme.Blanco, TextAnchor.UpperCenter);
        UIPC.Anclar(texto.rectTransform, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 48));

        var boton = icono.gameObject.AddComponent<Button>();
        boton.targetGraphic = caja.GetComponent<Image>();
        var colores = boton.colors;
        colores.normalColor = colores.selectedColor = TerminalTheme.Boton;
        colores.highlightedColor = TerminalTheme.BotonHover;
        colores.pressedColor = TerminalTheme.VerdeTenue;
        boton.colors = colores;
        boton.onClick.AddListener(() => Abrir(id));
    }

    void CrearInsigniaCorreo()
    {
        var caja = fondo.Find("Icono_" + IdCorreo + "/Caja");
        insignia = UIPC.Panel("Insignia", caja, UIPC.Rojo).gameObject;
        var irt = (RectTransform)insignia.transform;
        irt.anchorMin = irt.anchorMax = new Vector2(1, 1);
        irt.anchoredPosition = new Vector2(2, -2);
        irt.sizeDelta = new Vector2(28, 28);
        textoInsignia = UIPC.Texto("Numero", irt, "0", 17, Color.white, TextAnchor.MiddleCenter);
        UIPC.Llenar(textoInsignia.rectTransform);
        insignia.SetActive(false);
    }

    void CrearBarraTareas()
    {
        var barra = UIPC.Panel("BarraTareas", transform, TerminalTheme.BarraTitulo);
        UIPC.Anclar(barra, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, AltoBarra));
        var linea = UIPC.Panel("Linea", barra, TerminalTheme.VerdeTenue);
        UIPC.Anclar(linea, new Vector2(0, 1), Vector2.one, new Vector2(0, -2), Vector2.zero);

        var so = UIPC.Texto("SO", barra, "[#] SURVIVOR OS", 18, TerminalTheme.Verde, TextAnchor.MiddleLeft);
        UIPC.Anclar(so.rectTransform, Vector2.zero, new Vector2(0, 1), new Vector2(14, 0), new Vector2(200, 0));

        pestanas = UIPC.Anclar(UIPC.Panel("Pestanas", barra, Color.clear), Vector2.zero, Vector2.one, new Vector2(210, 5), new Vector2(-250, -5));
        var hl = pestanas.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 6f;
        hl.childControlWidth = hl.childControlHeight = true;
        hl.childForceExpandWidth = false; hl.childForceExpandHeight = true;

        reloj = UIPC.Texto("Reloj", barra, "", 18, TerminalTheme.Blanco, TextAnchor.MiddleRight);
        UIPC.Anclar(reloj.rectTransform, new Vector2(1, 0), Vector2.one, new Vector2(-240, 0), new Vector2(-16, 0));
    }

    void CrearNotificacion()
    {
        var n = UIPC.Panel("Notificacion", transform, TerminalTheme.FondoVentana);
        n.anchorMin = n.anchorMax = n.pivot = new Vector2(1, 0);
        n.anchoredPosition = new Vector2(-14, AltoBarra + 14);
        n.sizeDelta = new Vector2(430, 96);
        UIPC.Borde(n, TerminalTheme.Verde);
        tituloNotificacion = UIPC.Texto("Titulo", n, "", 16, TerminalTheme.Verde);
        UIPC.FilaArriba(tituloNotificacion.rectTransform, 10, 22, 14);
        textoNotificacion = UIPC.Texto("Texto", n, "", 17, TerminalTheme.Blanco);
        UIPC.Anclar(textoNotificacion.rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 8), new Vector2(-14, -36));
        n.gameObject.AddComponent<Button>().onClick.AddListener(() =>
        {
            notificacion.SetActive(false);
            alClicNotificacion?.Invoke();
        });
        notificacion = n.gameObject;
        notificacion.SetActive(false);
    }
}
