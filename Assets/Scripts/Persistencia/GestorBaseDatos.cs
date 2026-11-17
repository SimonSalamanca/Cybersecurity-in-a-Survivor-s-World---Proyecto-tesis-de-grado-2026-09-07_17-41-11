using System;
using System.IO;
using SQLite;
using UnityEngine;

/// <summary>
/// Abre la base local del juego (SQLite) y la deja lista para el resto de sistemas.
///
/// - En el primer arranque copia la base prearmada (esquema v2 + catálogo) desde
///   StreamingAssets a Application.persistentDataPath.
/// - Activa las llaves foráneas en cada conexión (en SQLite son por conexión).
/// - Verifica la versión del esquema (PRAGMA user_version). Si la base del equipo
///   es de otra versión y todavía no tiene sesiones, la reemplaza; si ya tiene
///   sesiones, NO la toca y avisa (requiere migración para no perder datos).
///
/// Se crea desde Boot_Managers, igual que los demás managers persistentes.
/// Ruta: Assets/Scripts/Persistencia/GestorBaseDatos.cs
/// </summary>
public class GestorBaseDatos : MonoBehaviour
{
    public const string NombreArchivo = "inadaptados.db";
    public const int VersionEsquema = 4;

    public static GestorBaseDatos Instance { get; private set; }

    /// <summary>Conexión compartida. Null si la base no se pudo abrir.</summary>
    public SQLiteConnection Conexion { get; private set; }
    public string RutaBase { get; private set; }
    public bool Lista => Conexion != null;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            Inicializar();
            Diagnostico();
        }
        catch (Exception e)
        {
            Conexion = null;
            Debug.LogError("[BD] No se pudo inicializar la base de datos: " + e);
        }
    }

    void Inicializar()
    {
        RutaBase = Path.Combine(Application.persistentDataPath, NombreArchivo);
        string origen = Path.Combine(Application.streamingAssetsPath, NombreArchivo);

        if (!File.Exists(RutaBase))
            CopiarDesdeStreamingAssets(origen);

        Abrir();

        int version = Conexion.ExecuteScalar<int>("PRAGMA user_version");
        if (version == VersionEsquema) return;

        // Versión distinta: solo se reemplaza si no hay datos de sesiones.
        int sesiones = ContarSesiones();
        if (sesiones == 0)
        {
            Debug.LogWarning($"[BD] Esquema local v{version} sin sesiones: se reemplaza por v{VersionEsquema}.");
            Cerrar();
            BorrarArchivos(RutaBase);
            CopiarDesdeStreamingAssets(origen);
            Abrir();
        }
        else
        {
            throw new InvalidOperationException(
                $"La base local es v{version} y tiene {sesiones} sesiones. " +
                $"Se requiere una migración a v{VersionEsquema}; no se modificó el archivo: {RutaBase}");
        }
    }

    void Abrir()
    {
        Conexion = new SQLiteConnection(RutaBase, SQLiteOpenFlags.ReadWrite);
        Conexion.Execute("PRAGMA foreign_keys = ON");
        if (Conexion.ExecuteScalar<int>("PRAGMA foreign_keys") != 1)
            throw new InvalidOperationException("No se pudieron activar las llaves foráneas.");
    }

    int ContarSesiones()
    {
        try { return Conexion.ExecuteScalar<int>("SELECT COUNT(*) FROM sesion_juego"); }
        catch (SQLiteException) { return 0; } // la tabla no existe: base vacía o corrupta
    }

    static void CopiarDesdeStreamingAssets(string origen)
    {
        if (!File.Exists(origen))
            throw new FileNotFoundException("Falta la base prearmada en StreamingAssets.", origen);
        string destino = Path.Combine(Application.persistentDataPath, NombreArchivo);
        File.Copy(origen, destino, overwrite: false);
        Debug.Log("[BD] Base copiada a " + destino);
    }

    static void BorrarArchivos(string ruta)
    {
        foreach (var r in new[] { ruta, ruta + "-wal", ruta + "-shm" })
            if (File.Exists(r)) File.Delete(r);
    }

    /// <summary>Resumen en consola para comprobar que la base quedó bien.</summary>
    void Diagnostico()
    {
        int version = Conexion.ExecuteScalar<int>("PRAGMA user_version");
        int tablas = Conexion.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table'");
        int vistas = Conexion.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'view'");
        int triggers = Conexion.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger'");
        int preguntas = Conexion.ExecuteScalar<int>("SELECT COUNT(*) FROM pregunta");
        int dimensiones = Conexion.ExecuteScalar<int>("SELECT COUNT(*) FROM dimension WHERE activa = 1");
        string sqlite = Conexion.ExecuteScalar<string>("SELECT sqlite_version()");

        Debug.Log($"[BD] OK · {RutaBase}\n" +
                  $"SQLite {sqlite} · esquema v{version} · {tablas} tablas, {vistas} vistas, {triggers} triggers · " +
                  $"{dimensiones} dimensiones activas · {preguntas} preguntas en catálogo");
    }

    void Cerrar()
    {
        Conexion?.Close();
        Conexion = null;
    }

    void OnApplicationQuit() => Cerrar();

    void OnDestroy()
    {
        if (Instance == this) Cerrar();
    }
}
