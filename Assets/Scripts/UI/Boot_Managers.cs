using UnityEngine;

/// <summary>
/// Asegura que los managers persistentes existan en cualquier escena.
/// Colocar en un GameObject "Boot_Managers" de MainMenu_Laptop y Oficina.
/// La base de datos se crea primero: los demás sistemas pueden depender de ella.
/// </summary>
public class Boot_Managers : MonoBehaviour
{
    void Awake()
    {
        CrearSiFalta<GestorBaseDatos>("Managers_BaseDatos");
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
