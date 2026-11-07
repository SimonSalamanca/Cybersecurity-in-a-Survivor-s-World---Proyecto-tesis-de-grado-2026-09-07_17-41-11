using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// App Bestiario: catálogo de los virus del juego (Adware, Ransomware y
/// Rootkit). A la izquierda la lista de amenazas; al seleccionar una se
/// muestra su ficha (ilustración, peligro, qué hace, síntomas, cómo
/// eliminarla). Sin selección muestra las reglas generales de los virus.
/// </summary>
public class AppBestiario : MonoBehaviour
{
    class Virus
    {
        public string nombre, clase, peligro, arte, efecto, sintomas, eliminar, nota;
        public int nivelPeligro; // 1..5
        public Color color;
    }

    static readonly Virus[] virus =
    {
        new Virus
        {
            nombre = "ADWARE", clase = "Software publicitario", peligro = "Bajo", nivelPeligro = 1,
            color = new Color(1f, 0.72f, 0.2f, 1f),
            arte = "+------------------------+\n|  !! OFERTA UNICA !!  X |\n|   GANA $$$ HOY MISMO   |\n|   [ HAZ CLIC AQUI ]    |\n+------------------------+",
            efecto = "Llena la interfaz de publicidad intrusiva que interrumpe lo que estas haciendo. Su densidad controla con que frecuencia aparecen los anuncios.",
            sintomas = "Ventanas de ofertas que se abren solas y tapan el correo o el CMD en el peor momento.",
            eliminar = "No requiere accion: desaparece solo despues de un tiempo.",
            nota = "Molesto pero inofensivo. Solo hay que tener paciencia hasta que se vaya.",
        },
        new Virus
        {
            nombre = "RANSOMWARE", clase = "Secuestro de datos", peligro = "Alto", nivelPeligro = 4,
            color = new Color(1f, 0.32f, 0.28f, 1f),
            arte = "      .-------.\n     /  .---.  \\\n     |  |   |  |\n   .-'--'---'--'-.\n   |  ARCHIVOS   |\n   |  BLOQUEADOS |\n   |  PAGA $$$   |\n   '-------------'",
            efecto = "Bloquea los archivos del equipo y exige un rescate. Mientras esta activo no se puede abrir el portatil.",
            sintomas = "Al intentar usar el portatil solo aparece la pantalla de rescate con el monto a pagar.",
            eliminar = "Pagando el rescate. El monto se descuenta de tu saldo.",
            nota = "El mas peligroso para tu economia: guarda siempre algo de dinero por si te toca pagar.",
        },
        new Virus
        {
            nombre = "ROOTKIT", clase = "Control oculto del sistema", peligro = "Medio", nivelPeligro = 3,
            color = new Color(0.72f, 0.55f, 1f, 1f),
            arte = "   ___________\n  |  sistema  |\n  |  [1] [2]  |  <- tu eliges 1\n  |   [3]  ?  |  <- se ejecuta 3\n  |___________|\n     \\_root_/",
            efecto = "Se esconde en el sistema y sabotea tus decisiones: las respuestas de los dialogos del correo y los comandos del CMD se eligen de forma aleatoria.",
            sintomas = "Eliges una respuesta o un comando y el equipo ejecuta otro distinto.",
            eliminar = "Pulsa la tecla [Q] para eliminarlo.",
            nota = "Si tus elecciones no salen como esperabas, sospecha de un rootkit antes de seguir atacando.",
        },
    };

    RectTransform lista, ficha;
    Virus seleccionado;

    public void Inicializar(RectTransform cuerpo)
    {
        var izquierda = UIPC.Panel("Lista", cuerpo, new Color(0f, 0f, 0f, 0.3f));
        UIPC.Anclar(izquierda, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(330, 0));
        UIPC.FilaArriba(UIPC.Texto("Titulo", izquierda, "AMENAZAS CONOCIDAS (" + virus.Length + ")", 16, TerminalTheme.Verde, TextAnchor.MiddleLeft).rectTransform, 8, 28, 14);
        lista = UIPC.Anclar(UIPC.Panel("Entradas", izquierda, Color.clear), Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -44));
        var vl = lista.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 6f;
        vl.childControlWidth = vl.childControlHeight = true;
        vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;

        var general = UIPC.Boton("General", izquierda, "?  Reglas generales", 16, () => Seleccionar(null));
        UIPC.Anclar((RectTransform)general.transform, Vector2.zero, new Vector2(1, 0), new Vector2(8, 10), new Vector2(-8, 52));

        var separador = UIPC.Panel("Separador", cuerpo, TerminalTheme.VerdeTenue);
        UIPC.Anclar(separador, Vector2.zero, new Vector2(0, 1), new Vector2(330, 0), new Vector2(332, 0));
        ficha = UIPC.Anclar(UIPC.Panel("Ficha", cuerpo, Color.clear), Vector2.zero, Vector2.one, new Vector2(350, 14), new Vector2(-18, -14));

        Seleccionar(null);
    }

    void Seleccionar(Virus v)
    {
        seleccionado = v;
        RefrescarLista();
        UIPC.Vaciar(ficha);
        if (v == null) Reglas(); else Ficha(v);
    }

    void RefrescarLista()
    {
        UIPC.Vaciar(lista);
        foreach (var v in virus)
        {
            var entrada = v;
            var b = UIPC.Boton("Virus_" + v.nombre, lista, "", 16, () => Seleccionar(entrada));
            b.gameObject.AddComponent<LayoutElement>().preferredHeight = 76;
            if (v == seleccionado) b.GetComponent<Image>().color = TerminalTheme.BotonHover;
            var t = b.GetComponentInChildren<Text>();
            t.text = "<color=#" + ColorUtility.ToHtmlStringRGB(v.color) + "><b>" + v.nombre + "</b></color>\n" +
                     "<color=#739980>Peligro: " + v.peligro + "  " + Medidor(v) + "</color>";
            t.color = TerminalTheme.Blanco;
        }
    }

    static string Medidor(Virus v) => "[" + new string('#', v.nivelPeligro) + new string('-', 5 - v.nivelPeligro) + "]";

    // ---------------- Contenido ----------------

    void Reglas()
    {
        UIPC.FilaArriba(UIPC.Texto("Titulo", ficha, "BESTIARIO DE VIRUS", 28, TerminalTheme.Verde, TextAnchor.MiddleLeft).rectTransform, 4, 40);
        UIPC.FilaArriba(UIPC.Texto("Subtitulo", ficha, "Guia de campo de amenazas informaticas", 17, TerminalTheme.Gris, TextAnchor.MiddleLeft).rectTransform, 44, 24);
        UIPC.FilaArriba(UIPC.Panel("Linea", ficha, TerminalTheme.VerdeTenue), 78, 1);

        string texto =
            "Conocer a los virus es la mejor defensa: cada uno tiene sintomas reconocibles y una forma especifica de eliminarlo.\n\n" +
            "<color=#33FF66>REGLAS GENERALES</color>\n\n" +
            "-  El equipo solo puede tener UN virus activo a la vez.\n\n" +
            "-  Se instalan al fallar un comando durante un ataque con el CMD: el ataque se cancela, se pierde todo el progreso y entra un virus al azar.\n\n" +
            "-  Tambien pueden llegar por correo, al abrir el enlace malicioso de un correo trampa (phishing).\n\n" +
            "-  Cancelar un ataque manualmente no instala ningun virus.";
        UIPC.FilaArriba(UIPC.Texto("Reglas", ficha, texto, 17, TerminalTheme.Blanco).rectTransform, 96, 360);

        var pista = UIPC.Texto("Pista", ficha, "< Selecciona un virus de la lista para ver su ficha.", 16, TerminalTheme.Gris, TextAnchor.MiddleLeft);
        UIPC.Anclar(pista.rectTransform, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 30));
    }

    void Ficha(Virus v)
    {
        // Encabezado con el color de la amenaza.
        var franja = UIPC.FilaArriba(UIPC.Panel("Franja", ficha, new Color(v.color.r * 0.25f, v.color.g * 0.25f, v.color.b * 0.25f, 1f)), 0, 52);
        UIPC.Borde(franja, v.color);
        var titulo = UIPC.Texto("Titulo", franja, "<b>" + v.nombre + "</b>   <color=#739980>" + v.clase + "</color>", 24, v.color, TextAnchor.MiddleLeft);
        UIPC.Llenar(titulo.rectTransform).offsetMin = new Vector2(16, 0);

        // Ilustración y datos rápidos.
        var marco = UIPC.Panel("Ilustracion", ficha, new Color(0f, 0.012f, 0.006f, 1f));
        UIPC.Anclar(marco, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -266), new Vector2(320, -66));
        UIPC.Borde(marco, TerminalTheme.VerdeTenue);
        // Alineado a la izquierda (centrar línea por línea deforma el arte) y el bloque centrado en el marco.
        var arte = UIPC.Texto("Arte", marco, v.arte, 15, v.color, TextAnchor.MiddleLeft);
        arte.horizontalOverflow = HorizontalWrapMode.Overflow;
        float margen = Mathf.Max(0f, (320f - arte.preferredWidth) * 0.5f);
        UIPC.Anclar(arte.rectTransform, Vector2.zero, Vector2.one, new Vector2(margen, 0), Vector2.zero);

        string datos =
            "<color=#739980>PELIGRO</color>\n<color=#" + ColorUtility.ToHtmlStringRGB(v.color) + ">" + v.peligro + "  " + Medidor(v) + "</color>\n\n" +
            "<color=#739980>COMO LLEGA</color>\nAl fallar un comando del CMD o al abrir el enlace de un correo trampa.";
        UIPC.Anclar(UIPC.Texto("Datos", ficha, datos, 16, TerminalTheme.Blanco).rectTransform,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(340, -266), new Vector2(0, -70));

        float y = 282f;
        y = Seccion("QUE HACE", v.efecto, y);
        y = Seccion("SINTOMAS", v.sintomas, y);
        y = Seccion("COMO ELIMINARLO", v.eliminar, y);

        var nota = UIPC.FilaArriba(UIPC.Panel("Nota", ficha, new Color(0.03f, 0.09f, 0.05f, 1f)), y + 4, 58);
        UIPC.Borde(nota, TerminalTheme.VerdeTenue);
        UIPC.Llenar(UIPC.Texto("Texto", nota, "<color=#33FF66>NOTA DE CAMPO:</color> " + v.nota, 16, TerminalTheme.Blanco, TextAnchor.MiddleLeft).rectTransform, 12);
    }

    float Seccion(string titulo, string cuerpo, float y)
    {
        UIPC.FilaArriba(UIPC.Texto("Seccion", ficha, titulo, 16, TerminalTheme.Verde, TextAnchor.MiddleLeft).rectTransform, y, 22);
        UIPC.FilaArriba(UIPC.Texto("Cuerpo", ficha, cuerpo, 16, TerminalTheme.Blanco).rectTransform, y + 24, 44);
        return y + 74f;
    }
}
