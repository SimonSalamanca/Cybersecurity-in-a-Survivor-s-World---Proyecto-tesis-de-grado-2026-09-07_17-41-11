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

    // Estado
    public bool ControlBloqueado { get; private set; }
    public float Yaw => yaw;

    InputAction accMover, accMirar, accInteractuar, accCorrer;
    CharacterController cc;
    float yaw, pitch, velVertical;
    bool limitarYaw; float yawCentro, yawRango;
    Vector3 posCamaraSuave;
    bool camaraInicializada;

    // HUD
    Text textoPrompt;
    Image mira;
    PuestoInteractivo puestoMirado;
    PuestoInteractivo puestoActual;

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
        if (Cursor.lockState != CursorLockMode.Locked) BloquearCursor(true);

        Mirar();
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
        camara.transform.position = posCamaraSuave;
        camara.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        // Oculta la cabeza solo para el render (después de que el Animator la posicionó).
        if (huesoCabeza != null) huesoCabeza.localScale = Vector3.one * 0.0001f;
    }

    // ---------------- Interacción ----------------

    void DetectarInteraccion()
    {
        puestoMirado = null;
        if (puestoActual == null && camara != null)
        {
            var rayo = new Ray(camara.transform.position, camara.transform.forward);
            if (Physics.Raycast(rayo, out var hit, distanciaInteraccion, ~0, QueryTriggerInteraction.Collide))
            {
                var p = hit.collider.GetComponentInParent<PuestoInteractivo>();
                if (p != null && p.EsObjetivo(hit.collider)) puestoMirado = p;
            }
        }

        bool pulsoE = accInteractuar != null ? accInteractuar.WasPressedThisFrame()
                                              : (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);
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
        else MostrarPrompt(null);
    }

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
        if (mira != null) mira.enabled = puestoActual == null;
    }

    void BloquearCursor(bool bloquear)
    {
        Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !bloquear;
    }
}
