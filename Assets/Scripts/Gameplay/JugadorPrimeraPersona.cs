using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Controlador en primera persona del protagonista.
/// - WASD / flechas para moverse (acción "Move"), ratón para mirar (acción "Look"),
///   Shift para correr, E para interactuar (acción "Interact").
/// - La cámara sigue el hueso de la cabeza para que las animaciones
///   (caminar, sentarse, usar computador) se sientan desde la vista del personaje.
/// - La cabeza del modelo se oculta solo visualmente (escala casi cero) para
///   que no tape la cámara; el cuerpo se sigue viendo al mirar hacia abajo.
/// - Respeta los ajustes de Gameplay (sensibilidad, invertir Y) y Gráficos (FOV),
///   y libera el cursor cuando el juego está en pausa (Time.timeScale == 0).
/// - Al usar la computadora la cámara se acerca a la pantalla y el cursor queda
///   libre para manejar el escritorio (EscritorioPC).
/// - Mirando a un NPC con DialogoNPC, [E] abre una conversación (DialogoHUD):
///   el jugador queda quieto y la cámara se gira hacia la cara del NPC.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class JugadorPrimeraPersona : MonoBehaviour
{
    [Header("Referencias")]
    public InputActionAsset acciones;
    public Animator animator;
    public Transform huesoCabeza;
    public Camera camara;

    [Header("Movimiento")]
    public float velocidadCaminar = 1.8f;
    public float velocidadCorrer = 3.4f;
    public float gravedad = -15f;

    [Header("Cámara")]
    public float sensibilidadBase = 0.08f;          // grados por píxel de ratón
    public Vector3 desplazamientoOjos = new Vector3(0f, 0.08f, 0.12f); // respecto a la cabeza, en el espacio del jugador
    public float limitePitch = 80f;
    public float suavizadoCamara = 18f;

    [Header("Interacción")]
    public float distanciaInteraccion = 2.2f;

    [Header("Uso de la computadora (la cámara se acerca a la pantalla)")]
    [Tooltip("Fracción del alto de la vista que ocupa la pantalla.")]
    public float altoPantallaEnVista = 0.78f;
    [Tooltip("Centro vertical de la pantalla en la vista (-1 abajo, 1 arriba). Negativo deja sitio arriba para el tutorial.")]
    public float centroPantallaEnVista = -0.06f;
    public float duracionEnfoque = 0.45f;

    // Estado
    public bool ControlBloqueado { get; private set; }
    public float Yaw => yaw;
    public bool UsandoPantalla => pantallaEnfocada != null;
    public bool EnDialogo => dialogo != null && dialogo.Activo;
    public DialogoHUD Dialogo => dialogo;

    InputAction accMover, accMirar, accInteractuar, accCorrer;
    CharacterController cc;
    float yaw, pitch, velVertical;
    bool limitarYaw; float yawCentro, yawRango;
    Vector3 posCamaraSuave;
    bool camaraInicializada;
    RectTransform pantallaEnfocada, ultimaPantalla;
    float pesoEnfoque;
    readonly Vector3[] esquinas = new Vector3[4];

    // HUD
    Text textoPrompt;
    Image mira;
    PuestoInteractivo puestoMirado;
    PuestoInteractivo puestoActual;
    DialogoNPC npcMirado;
    DialogoHUD dialogo;
    float interaccionLibreDesde;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (camara == null) camara = GetComponentInChildren<Camera>();
        if (acciones != null)
        {
            if (SettingsManager.Instance != null && !string.IsNullOrEmpty(SettingsManager.Instance.inputOverridesJson))
            {
                try { acciones.LoadBindingOverridesFromJson(SettingsManager.Instance.inputOverridesJson); } catch { }
            }
            var mapa = acciones.FindActionMap("Player", true);
            accMover = mapa.FindAction("Move");
            accMirar = mapa.FindAction("Look");
            accInteractuar = mapa.FindAction("Interact");
            accCorrer = mapa.FindAction("Sprint");
        }
        yaw = transform.eulerAngles.y;
        CrearHud();
        dialogo = gameObject.AddComponent<DialogoHUD>();
        dialogo.jugador = this;
    }

    void OnEnable() { acciones?.FindActionMap("Player")?.Enable(); }
    void OnDisable() { acciones?.FindActionMap("Player")?.Disable(); }

    void Start()
    {
        BloquearCursor(true);
        if (camara != null && SettingsManager.Instance != null)
            camara.fieldOfView = SettingsManager.Instance.graficos.fov;
    }

    bool Pausado => Time.timeScale == 0f;

    void Update()
    {
        if (Pausado)
        {
            if (Cursor.lockState != CursorLockMode.None) BloquearCursor(false);
            return;
        }
        // Usando la computadora (o conversando) el cursor queda libre para hacer clic.
        bool bloquear = !UsandoPantalla && !EnDialogo;
        if ((Cursor.lockState == CursorLockMode.Locked) != bloquear) BloquearCursor(bloquear);

        if (EnDialogo)
        {
            MirarHacia(dialogo.Interlocutor.PuntoMirada);
            if (animator != null) animator.SetFloat("Velocidad", 0f, 0.1f, Time.deltaTime);
            dialogo.ProcesarEntrada(PulsoInteractuar());
            MostrarPrompt(null);
            return;
        }

        if (!UsandoPantalla) Mirar();
        if (!ControlBloqueado) Mover();
        else if (animator != null) animator.SetFloat("Velocidad", 0f);

        DetectarInteraccion();
    }

    void Mirar()
    {
        if (accMirar == null) return;
        Vector2 d = accMirar.ReadValue<Vector2>();
        float sens = sensibilidadBase;
        bool invertir = false;
        if (SettingsManager.Instance != null)
        {
            sens *= SettingsManager.Instance.gameplay.sensibilidadRaton;
            invertir = SettingsManager.Instance.gameplay.invertirY;
        }
        yaw += d.x * sens;
        pitch += (invertir ? d.y : -d.y) * sens;
        pitch = Mathf.Clamp(pitch, -limitePitch, limitePitch);
        if (limitarYaw)
        {
            float delta = Mathf.Clamp(Mathf.DeltaAngle(yawCentro, yaw), -yawRango, yawRango);
            yaw = yawCentro + delta;
        }
        // De pie el cuerpo gira con la mirada; sentado el cuerpo queda fijo.
        if (!ControlBloqueado) transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    /// <summary>Gira la mirada (y el cuerpo, de pie) suavemente hacia un punto: la cara del NPC.</summary>
    void MirarHacia(Vector3 punto)
    {
        if (camara == null) return;
        Vector3 dir = punto - camara.transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;
        float yawObjetivo = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float pitchObjetivo = -Mathf.Asin(Mathf.Clamp(dir.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
        float k = 1f - Mathf.Exp(-7f * Time.deltaTime);
        yaw = Mathf.LerpAngle(yaw, yawObjetivo, k);
        pitch = Mathf.Clamp(Mathf.LerpAngle(pitch, pitchObjetivo, k), -limitePitch, limitePitch);
        if (!ControlBloqueado) transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void Mover()
    {
        Vector2 entrada = accMover != null ? accMover.ReadValue<Vector2>() : Vector2.zero;
        entrada = Vector2.ClampMagnitude(entrada, 1f);
        bool corriendo = accCorrer != null && accCorrer.IsPressed() && entrada.y > 0.1f;
        float vel = corriendo ? velocidadCorrer : velocidadCaminar;
        Vector3 dir = transform.right * entrada.x + transform.forward * entrada.y;
        if (cc.isGrounded && velVertical < 0f) velVertical = -2f;
        velVertical += gravedad * Time.deltaTime;
        cc.Move((dir * vel + Vector3.up * velVertical) * Time.deltaTime);

        if (animator != null)
        {
            float magnitud = entrada.magnitude * (corriendo ? 1.9f : 1f);
            animator.SetFloat("Velocidad", magnitud, 0.1f, Time.deltaTime);
            // Acelera la animación de caminar al correr.
            animator.speed = corriendo ? 1.6f : 1f;
        }
    }

    void LateUpdate()
    {
        if (camara == null) return;
        // Posición: sigue la cabeza animada (con un pequeño adelanto para no ver el interior del modelo).
        Vector3 objetivo = huesoCabeza != null
            ? huesoCabeza.position + transform.rotation * desplazamientoOjos
            : transform.position + Vector3.up * 1.6f;
        if (!camaraInicializada) { posCamaraSuave = objetivo; camaraInicializada = true; }
        posCamaraSuave = Vector3.Lerp(posCamaraSuave, objetivo, 1f - Mathf.Exp(-suavizadoCamara * Time.unscaledDeltaTime));
        Vector3 pos = posCamaraSuave;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

        // Al usar la computadora la cámara se acerca a la pantalla (y vuelve al dejarla).
        pesoEnfoque = Mathf.MoveTowards(pesoEnfoque, UsandoPantalla ? 1f : 0f, Time.unscaledDeltaTime / duracionEnfoque);
        if (pesoEnfoque > 0f && ultimaPantalla != null)
        {
            PoseEnfoque(ultimaPantalla, out var posPantalla, out var rotPantalla);
            float s = Mathf.SmoothStep(0f, 1f, pesoEnfoque);
            pos = Vector3.Lerp(pos, posPantalla, s);
            rot = Quaternion.Slerp(rot, rotPantalla, s);
        }
        camara.transform.SetPositionAndRotation(pos, rot);
        // Oculta la cabeza solo para el render (después de que el Animator la posicionó).
        if (huesoCabeza != null) huesoCabeza.localScale = Vector3.one * 0.0001f;
    }

    // ---------------- Interacción ----------------

    void DetectarInteraccion()
    {
        puestoMirado = null;
        npcMirado = null;
        if (puestoActual == null && camara != null)
        {
            var rayo = new Ray(camara.transform.position, camara.transform.forward);
            if (Physics.Raycast(rayo, out var hit, distanciaInteraccion, ~0, QueryTriggerInteraction.Collide))
            {
                var p = hit.collider.GetComponentInParent<PuestoInteractivo>();
                if (p != null && p.EsObjetivo(hit.collider)) puestoMirado = p;
                else npcMirado = hit.collider.GetComponentInParent<DialogoNPC>();
            }
        }

        // Tras cerrar un diálogo la misma pulsación no debe abrir otro.
        bool pulsoE = PulsoInteractuar() && Time.time >= interaccionLibreDesde;
        bool pulsoQ = Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;

        if (puestoActual != null)
        {
            if (pulsoE) puestoActual.AlternarComputadora(this);
            else if (pulsoQ) puestoActual.Levantarse(this);
            MostrarPrompt(puestoActual.TextoPrompt());
        }
        else if (puestoMirado != null && puestoMirado.Disponible)
        {
            MostrarPrompt("[E] Sentarse");
            if (pulsoE) puestoMirado.Sentarse(this);
        }
        else if (npcMirado != null)
        {
            MostrarPrompt("[E] Hablar con " + npcMirado.Perfil.nombre);
            if (pulsoE) dialogo.Iniciar(npcMirado);
        }
        else MostrarPrompt(null);
    }

    bool PulsoInteractuar() => accInteractuar != null ? accInteractuar.WasPressedThisFrame()
                                                      : (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);

    /// <summary>Llamado por DialogoHUD al cerrar la conversación.</summary>
    public void FinDialogo() { interaccionLibreDesde = Time.time + 0.25f; }

    // API usada por PuestoInteractivo
    public void EntrarEnPuesto(PuestoInteractivo p, float yawMirada, float rangoYaw)
    {
        puestoActual = p;
        ControlBloqueado = true;
        cc.enabled = false;
        animator.speed = 1f;
        yawCentro = yawMirada; yawRango = rangoYaw; limitarYaw = true;
    }

    public void SalirDePuesto()
    {
        puestoActual = null;
        limitarYaw = false;
        cc.enabled = true;
        ControlBloqueado = false;
        velVertical = 0f;
    }

    public void ApuntarCamara(float nuevoYaw, float nuevoPitch)
    {
        yaw = nuevoYaw; pitch = nuevoPitch;
    }

    /// <summary>Acerca la cámara a la pantalla dada (canvas World Space); null para volver a la vista normal.</summary>
    public void EnfocarPantalla(RectTransform pantalla)
    {
        pantallaEnfocada = pantalla;
        if (pantalla != null) ultimaPantalla = pantalla;
    }

    void PoseEnfoque(RectTransform pantalla, out Vector3 pos, out Quaternion rot)
    {
        pantalla.GetWorldCorners(esquinas); // 0 abajo-izq, 1 arriba-izq, 2 arriba-der, 3 abajo-der
        Vector3 centro = (esquinas[0] + esquinas[2]) * 0.5f;
        float ancho = Vector3.Distance(esquinas[0], esquinas[3]);
        float alto = Vector3.Distance(esquinas[0], esquinas[1]);
        // Alto de la vista a la distancia de la pantalla: que ocupe la fracción pedida y quepa a lo ancho.
        float altoVista = Mathf.Max(alto / altoPantallaEnVista, ancho / (0.96f * camara.aspect));
        float distancia = altoVista * 0.5f / Mathf.Tan(camara.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 adelante = pantalla.forward, arriba = pantalla.up;
        pos = centro - adelante * distancia - arriba * (centroPantallaEnVista * altoVista * 0.5f);
        rot = Quaternion.LookRotation(adelante, arriba);
    }

    // ---------------- HUD ----------------

    void CrearHud()
    {
        var go = new GameObject("HUD_Jugador");
        go.transform.SetParent(transform, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        mira = new GameObject("Mira").AddComponent<Image>();
        mira.transform.SetParent(go.transform, false);
        mira.rectTransform.sizeDelta = new Vector2(6, 6);
        mira.color = new Color(1f, 1f, 1f, 0.75f);
        mira.raycastTarget = false;

        textoPrompt = new GameObject("Prompt").AddComponent<Text>();
        textoPrompt.transform.SetParent(go.transform, false);
        textoPrompt.font = TerminalTheme.Fuente();
        textoPrompt.fontSize = 30;
        textoPrompt.alignment = TextAnchor.MiddleCenter;
        textoPrompt.color = TerminalTheme.Verde;
        textoPrompt.raycastTarget = false;
        var sombra = textoPrompt.gameObject.AddComponent<Outline>();
        sombra.effectColor = new Color(0, 0, 0, 0.85f);
        var rt = textoPrompt.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, 110);
        rt.sizeDelta = new Vector2(1200, 60);
        textoPrompt.text = "";
    }

    void MostrarPrompt(string texto)
    {
        if (textoPrompt == null) return;
        textoPrompt.text = texto ?? "";
        // Con la pantalla en primer plano el aviso baja para no tapar la barra de tareas.
        textoPrompt.rectTransform.anchoredPosition = new Vector2(0, UsandoPantalla ? 38 : 110);
        if (mira != null) mira.enabled = puestoActual == null && !EnDialogo;
    }

    void BloquearCursor(bool bloquear)
    {
        Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !bloquear;
    }
}
