using UnityEngine;

public class PersistentUI : MonoBehaviour
{
    public static PersistentUI Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // �Esta es la clave! El Canvas y todo lo que contiene (el bot�n) 
            // no se destruir� al cambiar de escena.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Si ya existe una instancia, nos destruimos.
            Destroy(gameObject);
        }
    }
}