/// <summary>
/// Contenido de las conversaciones con los compañeros de Vigia Consultores.
/// Cada NPC muestra una cara distinta del mundo (ver GDD, "Historia del mundo"):
///   Diana  - la creyente: el "mundo ideal", el implante obligatorio, la propaganda.
///   Julian - el de soporte: rumores de las cloacas, la muerte digital, Centinelas y Bestias.
///   Elena  - la veterana: el vacío emocional y el colega que desapareció tras las alertas.
///   Mateo  - el practicante: la recalibración del implante, el índice de conformidad.
/// Las líneas de Sistema son el implante del NPC registrando lo que dice: muestran
/// la hipervigilancia sin explicarla. Los textos de UI van sin tildes (convención del proyecto).
/// </summary>
public enum Hablante { NPC, Jugador, Sistema }

public class LineaDialogo
{
    public Hablante hablante;
    public string texto;
    public LineaDialogo(Hablante h, string t) { hablante = h; texto = t; }
}

public class TemaDialogo
{
    public string titulo;
    public LineaDialogo[] lineas;
    public TemaDialogo(string titulo, params LineaDialogo[] lineas) { this.titulo = titulo; this.lineas = lineas; }
}

public class PerfilNPC
{
    public string id, nombre, cargo;
    public LineaDialogo[] saludo, saludoRepetido, despedida;
    public TemaDialogo[] temas;
}

public static class DialogosOficina
{
    static LineaDialogo N(string t) => new LineaDialogo(Hablante.NPC, t);
    static LineaDialogo J(string t) => new LineaDialogo(Hablante.Jugador, t);
    static LineaDialogo S(string t) => new LineaDialogo(Hablante.Sistema, t);
    static LineaDialogo[] L(params LineaDialogo[] l) => l;

    /// <summary>Busca el perfil por id; si no hay id, lo deduce del nombre del objeto (NPC_Mujer_A...).</summary>
    public static PerfilNPC Obtener(string id, string nombreObjeto)
    {
        if (string.IsNullOrEmpty(id))
        {
            switch (nombreObjeto)
            {
                case "NPC_Mujer_A": id = "Diana"; break;
                case "NPC_Hombre_A": id = "Julian"; break;
                case "NPC_Mujer_B": id = "Elena"; break;
                default: id = "Mateo"; break;
            }
        }
        switch (id)
        {
            case "Diana": return Diana();
            case "Julian": return Julian();
            case "Elena": return Elena();
            default: return Mateo();
        }
    }

    static PerfilNPC Diana() => new PerfilNPC
    {
        id = "Diana",
        nombre = "Diana Ortiz",
        cargo = "Analista de vulnerabilidades",
        saludo = L(
            N("Buen dia. Ya cumpliste tu cuota de auditorias? La IA publico el ranking de productividad y nuestro piso vuelve a ir primero."),
            J("Todavia no. Solo vine a estirar las piernas."),
            N("Haz pausas cortas. Mi implante me avisa cada noventa minutos que debo levantarme. Es muy considerado, de verdad.")),
        saludoRepetido = L(
            N("Otra pausa? Ten cuidado, el implante cuenta cada minuto que pasas lejos de tu puesto.")),
        temas = new[]
        {
            new TemaDialogo("La ciudad",
                N("Mira por el ventanal. Cero crimenes en once periodos seguidos. Antes de la IA la gente se robaba en la calle, te imaginas?"),
                J("Y nadie extrana nada de como era antes?"),
                N("Extranar que? Todo llega a tiempo: comida, trabajo, descanso programado. Es un mundo ideal, lo dicen en cada anuncio."),
                N("Bueno... a veces quisiera ver un arbol de verdad. Uno que no sea una pantalla. Pero eso no es productivo, supongo."),
                S("Deseo ajeno al ciclo de produccion detectado. Nivel de riesgo: bajo."),
                N("...Ves? Ni siquiera vale la pena pensarlo.")),
            new TemaDialogo("El implante",
                N("Me lo pusieron al nacer, como a todos. Es obligatorio por ley. No recuerdo como era vivir sin el, y tampoco quiero saberlo."),
                N("Conecta lo que pensamos con la base de datos de la IA. Asi detecta los problemas antes de que existan. Prevencion, le dicen."),
                J("No te incomoda que sepa todo lo que piensas?"),
                N("Si no piensas nada malo, no tienes nada que esconder. Es lo primero que nos ensenan en la escuela.")),
            new TemaDialogo("Los Inadaptados",
                N("Esos? Gente que rechazo la sociedad. Viven en las cloacas de la ciudad, entre basura y agua podrida."),
                N("Dicen que se arrancan el implante y la IA los da por muertos. Muerte digital. Cambiar todo esto por oscuridad, que absurdo."),
                J("Y por que alguien haria eso?"),
                N("Porque estan enfermos. Querer algo distinto al orden es un sintoma. La IA solo intenta protegernos de ellos.")),
        },
        despedida = L(
            N("Vuelve a tu puesto pronto. No quiero que tu indice baje por mi culpa.")),
    };

    static PerfilNPC Julian() => new PerfilNPC
    {
        id = "Julian",
        nombre = "Julian Mora",
        cargo = "Soporte tecnico",
        saludo = L(
            N("Eh, el de las auditorias. Si vienes a decirme que tu monitor parpadea, ya lo se. Todos parpadean en este piso."),
            J("No, solo queria hablar un rato."),
            N("Hablar. Que concepto tan raro por aqui. Dale, pero en voz baja.")),
        saludoRepetido = L(
            N("Otra vez tu. Bueno, al menos contigo la conversacion no es sobre el ranking.")),
        temas = new[]
        {
            new TemaDialogo("Rumores de las cloacas",
                N("Arreglo equipos en toda la torre, asi que escucho cosas. Dicen que bajo la ciudad hay comunidades enteras viviendo en los tuneles."),
                N("Los llaman Inadaptados. Arman sus aparatos con lo que nosotros botamos: pantallas rotas, discos quemados, baterias hinchadas."),
                J("Con basura?"),
                N("Con nuestra basura. Lo que aqui es chatarra alla abajo vale oro. Una memoria RAM usada te alcanza para una semana, dicen.")),
            new TemaDialogo("La muerte digital",
                N("El truco esta en el implante. Si te lo sacas, la IA deja de recibir tus pensamientos y te registra como muerto."),
                N("Ya no existes en ninguna base de datos. Ni cuentas, ni historial, ni ranking. Nada."),
                J("Suena a libertad."),
                N("Suena a que te persigan. Cuidado con lo que dices... y con lo que piensas."),
                S("Patron de pensamiento anomalo registrado. Se notificara al supervisor."),
                N("...Genial. Mira, si alguien pregunta, estabamos hablando de impresoras. Impresoras.")),
            new TemaDialogo("Las maquinas de la IA",
                N("Has oido de los Centinelas? Maquinas chiquitas, como insectos. Se esconden entre las cucarachas de los tuneles buscando gente."),
                N("Dicen que se les nota un parpadeo de luz, muy tenue. Si ves uno, lo pisas antes de que avise."),
                J("Avisar a quien?"),
                N("A las Bestias. Cosas de cuatro patas, mitad carne, mitad metal. Nadie sabe de donde salieron. Un golpe y se acabo."),
                N("Yo no he visto ninguna, eh. Son historias que se cuentan en el turno de noche.")),
        },
        despedida = L(
            N("Y oye... si un dia desapareces, no me metas en tus pensamientos, vale?")),
    };

    static PerfilNPC Elena() => new PerfilNPC
    {
        id = "Elena",
        nombre = "Elena Castro",
        cargo = "Auditora senior",
        saludo = L(
            N("Ah, eres tu. Siempre te veo mirando por el ventanal. Yo hacia lo mismo cuando entre a Vigia."),
            J("Y ya no lo hace?"),
            N("Ya no. Despues de tantos periodos uno aprende a mirar solo la pantalla.")),
        saludoRepetido = L(
            N("Sigues dandole vueltas a algo, se te nota en la cara. Ten cuidado, eso tambien se nota alla arriba.")),
        temas = new[]
        {
            new TemaDialogo("El trabajo en Vigia",
                N("Buscamos las grietas de otras empresas antes de que alguien las use. Claves debiles, puertas abiertas, empleados que confian demasiado."),
                N("Lo curioso es que la mayoria de ataques no rompen sistemas, rompen personas. Un correo amable, una llamada urgente, y listo."),
                J("Ingenieria social."),
                N("Exacto. Por eso entrenamos con casos reales. Si tu puedes convencer a alguien, cualquiera puede hacerlo.")),
            new TemaDialogo("Un colega de antes",
                N("Antes en tu puesto se sentaba Tomas. Muy bueno con los sistemas. Un dia empezo a preguntar por que trabajamos, para quien, si podia ser distinto."),
                N("Le llegaron alertas de la IA. Primero amables, luego no tanto. 'Incitacion al desorden publico', decia la ultima."),
                J("Y que paso con el?"),
                N("Una manana su silla estaba vacia y su nombre ya no salia en el directorio. Como si nunca hubiera trabajado aqui."),
                N("Algunos dicen que lo atraparon. Yo prefiero pensar que llego a las cloacas."),
                S("Recuerdo no autorizado en revision.")),
            new TemaDialogo("La vida en la superficie",
                N("Despertar, trabajar, consumir, dormir. Todo optimizado. La IA hasta te recomienda que sentir el fin de semana."),
                N("No me quejo, eh. No me falta nada. Es solo que... no recuerdo la ultima vez que algo me importo de verdad."),
                J("A mi me pasa lo mismo."),
                N("Lo se. Por eso te lo digo. No dejes que te vacien del todo.")),
        },
        despedida = L(
            N("Vuelve al trabajo. Y piensa bajito.")),
    };

    static PerfilNPC Mateo() => new PerfilNPC
    {
        id = "Mateo",
        nombre = "Mateo Rincon",
        cargo = "Practicante",
        saludo = L(
            N("Hola! Soy Mateo, el practicante. Llevo tres semanas aqui y la supervisora Restrepo dice que voy muy bien."),
            J("Y como te trata la oficina?"),
            N("Genial! Mi indice de conformidad esta en 99%. Me falta un punto para el bono de consumo.")),
        saludoRepetido = L(
            N("Hola otra vez! Ya revisaste tu indice hoy? El mio subio un decimal.")),
        temas = new[]
        {
            new TemaDialogo("La recalibracion",
                N("La semana pasada me recalibraron el implante. Una noche pense algo raro mientras dormia... no lo recuerdo. El implante lo marco y lo borro."),
                N("Fue rapido. Dolor de cabeza un par de dias y ya. Ahora duermo tranquilo, sin pensar en nada."),
                J("No te da curiosidad saber que era?"),
                N("Curiosidad? ...No. Creo que no. No deberia, cierto?"),
                S("Pregunta registrada. Indice de conformidad: 98%."),
                N("Ay, no. Ahi se fue el bono.")),
            new TemaDialogo("La supervisora Restrepo",
                N("La supervisora Restrepo revisa todos nuestros reportes. Cada ataque de ingenieria social que simulamos termina en su bandeja."),
                N("Dice que el reporte es lo que importa: que tecnica se uso, que senales habia y como protegerse. Sin reporte, el trabajo no existe."),
                J("Suena exigente."),
                N("Es justa. Bueno, eso dice su perfil en el directorio.")),
            new TemaDialogo("Lo que dicen de los Inadaptados",
                N("En la induccion nos mostraron un video. Gente sucia, enferma, peleando por baterias viejas en los tuneles."),
                N("Decian que son peligrosos, que odian el progreso. Que si alguno te contacta hay que reportarlo de inmediato."),
                J("Y tu les crees?"),
                N("Claro. Aunque... en el video se reian. Todos, como si nada les importara. Nunca he visto a nadie reirse asi aqui arriba.")),
        },
        despedida = L(
            N("Me voy, que si paso mas de cuatro minutos lejos del puesto me bajan el indice!")),
    };
}
