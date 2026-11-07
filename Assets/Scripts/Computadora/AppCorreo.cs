using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// App de Correo del escritorio. Los correos llegan a la bandeja con una
/// notificación; al abrir uno y pulsar Responder empieza el diálogo de
/// ingeniería social: cada respuesta suma (o resta) confianza del objetivo.
/// Llegar a 100% antes de agotar los pasos completa el ataque; si no, falla
/// y no se obtiene nada (se puede reintentar).
/// Versión Oficina: no llegan promociones ni trampas, los correos llegan de
/// uno en uno y cada ataque exitoso se cierra con un reporte a la supervisora
/// en lugar de dinero.
/// </summary>
public class AppCorreo : MonoBehaviour
{
    public enum EstadoMensaje { Nuevo, Leido, EnCurso, Exitoso, Fallido, Reportado }

    class Mensaje
    {
        public bool propio;
        public string texto;
        public int delta;
    }

    class Entrada
    {
        public CorreoIngenieriaSocial datos;
        public EstadoMensaje estado = EstadoMensaje.Nuevo;
        public int paso, confianza, confianzaAntes, numeroReporte;
        public int[] orden;
        public readonly List<Mensaje> registro = new List<Mensaje>();
    }

    [Header("Tiempos (segundos)")]
    public float esperaPrimerCorreo = 2f;
    public float esperaSiguienteCorreo = 6f;
    public float esperaRespuesta = 1.1f;

    // Progreso (lo consulta TutorialHUD)
    public bool Abierto { get; private set; }
    public int MensajesAbiertos { get; private set; }
    public int AtaquesExitosos { get; private set; }
    public int ReportesEnviados { get; private set; }
    public bool TodosReportados => pendientes.Count == 0 && bandeja.TrueForAll(e => e.estado == EstadoMensaje.Reportado);

    const string HexVerde = "#33FF66", HexGris = "#739980", HexAmbar = "#FFB833", HexRojo = "#FF524A";
    static readonly Color BurbujaPropia = new Color(0.03f, 0.14f, 0.07f, 1f);
    static readonly Color BurbujaObjetivo = new Color(0.05f, 0.07f, 0.06f, 1f);

    EscritorioPC escritorio;
    RectTransform listaBandeja, lectura, rellenoConfianza;
    Text tituloBandeja, textoConfianza;
    Image imagenConfianza;
    readonly Queue<CorreoIngenieriaSocial> pendientes = new Queue<CorreoIngenieriaSocial>();
    readonly List<Entrada> bandeja = new List<Entrada>();
    Entrada seleccionada;
    bool redactandoReporte;
    float proximoCorreo;
    float respuestaVisibleEn;   // mientras no llegue este instante, el objetivo "esta escribiendo"
    float confianzaMostrada;

    bool Escribiendo => respuestaVisibleEn > 0f;

    public void Inicializar(EscritorioPC escritorio, RectTransform cuerpo)
    {
        this.escritorio = escritorio;
        foreach (var c in CorreosOficina.Crear()) pendientes.Enqueue(c);
        proximoCorreo = Time.time + esperaPrimerCorreo;

        var izquierda = UIPC.Panel("Bandeja", cuerpo, new Color(0f, 0f, 0f, 0.3f));
        UIPC.Anclar(izquierda, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(330, 0));
        tituloBandeja = UIPC.Texto("Titulo", izquierda, "", 16, TerminalTheme.Verde, TextAnchor.MiddleLeft);
        UIPC.FilaArriba(tituloBandeja.rectTransform, 8, 28, 14);
        listaBandeja = UIPC.Anclar(UIPC.Panel("Lista", izquierda, Color.clear), Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -44));
        var vl = listaBandeja.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 6f;
        vl.childControlWidth = vl.childControlHeight = true;
        vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;

        var separador = UIPC.Panel("Separador", cuerpo, TerminalTheme.VerdeTenue);
        UIPC.Anclar(separador, Vector2.zero, new Vector2(0, 1), new Vector2(330, 0), new Vector2(332, 0));
        lectura = UIPC.Anclar(UIPC.Panel("Lectura", cuerpo, Color.clear), Vector2.zero, Vector2.one, new Vector2(332, 0), Vector2.zero);

        RefrescarBandeja();
        RefrescarLectura();
    }

    public void AlAbrir() { Abierto = true; }

    void Update()
    {
        // En la Oficina el siguiente correo llega cuando todo lo anterior ya está reportado.
        if (pendientes.Count > 0 && Time.time >= proximoCorreo && bandeja.TrueForAll(e => e.estado == EstadoMensaje.Reportado))
            RecibirCorreo();

        if (Escribiendo && Time.time >= respuestaVisibleEn)
        {
            respuestaVisibleEn = 0f;
            RefrescarLectura();
        }

        if (rellenoConfianza != null && seleccionada != null)
        {
            float meta = Escribiendo ? seleccionada.confianzaAntes : seleccionada.confianza;
            confianzaMostrada = Mathf.MoveTowards(confianzaMostrada, meta, 70f * Time.deltaTime);
            rellenoConfianza.anchorMax = new Vector2(confianzaMostrada / 100f, 1f);
            imagenConfianza.color = confianzaMostrada >= 50f ? TerminalTheme.Verde : UIPC.Ambar;
            textoConfianza.text = Mathf.RoundToInt(confianzaMostrada) + "%";
        }
    }

    // ---------------- Flujo ----------------

    void RecibirCorreo()
    {
        var e = new Entrada { datos = pendientes.Dequeue() };
        bandeja.Insert(0, e);
        RefrescarBandeja();
        escritorio.Notificar("NUEVO CORREO", e.datos.remitente + ": " + e.datos.asunto, () =>
        {
            escritorio.Abrir(EscritorioPC.IdCorreo);
            Seleccionar(e);
        });
    }

    void Seleccionar(Entrada e)
    {
        seleccionada = e;
        redactandoReporte = false;
        respuestaVisibleEn = 0f;
        if (e.estado == EstadoMensaje.Nuevo)
        {
            e.estado = EstadoMensaje.Leido;
            MensajesAbiertos++;
        }
        confianzaMostrada = e.confianza;
        RefrescarBandeja();
        RefrescarLectura();
    }

    // Primer clic sobre el correo. Fuera de la Oficina aquí se evalúa esTrampa
    // (los correos trampa traen un enlace malicioso); en la Oficina no hay trampas.
    void IniciarAtaque(Entrada e)
    {
        e.estado = EstadoMensaje.EnCurso;
        e.paso = e.confianza = e.confianzaAntes = 0;
        e.registro.Clear();
        e.registro.Add(new Mensaje { texto = e.datos.cuerpo });
        e.orden = Barajar(e.datos.pasos[0].opciones.Length);
        confianzaMostrada = 0f;
        RefrescarBandeja();
        RefrescarLectura();
    }

    void Elegir(Entrada e, OpcionDialogo op)
    {
        e.confianzaAntes = e.confianza;
        e.registro.Add(new Mensaje { propio = true, texto = op.texto });
        e.confianza = Mathf.Clamp(e.confianza + op.confianza, 0, 100);
        e.registro.Add(new Mensaje { texto = op.respuesta, delta = op.confianza });
        e.paso++;
        if (e.confianza >= 100)
        {
            e.estado = EstadoMensaje.Exitoso;
            AtaquesExitosos++;
        }
        else if (e.paso >= e.datos.pasos.Length) e.estado = EstadoMensaje.Fallido;
        else e.orden = Barajar(e.datos.pasos[e.paso].opciones.Length);

        respuestaVisibleEn = Time.time + esperaRespuesta;
        RefrescarBandeja();
        RefrescarLectura();
    }

    void EnviarReporte(Entrada e)
    {
        ReportesEnviados++;
        e.numeroReporte = ReportesEnviados;
        e.estado = EstadoMensaje.Reportado;
        redactandoReporte = false;
        proximoCorreo = Time.time + esperaSiguienteCorreo;
        if (TodosReportados)
            escritorio.Notificar("JORNADA COMPLETA", "Enviaste todos los reportes pendientes. Buen trabajo.", null);
        else
            escritorio.Notificar("REPORTE ENVIADO", CorreosOficina.Supervisora + " recibio el reporte " + NumeroReporte(e.numeroReporte) + ".", null);
        RefrescarBandeja();
        RefrescarLectura();
    }

    static int[] Barajar(int n)
    {
        var orden = new int[n];
        for (int i = 0; i < n; i++) orden[i] = i;
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (orden[i], orden[j]) = (orden[j], orden[i]);
        }
        return orden;
    }

    static string NumeroReporte(int n) => "No. " + n.ToString("000");
    static string Nombre(Entrada e) => e.datos.remitente.Split(' ')[0];

    // ---------------- Bandeja ----------------

    void RefrescarBandeja()
    {
        UIPC.Vaciar(listaBandeja);
        int noLeidos = bandeja.FindAll(x => x.estado == EstadoMensaje.Nuevo).Count;
        tituloBandeja.text = "BANDEJA DE ENTRADA (" + bandeja.Count + ")";
        escritorio.ActualizarInsignia(noLeidos);

        if (bandeja.Count == 0)
        {
            var vacio = UIPC.Texto("Vacio", listaBandeja, "(sin mensajes)", 16, TerminalTheme.Gris, TextAnchor.MiddleCenter);
            vacio.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
            return;
        }
        foreach (var e in bandeja)
        {
            var entrada = e;
            var b = UIPC.Boton("Mensaje", listaBandeja, "", 16, () => Seleccionar(entrada));
            b.gameObject.AddComponent<LayoutElement>().preferredHeight = 76;
            if (e == seleccionada) b.GetComponent<Image>().color = TerminalTheme.BotonHover;
            var t = b.GetComponentInChildren<Text>();
            string nombre = e.estado == EstadoMensaje.Nuevo ? "<b>" + e.datos.remitente + "</b>" : e.datos.remitente;
            t.text = nombre + "  " + EtiquetaEstado(e.estado) + "\n<color=" + HexGris + ">" + e.datos.asunto + "</color>";
            t.color = TerminalTheme.Blanco;
        }
    }

    static string EtiquetaEstado(EstadoMensaje e)
    {
        switch (e)
        {
            case EstadoMensaje.Nuevo: return "<color=" + HexVerde + ">[NUEVO]</color>";
            case EstadoMensaje.EnCurso: return "<color=" + HexAmbar + ">[EN CURSO]</color>";
            case EstadoMensaje.Exitoso: return "<color=" + HexVerde + ">[COMPROMETIDO]</color>";
            case EstadoMensaje.Fallido: return "<color=" + HexRojo + ">[FALLIDO]</color>";
            case EstadoMensaje.Reportado: return "<color=" + HexGris + ">[REPORTADO]</color>";
            default: return "";
        }
    }

    // ---------------- Panel de lectura ----------------

    void RefrescarLectura()
    {
        UIPC.Vaciar(lectura);
        rellenoConfianza = null;
        var e = seleccionada;
        if (e == null)
        {
            UIPC.Llenar(UIPC.Texto("Vacio", lectura, "Selecciona un mensaje de la bandeja de entrada", 18, TerminalTheme.Gris, TextAnchor.MiddleCenter).rectTransform);
            return;
        }

        float y = Cabecera(e);
        if (redactandoReporte) Reporte(e, y);
        else if (e.estado == EstadoMensaje.Leido) LecturaSimple(e, y);
        else Dialogo(e, y);
    }

    float Cabecera(Entrada e)
    {
        UIPC.FilaArriba(UIPC.Texto("De", lectura, "De: <b>" + e.datos.remitente + "</b>  <color=" + HexGris + "><" + e.datos.direccion + "></color>", 17, TerminalTheme.Blanco).rectTransform, 14, 24, 18);
        UIPC.FilaArriba(UIPC.Texto("Cargo", lectura, "    " + e.datos.cargo + " - " + e.datos.empresa, 15, TerminalTheme.Gris).rectTransform, 40, 20, 18);
        UIPC.FilaArriba(UIPC.Texto("Asunto", lectura, "Asunto: " + e.datos.asunto, 18, TerminalTheme.Verde).rectTransform, 64, 26, 18);
        UIPC.FilaArriba(UIPC.Panel("Linea", lectura, TerminalTheme.VerdeTenue), 96, 1, 18);
        return 100f;
    }

    void LecturaSimple(Entrada e, float y)
    {
        UIPC.FilaArriba(UIPC.Texto("Cuerpo", lectura, e.datos.cuerpo, 17, TerminalTheme.Blanco).rectTransform, y + 14, 300, 18);
        var responder = UIPC.Boton("Responder", lectura, "> Responder", 18, () => IniciarAtaque(e));
        UIPC.Anclar((RectTransform)responder.transform, Vector2.zero, Vector2.zero, new Vector2(18, 18), new Vector2(218, 66));
        var pista = UIPC.Texto("Pista", lectura, "Responde para iniciar el ataque de ingenieria social.", 15, TerminalTheme.Gris, TextAnchor.MiddleLeft);
        UIPC.Anclar(pista.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(236, 18), new Vector2(-18, 66));
    }

    void Dialogo(Entrada e, float y)
    {
        // Objetivo y barra de confianza.
        UIPC.FilaArriba(UIPC.Texto("Objetivo", lectura, "<color=" + HexGris + ">OBJETIVO:</color> " + e.datos.objetivo, 15, TerminalTheme.Verde, TextAnchor.MiddleLeft).rectTransform, y + 4, 22, 18);
        var etiqueta = UIPC.Texto("EtiquetaConfianza", lectura, "CONFIANZA", 15, TerminalTheme.Gris, TextAnchor.MiddleLeft);
        UIPC.Anclar(etiqueta.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -y - 54), new Vector2(118, -y - 32));
        var pista = UIPC.Panel("Pista", lectura, new Color(0.03f, 0.09f, 0.05f, 1f));
        UIPC.Anclar(pista, new Vector2(0, 1), new Vector2(1, 1), new Vector2(124, -y - 50), new Vector2(-84, -y - 36));
        rellenoConfianza = UIPC.Anclar(UIPC.Panel("Relleno", pista, Color.white), Vector2.zero, new Vector2(confianzaMostrada / 100f, 1f), Vector2.zero, Vector2.zero);
        imagenConfianza = rellenoConfianza.GetComponent<Image>();
        textoConfianza = UIPC.Texto("Porcentaje", lectura, "", 16, TerminalTheme.Blanco, TextAnchor.MiddleRight);
        UIPC.Anclar(textoConfianza.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-78, -y - 54), new Vector2(-18, -y - 32));

        bool enCurso = e.estado == EstadoMensaje.EnCurso;
        float alturaInferior = enCurso && !Escribiendo ? 3 * 58 + 2 * 8 + 32 : 104;
        Registro(e, y + 62, alturaInferior);

        if (Escribiendo)
        {
            var espera = UIPC.Texto("Espera", lectura, Nombre(e) + " esta escribiendo...", 16, TerminalTheme.Gris, TextAnchor.MiddleLeft);
            UIPC.Anclar(espera.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(18, 18), new Vector2(-18, 86));
            return;
        }
        if (enCurso)
        {
            var opciones = e.datos.pasos[e.paso].opciones;
            for (int i = 0; i < e.orden.Length; i++)
            {
                var op = opciones[e.orden[i]];
                float abajo = 16 + (e.orden.Length - 1 - i) * (58 + 8);
                var b = UIPC.Boton("Opcion_" + i, lectura, "> " + op.texto, 16, () => Elegir(e, op));
                UIPC.Anclar((RectTransform)b.transform, Vector2.zero, new Vector2(1, 0), new Vector2(18, abajo), new Vector2(-18, abajo + 58));
            }
            return;
        }

        string mensaje; Color color; string boton = null; UnityEngine.Events.UnityAction accion = null;
        switch (e.estado)
        {
            case EstadoMensaje.Exitoso:
                mensaje = "<b>ATAQUE EXITOSO</b>\nObtenido: " + e.datos.resultado;
                color = TerminalTheme.Verde; boton = "> Redactar reporte";
                accion = () => { redactandoReporte = true; RefrescarLectura(); };
                break;
            case EstadoMensaje.Fallido:
                mensaje = "<b>ATAQUE FALLIDO</b>\n" + Nombre(e) + " desconfio y corto la comunicacion. No obtuviste nada.";
                color = UIPC.Rojo; boton = "> Reintentar";
                accion = () => IniciarAtaque(e);
                break;
            default:
                mensaje = "[OK] Reporte " + NumeroReporte(e.numeroReporte) + " enviado a " + CorreosOficina.Supervisora + ".";
                color = TerminalTheme.Gris;
                break;
        }
        var banner = UIPC.Texto("Resultado", lectura, mensaje, 16, color, TextAnchor.MiddleLeft);
        UIPC.Anclar(banner.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(18, 16), new Vector2(boton != null ? -250 : -18, 88));
        if (boton != null)
        {
            var b = UIPC.Boton("Accion", lectura, boton, 17, accion);
            UIPC.Anclar((RectTransform)b.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-236, 28), new Vector2(-18, 76));
        }
    }

    void Registro(Entrada e, float arriba, float abajo)
    {
        var vista = UIPC.Panel("Registro", lectura, new Color(0f, 0f, 0f, 0.3f));
        UIPC.Anclar(vista, Vector2.zero, Vector2.one, new Vector2(18, abajo), new Vector2(-18, -arriba));
        vista.gameObject.AddComponent<RectMask2D>();
        var contenido = UIPC.Panel("Contenido", vista, Color.clear);
        contenido.anchorMin = new Vector2(0, 1); contenido.anchorMax = Vector2.one; contenido.pivot = new Vector2(0.5f, 1f);
        contenido.offsetMin = contenido.offsetMax = Vector2.zero;
        var vl = contenido.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(10, 10, 10, 10);
        vl.spacing = 8f;
        vl.childControlWidth = vl.childControlHeight = true;
        vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
        contenido.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = vista.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        scroll.viewport = vista;
        scroll.content = contenido;

        // Mientras el objetivo "escribe", su última respuesta todavía no se muestra.
        int visibles = Escribiendo ? e.registro.Count - 1 : e.registro.Count;
        for (int i = 0; i < visibles; i++)
        {
            var m = e.registro[i];
            string texto = m.propio
                ? "<color=" + HexVerde + ">Tu:</color> " + m.texto
                : "<b>" + Nombre(e) + ":</b> " + m.texto;
            if (m.delta != 0)
                texto += "\n<color=" + (m.delta > 0 ? HexVerde : HexRojo) + ">(" + (m.delta > 0 ? "+" : "") + m.delta + "% confianza)</color>";
            Burbuja(contenido, m.propio, texto);
        }
        if (Escribiendo) Burbuja(contenido, false, "<color=" + HexGris + ">...</color>");

        LayoutRebuilder.ForceRebuildLayoutImmediate(contenido);
        scroll.verticalNormalizedPosition = 0f;
    }

    static void Burbuja(Transform padre, bool propio, string texto)
    {
        // Fila con sangría: las propias a la derecha, las del objetivo a la izquierda.
        var fila = UIPC.Panel("Fila", padre, Color.clear);
        var vf = fila.gameObject.AddComponent<VerticalLayoutGroup>();
        vf.padding = propio ? new RectOffset(90, 0, 0, 0) : new RectOffset(0, 90, 0, 0);
        vf.childControlWidth = vf.childControlHeight = true;
        vf.childForceExpandWidth = true; vf.childForceExpandHeight = false;

        var burbuja = UIPC.Panel("Burbuja", fila, propio ? BurbujaPropia : BurbujaObjetivo);
        var vb = burbuja.gameObject.AddComponent<VerticalLayoutGroup>();
        vb.padding = new RectOffset(12, 12, 8, 8);
        vb.childControlWidth = vb.childControlHeight = true;
        vb.childForceExpandWidth = true; vb.childForceExpandHeight = false;
        UIPC.Texto("Texto", burbuja, texto, 16, TerminalTheme.Blanco);
    }

    void Reporte(Entrada e, float y)
    {
        var d = e.datos;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>REPORTE DE AUDITORIA " + NumeroReporte(ReportesEnviados + 1) + "</b>");
        sb.AppendLine("<color=" + HexGris + ">Para: " + CorreosOficina.Supervisora + " (Supervisora)    De: " + escritorio.usuario + "</color>");
        sb.AppendLine();
        sb.AppendLine("Cliente:    " + d.empresa);
        sb.AppendLine("Empleado:   " + d.remitente + " - " + d.cargo);
        sb.AppendLine("Tecnica:    " + d.tecnica);
        sb.AppendLine("Resultado:  <color=" + HexVerde + ">COMPROMETIDO</color> (confianza 100%)");
        sb.AppendLine("Obtenido:   " + d.resultado);
        sb.AppendLine();
        sb.AppendLine("Senales de alerta que el empleado ignoro:");
        foreach (var s in d.senales) sb.AppendLine("  - " + s);
        sb.AppendLine();
        sb.Append("Recomendacion: " + d.recomendacion);

        var hoja = UIPC.Panel("Hoja", lectura, new Color(0f, 0f, 0f, 0.3f));
        UIPC.Anclar(hoja, Vector2.zero, Vector2.one, new Vector2(18, 86), new Vector2(-18, -y - 10));
        UIPC.Llenar(UIPC.Texto("Texto", hoja, sb.ToString(), 15, TerminalTheme.Blanco).rectTransform, 14);

        var enviar = UIPC.Boton("Enviar", lectura, "> Enviar reporte", 17, () => EnviarReporte(e));
        UIPC.Anclar((RectTransform)enviar.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-250, 22), new Vector2(-18, 70));
        var volver = UIPC.Boton("Volver", lectura, "< Volver", 17, () => { redactandoReporte = false; RefrescarLectura(); });
        UIPC.Anclar((RectTransform)volver.transform, Vector2.zero, Vector2.zero, new Vector2(18, 22), new Vector2(178, 70));
    }
}
