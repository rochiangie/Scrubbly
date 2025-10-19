// Scripts/Objects/TrashObject.cs

using UnityEngine;

// Asegúrate de que los objetos de basura tengan el Tag "Basura" y estén en el Layer "Interactable".
public class TrashObject : MonoBehaviour
{
    // Función pública llamada por el script del jugador para iniciar la destrucción
    public void EliminateTrash()
    {
        Debug.Log($"Basura '{gameObject.name}' eliminada al instante.");

        // Aquí podrías añadir un efecto de sonido o partículas antes de la destrucción
        // ...

        // Destruye este objeto
        Destroy(gameObject);
    }
}