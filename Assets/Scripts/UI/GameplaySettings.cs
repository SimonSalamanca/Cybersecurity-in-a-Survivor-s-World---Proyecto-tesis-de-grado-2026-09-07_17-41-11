using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ajustes de gameplay para FPS: sensibilidad de ratón, distancia de cámara
/// (desplazamiento aplicable al rig cuando exista), modo fotosensible que
/// reduce flashes/luces parpadeantes, invertir Y y subtítulos.
/// </summary>
public class GameplaySettings : MonoBehaviour
{
    public static GameplaySettings Instance { get; private set; }

    /// <summary>
    /// Luces/efectos que parpadean deben registrarse aquí para que el modo
    /// fotosensible pueda atenuarlos. Aún sin objetos afectados: registro listo.
    /// </summary>
    public static readonly List<Light> LucesParpadeantes = new List<Light>();
    public static readonly List<string> EfectosConFlash = new List<string>();

    // Intensidad original guardada por instancia para restaurar.
    static readonly Dictionary<Light, float> intensidadBase = new Dictionary<Light, float>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() => Aplicar();

    public static void RegistrarLuzParpadeante(Light l)
    {
        if (l != null && !LucesParpadeantes.Contains(l))
        {
            LucesParpadeantes.Add(l);
            if (!intensidadBase.ContainsKey(l)) intensidadBase[l] = l.intensity;
        }
    }

    public void Aplicar()
    {
        var s = SettingsManager.Instance != null ? SettingsManager.Instance.gameplay : new SettingsManager.GameplaySettings();
        AplicarModoFotosensible(s.modoFotosensible);
        // distanciaCamara y sensibilidad se consumen en el controlador FPS futuro
        // vía SettingsManager.Instance.gameplay.
    }

    public void AplicarModoFotosensible(bool activo)
    {
        foreach (var l in LucesParpadeantes)
        {
            if (l == null) continue;
            if (!intensidadBase.ContainsKey(l)) intensidadBase[l] = l.intensity;
            if (activo)
            {
                // Atenúa a la mitad y apaga parpadeo: el parpadeo futuro debe
                // consultar ModoFotosensibleActivo() antes de modular intensidad.
                l.intensity = intensidadBase[l] * 0.5f;
            }
            else
            {
                l.intensity = intensidadBase[l];
            }
        }
    }

    public static bool ModoFotosensibleActivo()
    {
        return SettingsManager.Instance != null && SettingsManager.Instance.gameplay.modoFotosensible;
    }
}
