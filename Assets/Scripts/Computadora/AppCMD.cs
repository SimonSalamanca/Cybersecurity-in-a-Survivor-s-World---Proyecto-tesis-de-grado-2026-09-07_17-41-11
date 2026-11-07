using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// App CMD (solo lectura en la Oficina). Muestra cómo se verá el ataque
/// contra la IA: vida de la IA, virus activo, un paso de ejemplo de la
/// secuencia de 10 comandos y la explicación de las reglas. Los botones
/// están deshabilitados: el ataque se juega en escenarios posteriores.
/// </summary>
public class AppCMD : MonoBehaviour
{
    const string HexVerde = "#33FF66", HexGris = "#739980", HexRojo = "#FF524A", HexBlanco = "#D9FFE0";

    public void Inicializar(RectTransform cuerpo)
    {
        UIPC.AvisoBloqueo(UIPC.Anclar(UIPC.Panel("Margen", cuerpo, Color.clear), Vector2.zero, Vector2.one, new Vector2(14, 0), new Vector2(-14, -10)),
            "Bloqueado por la politica de TI de Vigia Consultores. Disponible mas adelante.");

        // Terminal con una sesión de ejemplo.
        var terminal = UIPC.Panel("Terminal", cuerpo, new Color(0f, 0.012f, 0.006f, 1f));
        UIPC.Anclar(terminal, Vector2.zero, Vector2.one, new Vector2(14, 70), new Vector2(-404, -54));
        UIPC.Borde(terminal, TerminalTheme.VerdeTenue);
        string sesion =
            "SURVIVOR OS [Version 2.1] - Modulo de ataque contra la IA\n" +
            "<color=" + HexGris + ">(c) Vigia Consultores. Uso restringido.</color>\n\n" +
            "C:\\SURVIVOR\\cmd> estado --objetivo ia\n" +
            "<color=" + HexBlanco + ">  OBJETIVO ........ Nucleo de la IA\n" +
            "  VIDA DE LA IA ... </color>[####################] 100%\n" +
            "<color=" + HexBlanco + ">  VIRUS ACTIVO .... </color>ninguno\n\n" +
            "C:\\SURVIVOR\\cmd> iniciar_ataque\n" +
            "<color=" + HexBlanco + ">  Cargando secuencia de 10 comandos...\n" +
            "  PASO 1/10 - Elige el comando correcto:\n" +
            "    [1] escanear_puertos --objetivo nucleo\n" +
            "    [2] forzar_acceso --puerto 443\n" +
            "    [3] inyectar_carga --modo silencioso</color>\n\n" +
            "<color=" + HexRojo + ">  ERROR: acceso denegado. Este equipo no tiene permisos\n" +
            "         para ejecutar el modulo de ataque.</color>";
        UIPC.Llenar(UIPC.Texto("Sesion", terminal, sesion, 15, TerminalTheme.Verde).rectTransform, 14);

        var prompt = UIPC.Texto("Prompt", terminal, "C:\\SURVIVOR\\cmd", 15, TerminalTheme.Verde, TextAnchor.MiddleLeft);
        UIPC.Anclar(prompt.rectTransform, Vector2.zero, new Vector2(0, 0), new Vector2(14, 10), new Vector2(160, 34));
        var cursor = UIPC.Texto("Cursor", terminal, "> |", 15, TerminalTheme.Verde, TextAnchor.MiddleLeft);
        UIPC.Anclar(cursor.rectTransform, Vector2.zero, new Vector2(0, 0), new Vector2(136, 10), new Vector2(200, 34));
        cursor.gameObject.AddComponent<CursorBlink>();

        // Reglas del CMD.
        var ayuda = UIPC.Panel("Ayuda", cuerpo, new Color(0f, 0f, 0f, 0.3f));
        UIPC.Anclar(ayuda, new Vector2(1, 0), Vector2.one, new Vector2(-390, 70), new Vector2(-14, -54));
        UIPC.FilaArriba(UIPC.Texto("Titulo", ayuda, "COMO FUNCIONA", 17, TerminalTheme.Verde).rectTransform, 12, 24, 14);
        string reglas =
            "- Es tu arma contra la IA: cada ataque completo le quita vida. Si llega a <color=" + HexVerde + ">0%</color>, ganas la partida.\n\n" +
            "- Al iniciar se carga una secuencia de <color=" + HexVerde + ">10 comandos</color>. En cada paso solo una opcion es correcta y ves tu avance (ej. 3/10).\n\n" +
            "- Acierta los 10 seguidos para completar el ataque.\n\n" +
            "- Si fallas en cualquier paso, el ataque se cancela, pierdes todo el progreso y <color=" + HexRojo + ">se instala un virus al azar</color>.\n\n" +
            "- Si cancelas tu mismo, vuelves a la espera sin virus.\n\n" +
            "<color=" + HexGris + ">Consulta la app Bestiario para conocer cada virus.</color>";
        UIPC.Anclar(UIPC.Texto("Reglas", ayuda, reglas, 15, TerminalTheme.Blanco).rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 12), new Vector2(-14, -44));

        // Controles deshabilitados.
        Deshabilitado(UIPC.Boton("Iniciar", cuerpo, "> Iniciar ataque", 17, null), new Vector2(14, 16), 220);
        Deshabilitado(UIPC.Boton("Cancelar", cuerpo, "x Cancelar ataque", 17, null), new Vector2(246, 16), 220);
        var progreso = UIPC.Texto("Progreso", cuerpo, "Progreso: 0/10", 16, TerminalTheme.Gris, TextAnchor.MiddleLeft);
        UIPC.Anclar(progreso.rectTransform, Vector2.zero, Vector2.zero, new Vector2(486, 16), new Vector2(700, 60));
    }

    static void Deshabilitado(Button b, Vector2 pos, float ancho)
    {
        b.interactable = false;
        b.GetComponentInChildren<Text>().color = TerminalTheme.Gris;
        UIPC.Anclar((RectTransform)b.transform, Vector2.zero, Vector2.zero, pos, pos + new Vector2(ancho, 44));
    }
}
