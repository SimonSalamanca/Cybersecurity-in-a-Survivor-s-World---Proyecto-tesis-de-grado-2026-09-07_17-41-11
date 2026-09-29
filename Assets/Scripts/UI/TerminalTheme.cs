using UnityEngine;

/// <summary>
/// Tema visual terminal retro (estilo PowerShell/CRT verde fósforo):
/// fondo casi negro, verde brillante, fuente monospace, marcos ASCII,
/// banner en arte ASCII, barras de estado y cursores parpadeantes.
/// Solo estética: no posiciona ni mueve objetos existentes.
/// </summary>
public static class TerminalTheme
{
    public static readonly Color Fondo = new Color(0.02f, 0.031f, 0.02f, 1f);
    public static readonly Color FondoVentana = new Color(0.008f, 0.02f, 0.012f, 0.98f);
    public static readonly Color Verde = new Color(0.2f, 1f, 0.4f, 1f);
    public static readonly Color VerdeTenue = new Color(0.25f, 0.55f, 0.35f, 1f);
    public static readonly Color Blanco = new Color(0.85f, 1f, 0.88f, 1f);
    public static readonly Color Gris = new Color(0.45f, 0.6f, 0.5f, 1f);
    public static readonly Color BarraTitulo = new Color(0.02f, 0.05f, 0.03f, 1f);
    public static readonly Color Boton = new Color(0.02f, 0.09f, 0.05f, 1f);
    public static readonly Color BotonHover = new Color(0.03f, 0.16f, 0.08f, 1f);

    static Font fuenteCache;

    public static Font Fuente()
    {
        if (fuenteCache != null) return fuenteCache;
        // Monospace real del SO para que el arte ASCII alinee perfecto.
        try { fuenteCache = Font.CreateDynamicFontFromOSFont("Consolas", 16); } catch { }
        if (fuenteCache == null)
        {
            try { fuenteCache = Font.CreateDynamicFontFromOSFont("Lucida Console", 16); } catch { }
        }
        if (fuenteCache == null)
            fuenteCache = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return fuenteCache;
    }

    public static UnityEngine.UI.Text CrearTexto(string nombre, Transform padre, string contenido, int tamano, Color color)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var t = go.AddComponent<UnityEngine.UI.Text>();
        t.text = contenido;
        t.fontSize = tamano;
        t.color = color;
        t.font = Fuente();
        t.alignment = TextAnchor.MiddleLeft;
        return t;
    }

    public static UnityEngine.UI.Text CrearTextoCentrado(string nombre, Transform padre, string contenido, int tamano, Color color, Vector2 pos, Vector2 tam)
    {
        var t = CrearTexto(nombre, padre, contenido, tamano, color);
        t.alignment = TextAnchor.MiddleCenter;
        var rt = t.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        return t;
    }

    public static UnityEngine.UI.Button CrearBoton(string nombre, Transform padre, string etiqueta, UnityEngine.Events.UnityAction onClick, Vector2 tamano, int fsEtiqueta = 20)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = Boton;
        var btn = go.AddComponent<UnityEngine.UI.Button>();
        var colors = btn.colors;
        colors.highlightedColor = BotonHover;
        btn.colors = colors;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = tamano;
        var lgo = new GameObject("Etiqueta");
        lgo.transform.SetParent(go.transform, false);
        var t = lgo.AddComponent<UnityEngine.UI.Text>();
        t.text = etiqueta;
        t.fontSize = fsEtiqueta;
        t.alignment = TextAnchor.MiddleLeft;
        t.font = Fuente();
        t.color = Verde;
        var lrt = lgo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(16, 0); lrt.offsetMax = Vector2.zero;
        if (onClick != null) btn.onClick.AddListener(onClick);
        return btn;
    }

    public static UnityEngine.UI.Slider CrearSlider(string nombre, Transform padre, float min, float max, float valor, Vector2 tamano)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = tamano;
        var slider = go.AddComponent<UnityEngine.UI.Slider>();
        slider.minValue = min; slider.maxValue = max; slider.value = valor;
        slider.transition = UnityEngine.UI.Selectable.Transition.None;
        // Pista
        var bg = new GameObject("Pista");
        bg.transform.SetParent(go.transform, false);
        var bgi = bg.AddComponent<UnityEngine.UI.Image>();
        bgi.color = new Color(0.03f, 0.09f, 0.05f, 1f);
        var bgrt = bg.GetComponent<RectTransform>();
        bgrt.anchorMin = new Vector2(0, 0.35f); bgrt.anchorMax = new Vector2(1, 0.65f);
        bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;
        // Relleno verde
        var fillArea = new GameObject("AreaRelleno");
        fillArea.transform.SetParent(go.transform, false);
        var fart = fillArea.AddComponent<RectTransform>();
        fart.anchorMin = new Vector2(0, 0.35f); fart.anchorMax = new Vector2(1, 0.65f);
        fart.offsetMin = Vector2.zero; fart.offsetMax = Vector2.zero;
        var fill = new GameObject("Relleno");
        fill.transform.SetParent(fillArea.transform, false);
        var fi = fill.AddComponent<UnityEngine.UI.Image>();
        fi.color = Verde;
        var firt = fill.GetComponent<RectTransform>();
        firt.anchorMin = Vector2.zero; firt.anchorMax = new Vector2(0, 1);
        firt.offsetMin = Vector2.zero; firt.offsetMax = Vector2.zero;
        slider.fillRect = firt;
        // Handle
        var slideArea = new GameObject("AreaHandle");
        slideArea.transform.SetParent(go.transform, false);
        var sart = slideArea.AddComponent<RectTransform>();
        sart.anchorMin = Vector2.zero; sart.anchorMax = Vector2.one;
        sart.offsetMin = Vector2.zero; sart.offsetMax = Vector2.zero;
        var handle = new GameObject("Handle");
        handle.transform.SetParent(slideArea.transform, false);
        var hi = handle.AddComponent<UnityEngine.UI.Image>();
        hi.color = Verde;
        var hrt = handle.GetComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(16, 26);
        slider.handleRect = hrt;
        slider.targetGraphic = hi;
        return slider;
    }

    public static UnityEngine.UI.Toggle CrearToggle(string nombre, Transform padre, string etiqueta, bool valor)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var toggle = go.AddComponent<UnityEngine.UI.Toggle>();
        var fondo = new GameObject("Fondo");
        fondo.transform.SetParent(go.transform, false);
        var fi = fondo.AddComponent<UnityEngine.UI.Image>();
        fi.color = new Color(0.04f, 0.16f, 0.08f, 1f);
        var frt = fondo.GetComponent<RectTransform>();
        frt.sizeDelta = new Vector2(24, 24);
        toggle.targetGraphic = fi;
        var tick = new GameObject("Tick");
        tick.transform.SetParent(fondo.transform, false);
        var ti = tick.AddComponent<UnityEngine.UI.Image>();
        ti.color = Verde;
        var trt = tick.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.2f, 0.2f); trt.anchorMax = new Vector2(0.8f, 0.8f);
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        toggle.graphic = ti;
        var lab = new GameObject("Etiqueta");
        lab.transform.SetParent(go.transform, false);
        var t = lab.AddComponent<UnityEngine.UI.Text>();
        t.text = etiqueta; t.fontSize = 20; t.color = Blanco; t.font = Fuente();
        var lrt = lab.GetComponent<RectTransform>();
        lrt.anchoredPosition = new Vector2(90, 0); lrt.sizeDelta = new Vector2(500, 30);
        toggle.isOn = valor;
        var grt = go.GetComponent<RectTransform>();
        grt.sizeDelta = new Vector2(560, 32);
        return toggle;
    }

    // ---------- Estética retro: banner, marcos, barras ----------

    const string BannerArt =
        "  _____   _____ ___ ___  ___ ___ ___ _   _ ___ ___ _______   __\n" +
        " / __\\ \\ / / _ ) __| _ \\/ __| __/ __| | | | _ \\_ _|_   _\\ \\ / /\n" +
        "| (__ \\ V /| _ \\ _||   /\\__ \\ _| (__| |_| |   /| |  | |  \\ V /\n" +
        " \\___| |_| |___/___|_|_\\|___/___\\___|\\___/|_|_\\___| |_|   |_|";

    const string BannerSubArt =
        "___           __    _    ___    _  _   __        _  _    _\n" +
        " | |\\ |  /\\  (_ | ||_)\\  /|\\  // \\|_)/(_  \\    // \\|_)| | \\\n" +
        "_|_| \\| /--\\ __)|_|| \\ \\/_|_\\/ \\_/| \\ __)  \\/\\/ \\_/| \\|_|_/";

    /// <summary>Banner en tipografía ASCII (título + subtítulo) y versión.</summary>
    public static void CrearBanner(Transform padre, Vector2 pos, bool conNombre = true)
    {
        var b = CrearTexto("Banner", padre, BannerArt, 12, Verde);
        b.alignment = TextAnchor.MiddleCenter;
        var brt = b.GetComponent<RectTransform>();
        brt.anchoredPosition = pos + new Vector2(0, 14);
        brt.sizeDelta = new Vector2(560, 70);
        if (conNombre)
        {
            var b2 = CrearTexto("BannerSub", padre, BannerSubArt, 12, Verde);
            b2.alignment = TextAnchor.MiddleCenter;
            var b2rt = b2.GetComponent<RectTransform>();
            b2rt.anchoredPosition = pos + new Vector2(0, -46);
            b2rt.sizeDelta = new Vector2(560, 52);
        }
        var v = CrearTexto("Version", padre, "v1.0", 12, VerdeTenue);
        v.alignment = TextAnchor.MiddleRight;
        var vrt = v.GetComponent<RectTransform>();
        vrt.anchoredPosition = new Vector2(295, 128);
        vrt.sizeDelta = new Vector2(80, 20);
    }

    /// <summary>Barra horizontal tipo "-# |---...---| #-".</summary>
    public static UnityEngine.UI.Text CrearMarcoH(string nombre, Transform padre, Vector2 pos, int anchoChars)
    {
        string barra = "-# |" + new string('-', Mathf.Max(4, anchoChars)) + "| #-";
        var t = CrearTexto(nombre, padre, barra, 13, VerdeTenue);
        t.alignment = TextAnchor.MiddleCenter;
        var rt = t.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(640, 20);
        return t;
    }

    /// <summary>Riel vertical de glifos (">" o "+"), una columna multilínea.</summary>
    public static UnityEngine.UI.Text CrearRiel(string nombre, Transform padre, Vector2 pos, int filas, string glifo)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < filas; i++)
        {
            if (i > 0) sb.Append('\n');
            sb.Append(glifo);
        }
        var t = CrearTexto(nombre, padre, sb.ToString(), 13, VerdeTenue);
        t.alignment = TextAnchor.UpperCenter;
        t.lineSpacing = 1.35f;
        var rt = t.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(30, filas * 26);
        return t;
    }

    /// <summary>Barra de estado inferior: hint + prompt con cursor parpadeante.</summary>
    public static void CrearBarraEstado(Transform padre, Vector2 pos, string ruta, string hint)
    {
        var h = CrearTexto("Hint", padre, ruta + "> " + hint, 14, Verde);
        var hrt = h.GetComponent<RectTransform>();
        hrt.anchoredPosition = pos;
        hrt.sizeDelta = new Vector2(640, 22);
        var p = CrearTexto("Prompt", padre, "> ", 14, Verde);
        var prt = p.GetComponent<RectTransform>();
        prt.anchoredPosition = pos + new Vector2(0, -24);
        prt.sizeDelta = new Vector2(640, 22);
        p.gameObject.AddComponent<CursorBlink>();
    }

    /// <summary>Borde superior/inferior de caja: "+----...----+".</summary>
    public static string BordeCaja(int anchoChars)
    {
        return "+" + new string('-', Mathf.Max(4, anchoChars - 2)) + "+";
    }

    public static string CentrarEnCaja(string texto, int anchoChars)
    {
        int interior = Mathf.Max(4, anchoChars - 2);
        if (texto.Length >= interior) return "|" + texto.Substring(0, interior) + "|";
        int pad = interior - texto.Length;
        int izq = pad / 2;
        return "|" + new string(' ', izq) + texto + new string(' ', pad - izq) + "|";
    }
}

/// <summary>Parpadeo del cursor del prompt ("&gt; |" alternado).</summary>
public class CursorBlink : MonoBehaviour
{
    float t;
    UnityEngine.UI.Text txt;

    void Awake() { txt = GetComponent<UnityEngine.UI.Text>(); }

    void OnEnable() { t = 0f; }

    void Update()
    {
        if (txt == null) return;
        t += Time.unscaledDeltaTime;
        txt.text = (Mathf.FloorToInt(t * 1.6f) % 2 == 0) ? "> |" : ">  ";
    }
}
