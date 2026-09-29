using System;
using UnityEngine;

/// <summary>
/// Almacén central de ajustes con persistencia en PlayerPrefs.
/// Singleton persistente entre escenas (DontDestroyOnLoad).
/// Los managers específicos (audio, gráficos, gameplay) leen/escriben aquí.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Serializable]
    public class AudioSettings
    {
        [Range(0f, 1f)] public float master = 1f;
        [Range(0f, 1f)] public float ambiente = 0.8f;
        [Range(0f, 1f)] public float musica = 0.8f;
        [Range(0f, 1f)] public float bestias = 0.8f;
        public bool mute = false;
    }

    [Serializable]
    public class GraphicsSettings
    {
        public int qualityIndex = 1; // 0 Mobile, 1 PC
        public int resolutionIndex = -1; // -1 = nativa
        public int fullscreenMode = 1; // 0 Ventana, 1 Pantalla completa, 2 Completa sin bordes
        public int vsync = 1;
        [Range(0.5f, 1.5f)] public float brillo = 1f;
        [Range(10f, 200f)] public float distanciaSombras = 40f;
        [Range(0.5f, 2f)] public float lodBias = 1f;
        [Range(60f, 90f)] public float fov = 70f;
    }

    [Serializable]
    public class GameplaySettings
    {
        [Range(0.1f, 5f)] public float sensibilidadRaton = 1f;
        [Range(0f, 2f)] public float distanciaCamara = 0f; // FPS: desplazamiento aplicable a rig futuro
        public bool modoFotosensible = false; // reduce flashes y luces parpadeantes
        public bool invertirY = false;
        public bool subtitulos = true;
    }

    public AudioSettings audio = new AudioSettings();
    public GraphicsSettings graficos = new GraphicsSettings();
    public GameplaySettings gameplay = new GameplaySettings();
    public string inputOverridesJson = "";

    public event Action OnSettingsChanged;

    const string K_MASTER = "set.audio.master";
    const string K_AMB = "set.audio.ambiente";
    const string K_MUS = "set.audio.musica";
    const string K_BES = "set.audio.bestias";
    const string K_MUTE = "set.audio.mute";
    const string K_Q = "set.gfx.quality";
    const string K_RES = "set.gfx.res";
    const string K_FS = "set.gfx.fs";
    const string K_VSYNC = "set.gfx.vsync";
    const string K_BRI = "set.gfx.brillo";
    const string K_SH = "set.gfx.shadows";
    const string K_LOD = "set.gfx.lod";
    const string K_FOV = "set.gfx.fov";
    const string K_SENS = "set.gp.sens";
    const string K_CAM = "set.gp.camdist";
    const string K_FOTO = "set.gp.foto";
    const string K_INVY = "set.gp.invy";
    const string K_SUB = "set.gp.sub";
    const string K_INPUT = "set.input.overrides";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Cargar();
    }

    public void Cargar()
    {
        audio.master = PlayerPrefs.GetFloat(K_MASTER, 1f);
        audio.ambiente = PlayerPrefs.GetFloat(K_AMB, 0.8f);
        audio.musica = PlayerPrefs.GetFloat(K_MUS, 0.8f);
        audio.bestias = PlayerPrefs.GetFloat(K_BES, 0.8f);
        audio.mute = PlayerPrefs.GetInt(K_MUTE, 0) == 1;
        graficos.qualityIndex = PlayerPrefs.GetInt(K_Q, 1);
        graficos.resolutionIndex = PlayerPrefs.GetInt(K_RES, -1);
        graficos.fullscreenMode = PlayerPrefs.GetInt(K_FS, 1);
        graficos.vsync = PlayerPrefs.GetInt(K_VSYNC, 1);
        graficos.brillo = PlayerPrefs.GetFloat(K_BRI, 1f);
        graficos.distanciaSombras = PlayerPrefs.GetFloat(K_SH, 40f);
        graficos.lodBias = PlayerPrefs.GetFloat(K_LOD, 1f);
        graficos.fov = PlayerPrefs.GetFloat(K_FOV, 70f);
        gameplay.sensibilidadRaton = PlayerPrefs.GetFloat(K_SENS, 1f);
        gameplay.distanciaCamara = PlayerPrefs.GetFloat(K_CAM, 0f);
        gameplay.modoFotosensible = PlayerPrefs.GetInt(K_FOTO, 0) == 1;
        gameplay.invertirY = PlayerPrefs.GetInt(K_INVY, 0) == 1;
        gameplay.subtitulos = PlayerPrefs.GetInt(K_SUB, 1) == 1;
        inputOverridesJson = PlayerPrefs.GetString(K_INPUT, "");
    }

    public void Guardar()
    {
        PlayerPrefs.SetFloat(K_MASTER, audio.master);
        PlayerPrefs.SetFloat(K_AMB, audio.ambiente);
        PlayerPrefs.SetFloat(K_MUS, audio.musica);
        PlayerPrefs.SetFloat(K_BES, audio.bestias);
        PlayerPrefs.SetInt(K_MUTE, audio.mute ? 1 : 0);
        PlayerPrefs.SetInt(K_Q, graficos.qualityIndex);
        PlayerPrefs.SetInt(K_RES, graficos.resolutionIndex);
        PlayerPrefs.SetInt(K_FS, graficos.fullscreenMode);
        PlayerPrefs.SetInt(K_VSYNC, graficos.vsync);
        PlayerPrefs.SetFloat(K_BRI, graficos.brillo);
        PlayerPrefs.SetFloat(K_SH, graficos.distanciaSombras);
        PlayerPrefs.SetFloat(K_LOD, graficos.lodBias);
        PlayerPrefs.SetFloat(K_FOV, graficos.fov);
        PlayerPrefs.SetFloat(K_SENS, gameplay.sensibilidadRaton);
        PlayerPrefs.SetFloat(K_CAM, gameplay.distanciaCamara);
        PlayerPrefs.SetInt(K_FOTO, gameplay.modoFotosensible ? 1 : 0);
        PlayerPrefs.SetInt(K_INVY, gameplay.invertirY ? 1 : 0);
        PlayerPrefs.SetInt(K_SUB, gameplay.subtitulos ? 1 : 0);
        PlayerPrefs.SetString(K_INPUT, inputOverridesJson ?? "");
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public void NotificarCambio() => OnSettingsChanged?.Invoke();
}
