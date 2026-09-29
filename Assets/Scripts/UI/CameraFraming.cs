using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Impone el encuadre bueno del menú laptop al cargar la escena Y cada vez
/// que se vuelve a ella (menú → Jugar → pausa → Salir al menú), para que
/// todos los caminos encuadren idéntico. Valores de la escena ya ubicada.
/// </summary>
[DefaultExecutionOrder(100)]
public class CameraFraming : MonoBehaviour
{
    public Vector3 posicion = new Vector3(0f, 0.431f, -0.196f);
    public Vector3 rotacionEuler = new Vector3(8.38f, 0f, 0f);
    public float fov = 30f;

    void OnEnable()
    {
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    void Start()
    {
        Aplicar("Start");
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (escena.name == "MainMenu_Laptop")
            Aplicar("sceneLoaded");
    }

    void Aplicar(string origen)
    {
        var cam = GetComponent<Camera>();
        transform.position = posicion;
        transform.rotation = Quaternion.Euler(rotacionEuler);
        if (cam != null) cam.fieldOfView = fov;
        Debug.Log("[CameraFraming] Encuadre aplicado desde " + origen + ": pos=" + posicion + " fov=" + fov);
    }
}
