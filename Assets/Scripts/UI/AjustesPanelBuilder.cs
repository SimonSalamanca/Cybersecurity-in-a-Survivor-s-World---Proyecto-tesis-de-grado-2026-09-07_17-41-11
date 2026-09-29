using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Construye las 4 secciones de ajustes (Sonido/Gráficos/Gameplay/Controles)
/// con estética terminal compacta, contenida en la ventana. Reutilizable
/// desde el menú principal (laptop) y el menú de pausa.
/// Métrica: filas de 38px, fuente 16, todo dentro de ±140 vertical.
/// </summary>
public static class AjustesPanelBuilder
{
    const int FS = 16;

    static void FilaEtiqueta(Transform padre, string etiqueta, float y, out Text t)
    {
        t = TerminalTheme.CrearTexto("L_" + etiqueta, padre, etiqueta, FS, TerminalTheme.Blanco);
        var rt = t.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(-160, y);
        rt.sizeDelta = new Vector2(240, 30);
    }

    static Slider FilaSlider(Transform padre, string etiqueta, float y, float min, float max, float valor, System.Action<float> onChange)
    {
        FilaEtiqueta(padre, etiqueta, y, out _);
        var sl = TerminalTheme.CrearSlider("S_" + etiqueta, padre, min, max, valor, new Vector2(220, 20));
        sl.GetComponent<RectTransform>().anchoredPosition = new Vector2(155, y);
        sl.onValueChanged.AddListener(v => { onChange(v); SettingsManager.Instance.Guardar(); });
        return sl;
    }

    static Toggle FilaToggle(Transform padre, string etiqueta, float y, bool valor, System.Action<bool> onChange)
    {
        var tg = TerminalTheme.CrearToggle("T_" + etiqueta, padre, etiqueta, valor);
        tg.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, y);
        tg.onValueChanged.AddListener(v => { onChange(v); SettingsManager.Instance.Guardar(); });
        return tg;
    }

    public static void BuildSonido(Transform padre)
    {
        var s = SettingsManager.Instance.audio;
        FilaSlider(padre, "Sonido general", 95, 0f, 1f, s.master, v => { s.master = v; AudioManager.Instance?.Aplicar(); });
        FilaSlider(padre, "Ambiente", 57, 0f, 1f, s.ambiente, v => { s.ambiente = v; AudioManager.Instance?.Aplicar(); });
        FilaSlider(padre, "Musica", 19, 0f, 1f, s.musica, v => { s.musica = v; AudioManager.Instance?.Aplicar(); });
        FilaSlider(padre, "Bestias", -19, 0f, 1f, s.bestias, v => { s.bestias = v; AudioManager.Instance?.Aplicar(); });
        FilaToggle(padre, "Silenciar todo", -60, s.mute, v => { s.mute = v; AudioManager.Instance?.Aplicar(); });
    }

    public static void BuildGraficos(Transform padre)
    {
        var g = SettingsManager.Instance.graficos;
        var gm = GraphicsManager.Instance;
        FilaEtiqueta(padre, "Calidad", 95, out _);
        var tq = TerminalTheme.CrearTexto("V_Calidad", padre, gm.NombresCalidad()[Mathf.Clamp(g.qualityIndex, 0, gm.NombresCalidad().Length - 1)], FS, TerminalTheme.Verde);
        tq.GetComponent<RectTransform>().anchoredPosition = new Vector2(55, 95);
        tq.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 30);
        var bq = TerminalTheme.CrearBoton("Btn_Calidad", padre, "CAMBIAR >", () => {
            g.qualityIndex = (g.qualityIndex + 1) % gm.NombresCalidad().Length;
            tq.text = gm.NombresCalidad()[g.qualityIndex];
            gm.Aplicar(); SettingsManager.Instance.Guardar();
        }, new Vector2(150, 30));
        bq.GetComponent<RectTransform>().anchoredPosition = new Vector2(195, 95);
        FilaToggle(padre, "Sinc. vertical", 57, g.vsync == 1, v => { g.vsync = v ? 1 : 0; gm.Aplicar(); });
        FilaSlider(padre, "Brillo", 19, 0.5f, 1.5f, g.brillo, v => { g.brillo = v; gm.Aplicar(); });
        FilaSlider(padre, "Dist. sombras", -19, 10f, 200f, g.distanciaSombras, v => { g.distanciaSombras = v; gm.Aplicar(); });
        FilaEtiqueta(padre, "Resolucion", -57, out _);
        var r = gm.ResolucionElegida(g.resolutionIndex);
        var tr = TerminalTheme.CrearTexto("V_Res", padre, r.width + "x" + r.height, FS, TerminalTheme.Verde);
        tr.GetComponent<RectTransform>().anchoredPosition = new Vector2(55, -57);
        tr.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 30);
        var br = TerminalTheme.CrearBoton("Btn_Res", padre, "CAMBIAR >", () => {
            var list = gm.Resoluciones();
            g.resolutionIndex = g.resolutionIndex + 1 >= list.Length ? -1 : g.resolutionIndex + 1;
            var rr = gm.ResolucionElegida(g.resolutionIndex);
            tr.text = rr.width + "x" + rr.height;
            gm.Aplicar(); SettingsManager.Instance.Guardar();
        }, new Vector2(150, 30));
        br.GetComponent<RectTransform>().anchoredPosition = new Vector2(195, -57);
    }

    public static void BuildGameplay(Transform padre)
    {
        var s = SettingsManager.Instance.gameplay;
        FilaSlider(padre, "Sens. raton", 95, 0.1f, 5f, s.sensibilidadRaton, v => s.sensibilidadRaton = v);
        FilaSlider(padre, "Dist. camara", 57, 0f, 2f, s.distanciaCamara, v => s.distanciaCamara = v);
        FilaToggle(padre, "Modo fotosensible", 15, s.modoFotosensible, v => { s.modoFotosensible = v; GameplaySettings.Instance?.Aplicar(); });
        FilaToggle(padre, "Invertir eje Y", -23, s.invertirY, v => s.invertirY = v);
        FilaToggle(padre, "Subtitulos", -61, s.subtitulos, v => s.subtitulos = v);
    }

    /// <summary>Lista de controles con scroll recortado a la ventana.</summary>
    public static void BuildControles(Transform padre)
    {
        var vista = new GameObject("VistaControles");
        vista.transform.SetParent(padre, false);
        var vrt = vista.AddComponent<RectTransform>();
        vrt.anchoredPosition = new Vector2(0, 12);
        vrt.sizeDelta = new Vector2(560, 190);
        var vim = vista.AddComponent<Image>();
        vim.color = new Color(0, 0, 0, 0.35f);
        vista.AddComponent<RectMask2D>();
        var scroll = vista.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.scrollSensitivity = 24f;
        var contenido = new GameObject("Contenido");
        contenido.transform.SetParent(vista.transform, false);
        var crt = contenido.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 1);
        crt.anchoredPosition = new Vector2(0, 0);
        crt.sizeDelta = new Vector2(0, 100);
        scroll.content = crt;
        scroll.viewport = vrt;
        var cont = padre.gameObject.AddComponent<ControlsRebinding>();
        cont.modoCompacto = true;
        cont.Construir(contenido.transform);
        // Ajustar alto del contenido a las filas creadas.
        float alto = Mathf.Max(200, contenido.transform.childCount * 36 + 60);
        crt.sizeDelta = new Vector2(0, alto);
    }
}
