using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reasignación total de controles con el asset InputSystem_Actions.
/// Construye filas (acción + botón) dentro de un contenedor y persiste
/// los overrides en SettingsManager.inputOverridesJson.
/// </summary>
public class ControlsRebinding : MonoBehaviour
{
    [Header("Asset de acciones (se autocarga si se deja vacío)")]
    public InputActionAsset actions;

    [Header("Mapa a reasignar")]
    public string mapName = "Player";

    Transform filasPadre;
    InputActionMap mapa;
    bool escuchando;

    [Header("Filas compactas para caber en la ventana terminal")]
    public bool modoCompacto = true;
    int PasoFila => modoCompacto ? 36 : 52;
    int TamFuente => modoCompacto ? 15 : 20;

    static readonly string[] ACCIONES = { "Move", "Look", "Attack", "Interact", "Crouch", "Jump", "Sprint", "Previous", "Next" };

    void Awake()
    {
        if (actions == null)
            actions = Resources.Load<InputActionAsset>("InputSystem_Actions");
        if (actions == null)
        {
#if UNITY_EDITOR
            actions = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
#endif
        }
    }

    void OnEnable()
    {
        if (actions != null && !string.IsNullOrEmpty(SettingsManager.Instance.inputOverridesJson))
        {
            try { actions.LoadBindingOverridesFromJson(SettingsManager.Instance.inputOverridesJson); }
            catch { }
        }
        mapa = actions != null ? actions.FindActionMap(mapName) : null;
        mapa?.Enable();
    }

    void OnDisable()
    {
        mapa?.Disable();
    }

    /// <summary>Construye la lista de reasignación dentro del contenedor dado.</summary>
    public void Construir(Transform padre)
    {
        filasPadre = padre;
        if (actions == null || mapa == null) return;
        foreach (var nombre in ACCIONES)
        {
            var action = mapa.FindAction(nombre);
            if (action == null) continue;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].isComposite || action.bindings[i].isPartOfComposite)
                    continue; // composites (WASD) se muestran como fila única abajo
                CrearFila(action, i);
            }
            if (nombre == "Move")
                CrearFilaComposite(action);
        }
        var btnReset = TerminalTheme.CrearBoton("Btn_ResetControles", padre, "RESTABLECER CONTROLES", Restablecer, new Vector2(400, 32));
        btnReset.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, (filasPadre.childCount + 1) * -PasoFila);
    }

    void CrearFila(InputAction action, int bindingIndex)
    {
        int y = filasPadre.childCount * -PasoFila;
        var fila = new GameObject("Fila_" + action.name + bindingIndex);
        fila.transform.SetParent(filasPadre, false);
        var frt = fila.AddComponent<RectTransform>();
        frt.anchoredPosition = new Vector2(0, y);
        frt.sizeDelta = new Vector2(540, 32);

        var etiqueta = TerminalTheme.CrearTexto("Nombre", fila.transform, action.name, TamFuente, TerminalTheme.Blanco);
        etiqueta.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, 0);
        etiqueta.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30);

        var btn = TerminalTheme.CrearBoton("Rebind_" + action.name + bindingIndex, fila.transform, action.GetBindingDisplayString(bindingIndex), null, new Vector2(260, 30));
        btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(130, 0);
        int bi = bindingIndex;
        InputAction act = action;
        btn.onClick.AddListener(() => IniciarRebind(act, bi, btn));
    }

    void CrearFilaComposite(InputAction move)
    {
        // Muestra atajos WASD como texto informativo (el composite Dpad no se reasigna por botón).
        var info = TerminalTheme.CrearTexto("InfoWASD", filasPadre, "Move = WASD / flechas / stick (compuesto)", TamFuente, TerminalTheme.Gris);
        info.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, filasPadre.childCount * -PasoFila);
        info.GetComponent<RectTransform>().sizeDelta = new Vector2(540, 30);
    }

    void IniciarRebind(InputAction action, int bindingIndex, UnityEngine.UI.Button boton)
    {
        if (escuchando) return;
        escuchando = true;
        var texto = boton.GetComponentInChildren<UnityEngine.UI.Text>();
        if (texto != null) texto.text = "> pulsa una tecla...";
        action.Disable();
        int bi = bindingIndex;
        action.PerformInteractiveRebinding(bi)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .OnComplete(op => TerminarRebind(action, bi, boton, op))
            .OnCancel(op => TerminarRebind(action, bi, boton, op))
            .Start();
    }

    void TerminarRebind(InputAction action, int bindingIndex, UnityEngine.UI.Button boton, InputActionRebindingExtensions.RebindingOperation op)
    {
        var texto = boton.GetComponentInChildren<UnityEngine.UI.Text>();
        if (texto != null) texto.text = "> " + action.GetBindingDisplayString(bindingIndex);
        op.Dispose();
        action.Enable();
        escuchando = false;
        GuardarOverrides();
    }

    void GuardarOverrides()
    {
        if (actions == null) return;
        SettingsManager.Instance.inputOverridesJson = actions.SaveBindingOverridesAsJson();
        SettingsManager.Instance.Guardar();
    }

    public void Restablecer()
    {
        if (actions == null) return;
        actions.RemoveAllBindingOverrides();
        SettingsManager.Instance.inputOverridesJson = "";
        SettingsManager.Instance.Guardar();
        // Reconstruir lista
        foreach (Transform c in filasPadre) Destroy(c.gameObject);
        Construir(filasPadre);
    }
}
