using UnityEngine;

/// <summary>
/// Asegura que los managers persistentes existan en cualquier escena.
/// Colocar en un GameObject "Boot_Managers" de MainMenu_Laptop y SampleScene.
/// </summary>
public class Boot_Managers : MonoBehaviour
{
    void Awake()
    {
        CrearSiFalta<SettingsManager>("Managers_Settings");
        CrearSiFalta<AudioManager>("Managers_Audio");
        CrearSiFalta<GraphicsManager>("Managers_Graphics");
        CrearSiFalta<GameplaySettings>("Managers_Gameplay");
    }

    static void CrearSiFalta<T>(string nombre) where T : Component
    {
        if (FindFirstObjectByType<T>() == null)
        {
            var go = new GameObject(nombre);
            go.AddComponent<T>();
        }
    }
}
