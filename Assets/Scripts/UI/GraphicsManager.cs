using UnityEngine;

/// <summary>
/// Aplica los ajustes gráficos a Unity/URP y ofrece catálogo de opciones
/// para la UI (resoluciones, modos, calidades, vsync).
/// </summary>
public class GraphicsManager : MonoBehaviour
{
    public static GraphicsManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() => Aplicar();

    public void Aplicar()
    {
        var s = SettingsManager.Instance != null ? SettingsManager.Instance.graficos : new SettingsManager.GraphicsSettings();

        int qi = Mathf.Clamp(s.qualityIndex, 0, QualitySettings.names.Length - 1);
        if (qi != QualitySettings.GetQualityLevel())
            QualitySettings.SetQualityLevel(qi, true);

        QualitySettings.vSyncCount = s.vsync;
        QualitySettings.shadowDistance = s.distanciaSombras;
        QualitySettings.lodBias = s.lodBias;

        var res = ResolucionElegida(s.resolutionIndex);
        var mode = ModoPantalla(s.fullscreenMode);
        if (res.width > 0)
            Screen.SetResolution(res.width, res.height, mode, new RefreshRate { numerator = 60, denominator = 1 });

        RenderSettings.ambientIntensity = s.brillo;

        // El FOV del menú laptop lo impone CameraFraming; aquí solo se toca
        // la cámara fuera de esa escena para no romper su encuadre.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu_Laptop")
        {
            var cam = Camera.main;
            if (cam != null) cam.fieldOfView = s.fov;
        }
    }

    public Resolution[] Resoluciones() => Screen.resolutions;

    public Resolution ResolucionElegida(int index)
    {
        var list = Screen.resolutions;
        if (list.Length == 0) return new Resolution();
        if (index < 0 || index >= list.Length) return Screen.currentResolution;
        return list[index];
    }

    public static FullScreenMode ModoPantalla(int i)
    {
        switch (i)
        {
            case 0: return FullScreenMode.Windowed;
            case 2: return FullScreenMode.FullScreenWindow;
            default: return FullScreenMode.ExclusiveFullScreen;
        }
    }

    public string[] NombresCalidad() => QualitySettings.names;
}
