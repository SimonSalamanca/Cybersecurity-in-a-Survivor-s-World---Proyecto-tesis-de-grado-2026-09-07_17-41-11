using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú de pausa: overlay semitransparente oscurecido sobre el juego.
/// Reanudar / Ajustes (las 4 secciones completas, reutilizadas del menú
/// principal vía AjustesPanelBuilder) / Salir al menú principal.
/// Ajustes se abre SOBRE la pausa y VOLVER regresa a la pausa, nunca al menú.
/// </summary>
public class PauseManager : MonoBehaviour
{
    [Header("Teclas de pausa (nuevo Input System)")]
    public Key pausaKey = Key.Escape;
    public Key pausaAltKey = Key.P;

    GameObject canvasPausa;
    GameObject menuPausa;
    GameObject panelAjustes;
    readonly Dictionary<string, GameObject> tabs = new Dictionary<string, GameObject>();
    bool pausado;
    bool contenidoListo;

    void Awake()
    {
        ConstruirArmazon();
        MostrarPausa(false);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb[pausaKey].wasPressedThisFrame || kb[pausaAltKey].wasPressedThisFrame)
        {
            if (pausado) Reanudar();
            else Pausar();
        }
    }

    void ConstruirArmazon()
    {
        canvasPausa = new GameObject("Canvas_Pausa");
        var canvas = canvasPausa.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasPausa.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasPausa.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasPausa.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var fondo = new GameObject("FondoOscuro");
        fondo.transform.SetParent(canvasPausa.transform, false);
        var img = fondo.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0f, 0f, 0f, 0.65f);
        var frt = fondo.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;

        menuPausa = new GameObject("Menu_Pausa");
        menuPausa.transform.SetParent(canvasPausa.transform, false);
        var mrt = menuPausa.AddComponent<RectTransform>();
        mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
        mrt.offsetMin = Vector2.zero; mrt.offsetMax = Vector2.zero;

        var titulo = TerminalTheme.CrearTexto("Titulo", menuPausa.transform, "|| PAUSA", 68, TerminalTheme.Verde);
        titulo.alignment = TextAnchor.MiddleCenter;
        titulo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 290);
        titulo.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 100);

        var b1 = TerminalTheme.CrearBoton("Btn_Reanudar", menuPausa.transform, "REANUDAR JUEGO", Reanudar, new Vector2(560, 76), 28);
        b1.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 160);
        var b2 = TerminalTheme.CrearBoton("Btn_Ajustes", menuPausa.transform, "AJUSTES", MostrarAjustes, new Vector2(560, 76), 28);
        b2.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 60);
        var b3 = TerminalTheme.CrearBoton("Btn_MenuPrincipal", menuPausa.transform, "SALIR AL MENU PRINCIPAL", SalirAlMenu, new Vector2(560, 76), 28);
        b3.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -60);

        panelAjustes = new GameObject("Panel_AjustesPausa");
        panelAjustes.transform.SetParent(canvasPausa.transform, false);
        var prt = panelAjustes.AddComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
        var pfondo = panelAjustes.AddComponent<UnityEngine.UI.Image>();
        pfondo.color = new Color(0.008f, 0.02f, 0.012f, 0.97f);
        panelAjustes.SetActive(false);
    }

    /// <summary>Contenido pesado solo al abrir ajustes por primera vez
    /// (los managers ya existen entonces).</summary>
    void AsegurarContenidoAjustes()
    {
        if (contenidoListo) return;
        contenidoListo = true;
        TerminalTheme.CrearTextoCentrado("T", panelAjustes.transform, "C:\\SURVIVOR> ajustes (pausa)", 34, TerminalTheme.Verde, new Vector2(0, 370), new Vector2(1000, 50));
        string[] nombres = { "Sonido", "Graficos", "Gameplay", "Controles" };
        for (int i = 0; i < nombres.Length; i++)
        {
            string n = nombres[i];
            var contenido = new GameObject("TabPausa_" + n);
            contenido.transform.SetParent(panelAjustes.transform, false);
            var crt = contenido.AddComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(0, -140); crt.offsetMax = new Vector2(0, -160);
            tabs[n] = contenido;
            var tab = TerminalTheme.CrearBoton("Tab_" + n, panelAjustes.transform, n.ToUpper(), null, new Vector2(260, 58), 24);
            tab.GetComponent<RectTransform>().anchoredPosition = new Vector2(-390 + i * 260, 295);
            tab.onClick.AddListener(() => {
                foreach (var kv in tabs) kv.Value.SetActive(kv.Key == n);
            });
            contenido.SetActive(i == 0);
        }
        AjustesPanelBuilder.BuildSonido(tabs["Sonido"].transform);
        AjustesPanelBuilder.BuildGraficos(tabs["Graficos"].transform);
        AjustesPanelBuilder.BuildGameplay(tabs["Gameplay"].transform);
        AjustesPanelBuilder.BuildControles(tabs["Controles"].transform);
        // Ampliar contenido de cada pestaña (~1.9x) para que no se vea pequeño.
        foreach (var kv in tabs)
            kv.Value.transform.localScale = Vector3.one * 1.9f;
        var volver = TerminalTheme.CrearBoton("Btn_VolverPausa", panelAjustes.transform, "> VOLVER", () => panelAjustes.SetActive(false), new Vector2(400, 62), 26);
        volver.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -380);
    }

    public void Pausar()
    {
        pausado = true;
        Time.timeScale = 0f;
        if (panelAjustes != null) panelAjustes.SetActive(false);
        MostrarPausa(true);
    }

    public void Reanudar()
    {
        pausado = false;
        Time.timeScale = 1f;
        if (panelAjustes != null) panelAjustes.SetActive(false);
        MostrarPausa(false);
    }

    void MostrarAjustes()
    {
        AsegurarContenidoAjustes();
        if (panelAjustes != null) panelAjustes.SetActive(true);
    }

    void MostrarPausa(bool visible)
    {
        if (canvasPausa != null) canvasPausa.SetActive(visible);
    }

    public void SalirAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu_Laptop");
    }
}
