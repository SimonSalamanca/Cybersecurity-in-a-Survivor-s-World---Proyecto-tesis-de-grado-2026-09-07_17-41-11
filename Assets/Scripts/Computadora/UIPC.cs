using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Utilidades para construir por código la interfaz del escritorio del PC
/// (paneles anclados, textos con ajuste de línea y botones) con la estética
/// de TerminalTheme.
/// </summary>
public static class UIPC
{
    public static readonly Color Ambar = new Color(1f, 0.72f, 0.2f, 1f);
    public static readonly Color Rojo = new Color(1f, 0.32f, 0.28f, 1f);

    /// <summary>Aviso de app bloqueada (solo lectura) que se repite en CMD y Estado.</summary>
    public static void AvisoBloqueo(RectTransform padre, string texto)
    {
        var aviso = FilaArriba(Panel("Aviso_Bloqueo", padre, new Color(0.22f, 0.15f, 0.02f, 1f)), 0, 34);
        Borde(aviso, Ambar);
        Llenar(Texto("Texto", aviso, "[BLOQUEADO]  " + texto, 16, Ambar, TextAnchor.MiddleLeft).rectTransform, 0).offsetMin = new Vector2(14, 0);
    }

    /// <summary>Barra de progreso estática (fondo + relleno) con el valor 0..1.</summary>
    public static RectTransform Barra(Transform padre, float valor, Color color)
    {
        var pista = Panel("Barra", padre, new Color(0.03f, 0.09f, 0.05f, 1f));
        Anclar(Panel("Relleno", pista, color), Vector2.zero, new Vector2(Mathf.Clamp01(valor), 1f), Vector2.zero, Vector2.zero);
        return pista;
    }

    /// <summary>Panel vacío; solo lleva Image si el color no es transparente.</summary>
    public static RectTransform Panel(string nombre, Transform padre, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        if (color.a > 0f) go.AddComponent<Image>().color = color;
        return (RectTransform)go.transform;
    }

    public static RectTransform Anclar(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
    {
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = offMin; rt.offsetMax = offMax;
        return rt;
    }

    public static RectTransform Llenar(RectTransform rt, float margen = 0f)
        => Anclar(rt, Vector2.zero, Vector2.one, Vector2.one * margen, -Vector2.one * margen);

    /// <summary>Franja anclada arriba y estirada a lo ancho: y = distancia desde el borde superior.</summary>
    public static RectTransform FilaArriba(RectTransform rt, float y, float alto, float margenX = 0f)
        => Anclar(rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(margenX, -y - alto), new Vector2(-margenX, -y));

    public static Text Texto(string nombre, Transform padre, string contenido, int tam, Color color, TextAnchor alineacion = TextAnchor.UpperLeft)
    {
        var t = TerminalTheme.CrearTexto(nombre, padre, contenido, tam, color);
        t.alignment = alineacion;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    public static Button Boton(string nombre, Transform padre, string etiqueta, int tam, UnityEngine.Events.UnityAction onClick)
    {
        var b = TerminalTheme.CrearBoton(nombre, padre, etiqueta, onClick, Vector2.zero, tam);
        var t = b.GetComponentInChildren<Text>();
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.supportRichText = true;
        t.raycastTarget = false;
        t.rectTransform.offsetMin = new Vector2(14, 4);
        t.rectTransform.offsetMax = new Vector2(-14, -4);
        Borde(b.GetComponent<RectTransform>(), TerminalTheme.VerdeTenue);
        return b;
    }

    public static void Borde(RectTransform rt, Color color)
    {
        var o = rt.gameObject.AddComponent<Outline>();
        o.effectColor = color;
        o.effectDistance = new Vector2(1, -1);
    }

    public static void Vaciar(Transform t)
    {
        // Se desactivan antes de destruir para que no cuenten en los layouts de este frame.
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            var hijo = t.GetChild(i).gameObject;
            hijo.SetActive(false);
            Object.Destroy(hijo);
        }
    }
}
