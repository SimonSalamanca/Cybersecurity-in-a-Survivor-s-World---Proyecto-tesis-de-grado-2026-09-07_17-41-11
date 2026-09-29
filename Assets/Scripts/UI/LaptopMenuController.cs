using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controlador del menú en la laptop: escritorio con iconos + ventana
/// terminal con páginas (Principal, Ajustes con pestañas, Créditos,
/// confirmación de salida). Todo en español, base 1920x1080.
/// Las páginas se construyen por código para no depender de prefabs.
/// </summary>
public class LaptopMenuController : MonoBehaviour
{
    [Header("Se asignan al montar la escena (auto-búsqueda como respaldo)")]
    public GameObject canvasEscritorio;
    public GameObject canvasVentana;
    public Transform contenidoVentana;
    [Header("La ventana arranca abierta en Principal (sin pasar por escritorio)")]
    public bool arrancarConVentanaAbierta = true;

    readonly Stack<string> historial = new Stack<string>();
    string paginaActual = "Principal";

    readonly Dictionary<string, GameObject> paginas = new Dictionary<string, GameObject>();

    // ---------- Navegación estilo terminal (flechas + ENTER, clic con ratón) ----------
    public class OpcionMenu
    {
        public string texto;
        public System.Action accion;
        public Text label;
    }

    List<OpcionMenu> navActual;
    int navIndex;
    System.Action navAtras;
    readonly Dictionary<string, List<OpcionMenu>> navsPorPagina = new Dictionary<string, List<OpcionMenu>>();
    readonly Dictionary<string, System.Action> atrasPorPagina = new Dictionary<string, System.Action>();

    void Awake()
    {
        if (canvasEscritorio == null) canvasEscritorio = GameObject.Find("Canvas_Desktop");
        if (canvasVentana == null) canvasVentana = GameObject.Find("Canvas_Ventana");
        if (contenidoVentana == null)
        {
            var c = GameObject.Find("Ventana_Contenido");
            if (c != null) contenidoVentana = c.transform;
        }
        // Máscara de recorte: nada del menú puede dibujarse fuera del recuadro.
        if (contenidoVentana != null && contenidoVentana.GetComponent<UnityEngine.UI.RectMask2D>() == null)
            contenidoVentana.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
    }

    void Start()
    {
        // Cada página se protege por separado: si una falla, las demás
        // se construyen igual y el menú nunca queda vacío (imagen 6).
        TryConstruir("Principal", PaginaPrincipal);
        TryConstruir("Ajustes", PaginaAjustesLista);
        TryConstruir("Creditos", PaginaCreditos);
        TryConstruir("ConfirmSalir", PaginaConfirmSalir);
        TryConstruir("Aj_Sonido", () => PaginaSubAjuste("Aj_Sonido", "C:\\TermiGame\\Sonido>", "AJUSTES DE SONIDO", AjustesPanelBuilder.BuildSonido));
        TryConstruir("Aj_Graficos", () => PaginaSubAjuste("Aj_Graficos", "C:\\TermiGame\\Graficos>", "AJUSTES DE GRAFICOS", ConstruirGraficosGo));
        TryConstruir("Aj_Gameplay", () => PaginaSubAjuste("Aj_Gameplay", "C:\\TermiGame\\Gameplay>", "AJUSTES DE GAMEPLAY", ConstruirGameplayGo));
        TryConstruir("Aj_Controles", () => PaginaSubAjuste("Aj_Controles", "C:\\TermiGame\\Controles>", "CONFIGURAR CONTROLES", ConstruirControlesGo, false));
        historial.Clear();
        paginaActual = "Principal";
        foreach (var kv in paginas) kv.Value.SetActive(kv.Key == "Principal");
        ActivarNav("Principal");
        if (navActual == null || navActual.Count == 0)
            Debug.LogError("[LaptopMenu] Principal sin opciones navegables.");
        else
            Debug.Log("[LaptopMenu] Menu listo: " + paginas.Count + " paginas, " + navActual.Count + " opciones.");
        if (canvasVentana != null) canvasVentana.SetActive(arrancarConVentanaAbierta);
        Canvas.ForceUpdateCanvases();
    }

    void TryConstruir(string clave, System.Func<GameObject> construir)
    {
        try { paginas[clave] = construir(); }
        catch (System.Exception e) { Debug.LogError("[LaptopMenu] Fallo construyendo " + clave + ": " + e.Message); }
    }

    // ---------- API para botones ----------
    public void AbrirVentana(string pagina)
    {
        if (canvasVentana != null) canvasVentana.SetActive(true);
        Mostrar(pagina);
    }

    public void CerrarVentana()
    {
        if (canvasVentana != null) canvasVentana.SetActive(false);
        historial.Clear();
        paginaActual = "Principal";
        navActual = null;
        navAtras = null;
    }

    public void Mostrar(string pagina)
    {
        if (!string.IsNullOrEmpty(paginaActual) && paginaActual != pagina)
            historial.Push(paginaActual);
        paginaActual = pagina;
        foreach (var kv in paginas) kv.Value.SetActive(kv.Key == pagina);
        ActivarNav(pagina);
    }

    public void Atras()
    {
        if (historial.Count > 0)
        {
            string anterior = historial.Pop();
            paginaActual = anterior;
            foreach (var kv in paginas) kv.Value.SetActive(kv.Key == anterior);
            ActivarNav(anterior);
        }
        else CerrarVentana();
    }

    void ActivarNav(string pagina)
    {
        navsPorPagina.TryGetValue(pagina, out navActual);
        atrasPorPagina.TryGetValue(pagina, out navAtras);
        navIndex = 0;
        RefrescarNav();
    }

    void Update()
    {
        if (canvasVentana == null || !canvasVentana.activeInHierarchy) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (navActual != null && navActual.Count > 0)
        {
            if (kb.upArrowKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) MoverNav(-1);
            else if (kb.downArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) MoverNav(1);
            else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                navActual[navIndex].accion?.Invoke();
        }
        if (kb.escapeKey.wasPressedThisFrame && navAtras != null) navAtras();
    }

    void MoverNav(int dir)
    {
        navIndex = (navIndex + dir + navActual.Count) % navActual.Count;
        RefrescarNav();
    }

    void RefrescarNav()
    {
        if (navActual == null) return;
        for (int i = 0; i < navActual.Count; i++)
        {
            bool sel = i == navIndex;
            navActual[i].label.text = (sel ? "> " : "  ") + navActual[i].texto;
            navActual[i].label.color = sel ? TerminalTheme.Verde : TerminalTheme.VerdeTenue;
        }
    }

    /// <summary>Fila de opción navegable (teclado + clic). No mueve nada existente.</summary>
    OpcionMenu FilaOpcion(Transform padre, string etiqueta, Vector2 pos, Vector2 tam, System.Action accion)
    {
        var go = new GameObject("Op_" + etiqueta);
        go.transform.SetParent(padre, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        var fondo = go.AddComponent<Image>();
        fondo.color = new Color(0, 0, 0, 0);
        var btn = go.AddComponent<Button>();
        var t = new GameObject("Texto").AddComponent<Text>();
        t.transform.SetParent(go.transform, false);
        t.fontSize = 24;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = TerminalTheme.Fuente();
        var trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        // Texto y estilo por defecto aquí mismo: la fila nunca queda vacía
        // aunque la activación de navegación fallara después.
        t.text = "  " + etiqueta;
        t.color = TerminalTheme.VerdeTenue;
        var op = new OpcionMenu { texto = etiqueta, accion = accion, label = t };
        btn.onClick.AddListener(() => {
            if (navActual != null && navActual.Contains(op))
            {
                navIndex = navActual.IndexOf(op);
                RefrescarNav();
            }
            accion?.Invoke();
        });
        return op;
    }

    void RegistrarNav(string pagina, List<OpcionMenu> opciones, System.Action atras)
    {
        navsPorPagina[pagina] = opciones;
        atrasPorPagina[pagina] = atras;
    }

    public void Jugar()
    {
        Debug.Log("[LaptopMenu] Jugar -> cargando SampleScene");
        SceneManager.LoadScene("SampleScene");
    }

    public void PedirConfirmacionSalir() => Mostrar("ConfirmSalir");

    public void SalirSi()
    {
        Debug.Log("[LaptopMenu] Salir confirmado.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SalirNo() => Atras();

    // ---------- Construcción (ver Start: páginas protegidas una a una) ----------

    void DecorarPagina(GameObject p, string ruta, string hint)
    {
        TerminalTheme.CrearMarcoH("MarcoSup", p.transform, new Vector2(0, 150), 70);
        TerminalTheme.CrearRiel("RielIzq", p.transform, new Vector2(-298, -5), 11, ">");
        TerminalTheme.CrearRiel("RielDer", p.transform, new Vector2(298, -5), 11, "+");
        TerminalTheme.CrearBarraEstado(p.transform, new Vector2(0, -120), ruta, hint);
    }

    GameObject NuevaPagina(string nombre)
    {
        if (contenidoVentana == null)
            throw new System.InvalidOperationException("Ventana_Contenido no encontrado.");
        var go = new GameObject("P_" + nombre);
        go.transform.SetParent(contenidoVentana, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return go;
    }

    GameObject PaginaPrincipal()
    {
        var p = NuevaPagina("Principal");
        TerminalTheme.CrearBanner(p.transform, new Vector2(0, 110));
        TerminalTheme.CrearTextoCentrado("Subtitulo", p.transform, "MENU PRINCIPAL - SISTEMA DE CONTROL v2.1", 14, TerminalTheme.Blanco, new Vector2(0, 24), new Vector2(620, 22));
        var ops = new List<OpcionMenu>
        {
            FilaOpcion(p.transform, "1. Jugar", new Vector2(0, 0), new Vector2(480, 30), Jugar),
            FilaOpcion(p.transform, "2. Ajustes", new Vector2(0, -26), new Vector2(480, 30), () => Mostrar("Ajustes")),
            FilaOpcion(p.transform, "3. Creditos", new Vector2(0, -52), new Vector2(480, 30), () => Mostrar("Creditos")),
            FilaOpcion(p.transform, "4. Salir", new Vector2(0, -78), new Vector2(480, 30), PedirConfirmacionSalir),
        };
        RegistrarNav("Principal", ops, null);
        DecorarPagina(p, "C:\\TermiGame", "Seleccione una opcion con las flechas y presione ENTER.");
        return p;
    }

    GameObject PaginaAjustesLista()
    {
        var p = NuevaPagina("Ajustes");
        TerminalTheme.CrearBanner(p.transform, new Vector2(0, 110));
        TerminalTheme.CrearTextoCentrado("Subtitulo", p.transform, "AJUSTES DE CONFIGURACION", 14, TerminalTheme.Blanco, new Vector2(0, 24), new Vector2(620, 22));
        var ops = new List<OpcionMenu>
        {
            FilaOpcion(p.transform, "1. Sonido", new Vector2(0, 8), new Vector2(480, 28), () => Mostrar("Aj_Sonido")),
            FilaOpcion(p.transform, "2. Graficos", new Vector2(0, -17), new Vector2(480, 28), () => Mostrar("Aj_Graficos")),
            FilaOpcion(p.transform, "3. Gameplay", new Vector2(0, -42), new Vector2(480, 28), () => Mostrar("Aj_Gameplay")),
            FilaOpcion(p.transform, "4. Controles", new Vector2(0, -67), new Vector2(480, 28), () => Mostrar("Aj_Controles")),
            FilaOpcion(p.transform, "5. Volver", new Vector2(0, -92), new Vector2(480, 28), Atras),
        };
        RegistrarNav("Ajustes", ops, Atras);
        DecorarPagina(p, "C:\\TermiGame", "Seleccione una opcion con las flechas y presione ENTER.");
        return p;
    }

    /// <summary>Subpágina de ajuste: contenido compacto dentro de la ventana.</summary>
    GameObject PaginaSubAjuste(string clavePagina, string ruta, string titulo, System.Action<Transform> construir, bool conPrompt = true)
    {
        var p = NuevaPagina(clavePagina.Replace("Aj_", ""));
        TerminalTheme.CrearMarcoH("MarcoSup", p.transform, new Vector2(0, 150), 70);
        TerminalTheme.CrearRiel("RielIzq", p.transform, new Vector2(-298, -5), 11, ">");
        TerminalTheme.CrearRiel("RielDer", p.transform, new Vector2(298, -5), 11, "+");
        TerminalTheme.CrearTextoCentrado("Titulo", p.transform, titulo, 16, TerminalTheme.Blanco, new Vector2(0, 122), new Vector2(620, 24));
        var panel = new GameObject("Panel_" + clavePagina);
        panel.transform.SetParent(p.transform, false);
        var crt = panel.AddComponent<RectTransform>();
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
        construir(panel.transform);
        var ops = new List<OpcionMenu>
        {
            FilaOpcion(p.transform, "VOLVER", new Vector2(0, -102), new Vector2(480, 30), Atras),
        };
        RegistrarNav(clavePagina, ops, Atras);
        if (conPrompt)
        {
            TerminalTheme.CrearBarraEstado(p.transform, new Vector2(0, -124), ruta, "Presione ESC para volver.");
        }
        else
        {
            TerminalTheme.CrearTextoCentrado("Hint", p.transform, ruta + "> Presione ESC para volver.", 13, TerminalTheme.Verde, new Vector2(0, -128), new Vector2(640, 20));
        }
        return p;
    }

    void ConstruirGraficosGo(Transform padre) => AjustesPanelBuilder.BuildGraficos(padre);
    void ConstruirGameplayGo(Transform padre) => AjustesPanelBuilder.BuildGameplay(padre);
    void ConstruirControlesGo(Transform padre) => AjustesPanelBuilder.BuildControles(padre);

    // Contenido de ajustes: ver AjustesPanelBuilder (reutilizado por pausa).

    GameObject PaginaCreditos()
    {
        var p = NuevaPagina("Creditos");
        int ancho = 58;
        var caja = TerminalTheme.BordeCaja(ancho) + "\n"
            + TerminalTheme.CentrarEnCaja("CREDITOS DEL JUEGO", ancho) + "\n"
            + TerminalTheme.BordeCaja(ancho);
        TerminalTheme.CrearMarcoH("MarcoSup", p.transform, new Vector2(0, 150), 70);
        TerminalTheme.CrearRiel("RielIzq", p.transform, new Vector2(-298, -5), 11, ">");
        TerminalTheme.CrearRiel("RielDer", p.transform, new Vector2(298, -5), 11, "+");
        TerminalTheme.CrearTextoCentrado("CajaTitulo", p.transform, caja, 15, TerminalTheme.Verde, new Vector2(0, 112), new Vector2(640, 66));
        TerminalTheme.CrearTextoCentrado("Sub", p.transform, "DESARROLLO PRINCIPAL:", 16, TerminalTheme.Blanco, new Vector2(0, 62), new Vector2(620, 26));
        string[] nombres = { "Diego Casas", "Ivan", "Simon Salamanca" };
        for (int i = 0; i < nombres.Length; i++)
        {
            TerminalTheme.CrearTextoCentrado("C_" + i, p.transform, nombres[i], 20, TerminalTheme.Blanco, new Vector2(0, 28 - i * 30), new Vector2(620, 28));
        }
        TerminalTheme.CrearTextoCentrado("Thanks", p.transform, "Special Thanks: [Jugadores de Termi-Game]", 15, TerminalTheme.VerdeTenue, new Vector2(0, -78), new Vector2(620, 26));
        var ops = new List<OpcionMenu>
        {
            FilaOpcion(p.transform, "VOLVER", new Vector2(0, -108), new Vector2(480, 30), Atras),
        };
        RegistrarNav("Creditos", ops, Atras);
        TerminalTheme.CrearTextoCentrado("Hint", p.transform, "C:\\TermiGame\\Creditos> Presione ESC para volver.", 13, TerminalTheme.Verde, new Vector2(0, -138), new Vector2(640, 20));
        return p;
    }

    GameObject PaginaConfirmSalir()
    {
        var p = NuevaPagina("ConfirmSalir");
        TerminalTheme.CrearBanner(p.transform, new Vector2(0, 110), false);
        int ancho = 52;
        string caja = TerminalTheme.BordeCaja(ancho) + "\n"
            + TerminalTheme.CentrarEnCaja("CONFIRMAR SALIDA", ancho) + "\n"
            + TerminalTheme.CentrarEnCaja("", ancho) + "\n"
            + TerminalTheme.CentrarEnCaja("Realmente desea salir del juego?", ancho) + "\n"
            + TerminalTheme.CentrarEnCaja("Todo el progreso no guardado se perdera.", ancho) + "\n"
            + TerminalTheme.BordeCaja(ancho);
        TerminalTheme.CrearTextoCentrado("Dialogo", p.transform, caja, 14, TerminalTheme.Verde, new Vector2(0, 0), new Vector2(620, 130));
        var ops = new List<OpcionMenu>
        {
            FilaOpcion(p.transform, "[ SI ]", new Vector2(-130, -88), new Vector2(220, 36), SalirSi),
            FilaOpcion(p.transform, "[ NO ]", new Vector2(130, -88), new Vector2(220, 36), SalirNo),
        };
        RegistrarNav("ConfirmSalir", ops, SalirNo);
        DecorarPagina(p, "C:\\TermiGame", "Seleccione una opcion con las flechas y presione ENTER.");
        return p;
    }
}
