using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// App de Estado (solo lectura en la Oficina). Muestra el nivel de los cinco
/// componentes del equipo, una alerta de desgaste de ejemplo, la probabilidad
/// de encontrar cada repuesto en el mapa y las reglas de reemplazo. Los
/// niveles son fijos y el reemplazo está deshabilitado: la degradación se
/// juega en escenarios posteriores.
/// </summary>
public class AppEstado : MonoBehaviour
{
    struct Componente
    {
        public string nombre;
        public int nivel;
        public int aparicion; // % de aparición como repuesto en el mapa
    }

    static readonly Componente[] componentes =
    {
        new Componente { nombre = "RAM", nivel = 92, aparicion = 10 },
        new Componente { nombre = "Procesador", nivel = 88, aparicion = 5 },
        new Componente { nombre = "Disco duro", nivel = 95, aparicion = 5 },
        new Componente { nombre = "Bateria", nivel = 64, aparicion = 20 },
        new Componente { nombre = "Pantalla", nivel = 100, aparicion = 30 },
    };

    const float ColNivel = 200f, ColEstado = 620f, ColAparicion = 820f, AltoFila = 50f;

    public void Inicializar(RectTransform cuerpo)
    {
        var area = UIPC.Anclar(UIPC.Panel("Margen", cuerpo, Color.clear), Vector2.zero, Vector2.one, new Vector2(14, 14), new Vector2(-14, -10));
        UIPC.AvisoBloqueo(area, "Monitoreo administrado por TI. El reemplazo de componentes esta deshabilitado.");

        UIPC.FilaArriba(UIPC.Texto("Equipo", area, "ESTADO DEL EQUIPO  <color=#739980>VIGIA-PC-02 (auditor)</color>", 18, TerminalTheme.Verde, TextAnchor.MiddleLeft).rectTransform, 46, 26);

        // Tabla de componentes.
        float y = 84f;
        Celda(area, "COMPONENTE", 0, y, ColNivel, TerminalTheme.Gris, 15);
        Celda(area, "NIVEL", ColNivel, y, 200, TerminalTheme.Gris, 15);
        Celda(area, "ESTADO", ColEstado, y, 200, TerminalTheme.Gris, 15);
        Celda(area, "APARICION EN EL MAPA", ColAparicion, y, 260, TerminalTheme.Gris, 15);
        UIPC.FilaArriba(UIPC.Panel("Linea", area, TerminalTheme.VerdeTenue), y + 26, 1);

        y += 34f;
        foreach (var c in componentes)
        {
            var fila = UIPC.FilaArriba(UIPC.Panel("Fila_" + c.nombre, area, new Color(0f, 0f, 0f, 0.25f)), y, AltoFila - 8);
            Color color = c.nivel >= 75 ? TerminalTheme.Verde : c.nivel > 25 ? UIPC.Ambar : UIPC.Rojo;
            string estado = c.nivel >= 75 ? "OK" : c.nivel > 25 ? "DESGASTE [!]" : c.nivel > 0 ? "CRITICO [!]" : "AGOTADO";

            Celda(fila, c.nombre, 12, 0, ColNivel, TerminalTheme.Blanco, 17, AltoFila - 8);
            var barra = UIPC.Barra(fila, c.nivel / 100f, color);
            UIPC.Anclar(barra, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(ColNivel, -8), new Vector2(ColNivel + 320, 8));
            Celda(fila, c.nivel + "%", ColNivel + 332, 0, 70, color, 17, AltoFila - 8);
            Celda(fila, estado, ColEstado, 0, 190, color, 16, AltoFila - 8);
            Celda(fila, c.aparicion + "%", ColAparicion, 0, 120, TerminalTheme.Blanco, 17, AltoFila - 8);
            y += AltoFila;
        }

        // Inventario y reemplazo (deshabilitado).
        y += 10f;
        Celda(area, "Repuestos en inventario: 0", 0, y, 400, TerminalTheme.Blanco, 17, 44);
        var reemplazar = UIPC.Boton("Reemplazar", area, "Reemplazar componente", 17, null);
        reemplazar.interactable = false;
        reemplazar.GetComponentInChildren<Text>().color = TerminalTheme.Gris;
        UIPC.Anclar((RectTransform)reemplazar.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(ColEstado, -y - 44), new Vector2(ColEstado + 260, -y));

        string reglas =
            "Los componentes se desgastan con el tiempo y con el uso continuo; al degradarse, esta app muestra una alerta. " +
            "Si uno llega a 0% su funcion se deshabilita y se bloquea la app asociada. Para recuperarlo, abre el menu de reemplazo " +
            "(se activa cuando un componente se agota y tienes un repuesto) y elige uno nuevo.\n" +
            "<color=#739980>Los repuestos se recogen en el mapa con clic izquierdo (no aparecen en todas las zonas) o se compran " +
            "en la tienda de las aldeas, con un precio segun su rareza.</color>";
        var texto = UIPC.Texto("Reglas", area, reglas, 15, TerminalTheme.Blanco);
        UIPC.Anclar(texto.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -y - 58));
    }

    static void Celda(Transform padre, string texto, float x, float y, float ancho, Color color, int tam, float alto = 24f)
    {
        var t = UIPC.Texto("Celda", padre, texto, tam, color, TextAnchor.MiddleLeft);
        UIPC.Anclar(t.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y - alto), new Vector2(x + ancho, -y));
    }
}
