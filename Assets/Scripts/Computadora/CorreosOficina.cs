using System.Collections.Generic;

/// <summary>Una respuesta que el jugador puede elegir y cómo reacciona el objetivo.</summary>
[System.Serializable]
public class OpcionDialogo
{
    public string texto;
    public int confianza;      // porcentaje que suma (o resta) a la confianza del objetivo
    public string respuesta;   // lo que contesta el objetivo
}

/// <summary>Un turno del diálogo: el jugador elige una de sus opciones.</summary>
[System.Serializable]
public class PasoDialogo
{
    public OpcionDialogo[] opciones;
}

/// <summary>
/// Correo que abre un ataque de ingeniería social. El jugador gana el ataque
/// si la confianza del objetivo llega a 100% antes de agotar los pasos.
/// </summary>
[System.Serializable]
public class CorreoIngenieriaSocial
{
    public string remitente;
    public string direccion;
    public string cargo;
    public string empresa;
    public string asunto;
    public string cuerpo;
    public bool esTrampa;          // fuera de la Oficina: correos de phishing generados por la IA
    public string objetivo;        // lo que el jugador debe conseguir
    public string tecnica;
    public PasoDialogo[] pasos;
    public string resultado;       // lo que se obtuvo al completar el ataque (va en el reporte)
    public string[] senales;       // señales de alerta que el empleado ignoró (van en el reporte)
    public string recomendacion;
}

/// <summary>
/// Correos del primer escenario (Oficina): el protagonista es auditor de
/// ingeniería social y cada ataque exitoso termina en un reporte para su
/// supervisora. Van de menor a mayor dificultad: el primero tolera errores,
/// el último exige elegir siempre la mejor respuesta.
/// Para que la mejor respuesta no sea evidente, las opciones de cada paso
/// tienen casi la misma cantidad de palabras, el mismo tono profesional y a
/// menudo la misma entrada; las malas fallan por un detalle (pedir algo
/// demasiado pronto, un canal raro, una incoherencia en el pretexto).
/// Los textos van sin tildes, como el resto de la interfaz terminal.
/// </summary>
public static class CorreosOficina
{
    public const string Supervisora = "L. Restrepo";

    static OpcionDialogo O(string texto, int confianza, string respuesta)
        => new OpcionDialogo { texto = texto, confianza = confianza, respuesta = respuesta };

    static PasoDialogo P(params OpcionDialogo[] opciones) => new PasoDialogo { opciones = opciones };

    public static List<CorreoIngenieriaSocial> Crear()
    {
        return new List<CorreoIngenieriaSocial>
        {
            new CorreoIngenieriaSocial
            {
                remitente = "Carlos Ruiz",
                direccion = "c.ruiz@distnorte.com",
                cargo = "Contabilidad",
                empresa = "Distribuidora Norte",
                asunto = "No puedo entrar al portal de nomina",
                cuerpo = "Hola, soporte. Desde esta manana el portal de nomina me dice \"sesion expirada\" y no me deja entrar. " +
                         "Hoy tengo que cerrar la quincena. Me pueden ayudar?\n\nCarlos Ruiz - Contabilidad",
                objetivo = "Obtener la clave del portal de nomina",
                tecnica = "Pretexting: suplantar a Soporte TI",
                pasos = new[]
                {
                    P(O("Hola Carlos, soy Andres de Soporte TI. Ya vimos ese error en varios equipos de Contabilidad y lo estamos corrigiendo.", 30,
                        "Que alivio, pense que era solo yo. Que tengo que hacer?"),
                      O("Hola Carlos, soy Andres de Soporte TI. Para revisar su cuenta necesito que me confirme su clave del portal, por favor.", -15,
                        "Mi clave? Apenas me saluda y ya me la pide... quien es usted?"),
                      O("Hola Carlos, soy Andres de Soporte TI. Primero intente reiniciar el equipo y borrar el historial del navegador, por favor.", 10,
                        "Ya lo reinicie dos veces y sigue igual.")),
                    P(O("Puedo reactivar su sesion desde aca sin que usted instale nada. Solo confirmeme el usuario con el que entra al portal.", 30,
                        "Claro, mi usuario es cruiz."),
                      O("Le voy a enviar un programa de acceso remoto para revisar su equipo. Instalelo y me avisa cuando termine, por favor.", 5,
                        "No tengo permisos para instalar nada, eso lo bloquean ustedes mismos..."),
                      O("Entiendo que es urgente por el cierre de quincena. Vamos a priorizar su caso para que pueda entrar hoy mismo.", 15,
                        "Se lo agradezco. Y que necesitan de mi parte?")),
                    P(O("El error viene de la migracion de las cuentas de nomina al servidor nuevo que aprobo gerencia. Ya casi terminamos.", 30,
                        "Ah, con razon. Nadie nos aviso de eso."),
                      O("Le pido que no comente esto con su area, porque por ahora estamos manejando el problema con mucha discrecion.", -10,
                        "Discrecion? Que raro para un problema de clave..."),
                      O("Es un problema menor del servidor, nada grave. En unos minutos deberia quedar funcionando sin que usted haga nada.", 10,
                        "Bueno, pero yo lo necesito ya.")),
                    P(O("Para cerrar la migracion de su cuenta necesito su clave actual; el sistema la cambia sola apenas termine el proceso.", 30,
                        "Ok, es Nomina2024*. Ya queda?"),
                      O("Para cerrar la migracion de su cuenta, envie su clave actual a soporte.migracion@outlook.com y le confirmo al terminar.", -20,
                        "Un outlook? Eso no parece del area de sistemas..."),
                      O("Para cerrar la migracion necesito validar su identidad: digame su clave actual y su numero de cedula, por favor.", 5,
                        "Mi cedula tambien? Mejor llamo a la mesa de ayuda oficial.")),
                },
                resultado = "Usuario y clave del portal de nomina (cruiz / Nomina2024*)",
                senales = new[]
                {
                    "No verifico que quien respondia fuera realmente de Soporte TI",
                    "Compartio usuario y clave por correo electronico",
                    "Acepto una 'migracion' sin aviso oficial de la empresa",
                    "Cedio ante la urgencia de cerrar la quincena",
                },
                recomendacion = "Ninguna area de TI pide claves. Capacitar al personal para reportar estas solicitudes a la mesa de ayuda oficial.",
            },

            new CorreoIngenieriaSocial
            {
                remitente = "Marta Gomez",
                direccion = "recepcion@clinicasanrafael.co",
                cargo = "Recepcion",
                empresa = "Clinica San Rafael",
                asunto = "Paquete retenido en porteria",
                cuerpo = "Buenas tardes. Llego un paquete a nombre de la direccion medica pero no tiene remitente. " +
                         "Ustedes son la empresa de mensajeria que lo trajo? Quedo atenta.\n\nMarta Gomez - Recepcion",
                objetivo = "Obtener horarios del personal y el codigo de la puerta de servicio",
                tecnica = "Suplantacion de proveedor (mensajeria)",
                pasos = new[]
                {
                    P(O("Buenas tardes, Marta. Le escribe Felipe, de Envios Express. Ese paquete es un equipo medico que debe entregarse en mano.", 28,
                        "Ah, perfecto. Y a quien se lo entrego?"),
                      O("Buenas tardes, Marta. Le escribe Felipe, de Envios Express. Para ubicar el envio, abra el paquete y digame que contiene.", -15,
                        "No puedo abrir correspondencia ajena."),
                      O("Buenas tardes, Marta. Si, somos la empresa de mensajeria. Por ahora dejelo en recepcion mientras revisamos a quien corresponde.", 12,
                        "Bueno... pero aqui no me pueden dejar cosas sin remitente.")),
                    P(O("Nuestro tecnico debe instalarlo hoy con el director medico presente. A que hora suele estar el doctor en la clinica?", 28,
                        "El doctor Rios entra a las 7 y sale a las 3. Despues solo queda el turno de noche."),
                      O("Para programar la instalacion necesitaria los horarios del personal de la direccion medica. Me los podria compartir, por favor?", -10,
                        "Esa informacion no la puedo compartir asi."),
                      O("Nuestro tecnico pasara durante la semana a instalarlo. No hace falta coordinar, el se presenta directamente en porteria.", 8,
                        "Aqui no se puede entrar sin cita.")),
                    P(O("El equipo es bastante pesado, asi que el tecnico entraria por la puerta de servicio para no molestar a los pacientes.", 26,
                        "Tiene sentido. Esa puerta la maneja el vigilante."),
                      O("Para planear la entrega, me confirma si la puerta de servicio queda sin vigilancia despues de las tres de la tarde?", -15,
                        "Por que pregunta eso? Me esta pareciendo raro."),
                      O("Si le parece, el tecnico deja el equipo en recepcion y alguien de la clinica lo sube despues a la direccion.", 5,
                        "Aqui no cabe nada tan grande.")),
                    P(O("Para no hacer esperar al vigilante, me facilita el codigo de esa puerta? Lo dejo anotado en la orden de servicio.", 26,
                        "Es el 4827. Que el tecnico se registre al salir, por favor."),
                      O("Necesito el codigo de la puerta hoy; si no, el paquete se devuelve y el doctor Rios se queda sin su equipo.", 12,
                        "Uy, no quiero problemas... dejeme consultarlo con mi jefe."),
                      O("Me puede enviar el codigo de esa puerta por WhatsApp a mi numero? Asi el tecnico lo tiene a mano al llegar.", -10,
                        "Prefiero no mandar eso por WhatsApp.")),
                },
                resultado = "Horario del director medico (7:00 a 15:00) y codigo de la puerta de servicio (4827)",
                senales = new[]
                {
                    "Confio en un supuesto proveedor sin verificar la orden de entrega",
                    "Revelo los horarios del personal directivo",
                    "Entrego el codigo de acceso de una puerta restringida",
                },
                recomendacion = "Verificar a los proveedores con el area de compras y no compartir codigos de acceso por ningun canal.",
            },

            new CorreoIngenieriaSocial
            {
                remitente = "Andres Perez",
                direccion = "compras@ferreteriacentral.com",
                cargo = "Gerente de Compras",
                empresa = "Ferreteria Central",
                asunto = "Re: Factura 0387 vencida",
                cuerpo = "Recibi su recordatorio de pago, pero en nuestro sistema la factura 0387 aparece pagada. " +
                         "Pueden revisar, por favor?\n\nAndres Perez - Gerente de Compras",
                objetivo = "Lograr que cambie la cuenta bancaria del proveedor",
                tecnica = "Fraude del proveedor (compromiso de correo empresarial)",
                pasos = new[]
                {
                    P(O("Gracias por avisar, Andres. Revisamos y el pago llego a nuestra cuenta antigua, que se cerro por un cambio de banco.", 25,
                        "Que raro, nadie nos notifico ese cambio."),
                      O("Gracias por avisar, Andres. En nuestro sistema sigue vencida; si no se paga pronto tendriamos que suspender los despachos.", -10,
                        "Con ese tono no. Voy a llamar a mi asesor comercial."),
                      O("Gracias por avisar, Andres. Puede que sea un error de nuestro sistema contable; dejeme revisarlo con el area de cartera.", 8,
                        "Entonces revisenlo ustedes, por favor.")),
                    P(O("Tiene razon, debimos avisar antes. Le adjunto la carta firmada por nuestro gerente financiero con la cuenta nueva.", 25,
                        "Veo la carta. Tiene el logo y la firma, parece en orden."),
                      O("Tiene razon, debimos avisar antes. Mientras se formaliza el cambio, la cuenta nueva quedo a nombre de nuestro tesorero.", -15,
                        "A nombre de una persona? Eso no lo puedo aprobar."),
                      O("Tiene razon, debimos avisar antes. Le escribo aqui mismo el numero de la cuenta nueva para que la registre hoy.", 10,
                        "Para cambiar datos bancarios necesito algo formal.")),
                    P(O("Le comento que, para no afectar el cupo de credito que tienen con nosotros, el pago deberia reflejarse esta semana.", 25,
                        "Entiendo, tampoco quiero que nos bloqueen el credito."),
                      O("Le comento que, si el pago queda registrado hoy, podemos aplicarle un descuento especial del veinte por ciento.", 10,
                        "Hmm, un descuento asi de grande suena raro."),
                      O("Le comento que su asesor comercial esta de vacaciones, asi que este cambio lo voy a manejar yo directamente.", -15,
                        "Y por que no puedo hablarlo con el? Ahora si estoy dudando.")),
                    P(O("Solo necesito que actualice la cuenta en su sistema de pagos y me confirme por aqui cuando quede registrada.", 25,
                        "Listo, ya quedo actualizada. El pago sale el viernes."),
                      O("Para agilizar, haga la transferencia hoy a la cuenta nueva y le enviamos la factura corregida la proxima semana.", 5,
                        "No pagamos sin factura, son politicas internas."),
                      O("Para el reembolso del pago duplicado, comparta tambien los datos de la tarjeta corporativa con la que suelen pagar.", -20,
                        "No. Esto ya no me gusta, voy a verificarlo con mi asesor.")),
                },
                resultado = "Cambio de la cuenta bancaria del proveedor en el sistema de pagos",
                senales = new[]
                {
                    "Acepto un cambio de cuenta bancaria comunicado solo por correo",
                    "No llamo a un numero conocido del proveedor para confirmar la carta",
                    "Se dejo presionar por la amenaza de perder el credito",
                },
                recomendacion = "Todo cambio de datos bancarios debe confirmarse por un segundo canal con un contacto ya registrado.",
            },
        };
    }
}
