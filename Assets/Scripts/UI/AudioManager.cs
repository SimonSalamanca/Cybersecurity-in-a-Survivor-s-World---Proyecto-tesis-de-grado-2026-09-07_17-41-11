using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona volúmenes por grupo (Master/Ambiente/Música/Bestias).
/// Los buses se crean con silencio por ahora: genera clips silenciosos y
/// AudioSources en tiempo de ejecución para que los sliders tengan efecto
/// audible futuro sin romper nada. Aplica volúmenes a fuentes registradas.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public enum Grupo { Master, Ambiente, Musica, Bestias }

    readonly Dictionary<Grupo, List<AudioSource>> fuentes = new Dictionary<Grupo, List<AudioSource>>
    {
        { Grupo.Master, new List<AudioSource>() },
        { Grupo.Ambiente, new List<AudioSource>() },
        { Grupo.Musica, new List<AudioSource>() },
        { Grupo.Bestias, new List<AudioSource>() },
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        CrearBusesSilenciosos();
        Aplicar();
    }

    /// <summary>Buses con silencio: 1 fuente en loop por grupo con clip mudo.</summary>
    void CrearBusesSilenciosos()
    {
        foreach (Grupo g in System.Enum.GetValues(typeof(Grupo)))
        {
            var go = new GameObject("Bus_" + g);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.clip = ClipSilencio(1f);
            src.loop = true;
            src.playOnAwake = false;
            Registrar(src, g);
        }
    }

    public static AudioClip ClipSilencio(float segundos)
    {
        int rate = 44100;
        var clip = AudioClip.Create("Silencio", Mathf.Max(1, (int)(rate * segundos)), 1, rate, false);
        clip.SetData(new float[clip.samples], 0);
        return clip;
    }

    public void Registrar(AudioSource src, Grupo grupo)
    {
        if (src != null && !fuentes[grupo].Contains(src))
            fuentes[grupo].Add(src);
    }

    public float VolumenGrupo(Grupo g)
    {
        var s = SettingsManager.Instance != null ? SettingsManager.Instance.audio : new SettingsManager.AudioSettings();
        if (s.mute) return 0f;
        float master = s.master;
        switch (g)
        {
            case Grupo.Ambiente: return master * s.ambiente;
            case Grupo.Musica: return master * s.musica;
            case Grupo.Bestias: return master * s.bestias;
            default: return master;
        }
    }

    public void Aplicar()
    {
        foreach (var kv in fuentes)
        {
            float v = VolumenGrupo(kv.Key);
            foreach (var src in kv.Value)
                if (src != null) src.volume = v;
        }
        var s = SettingsManager.Instance != null ? SettingsManager.Instance.audio : null;
        AudioListener.volume = (s != null && s.mute) ? 0f : 1f;
    }

    public void SetVolumen(Grupo g, float v)
    {
        var s = SettingsManager.Instance.audio;
        v = Mathf.Clamp01(v);
        switch (g)
        {
            case Grupo.Master: s.master = v; break;
            case Grupo.Ambiente: s.ambiente = v; break;
            case Grupo.Musica: s.musica = v; break;
            case Grupo.Bestias: s.bestias = v; break;
        }
        Aplicar();
    }
}
