using UnityEngine;

/// <summary>
/// Contiene la información de cada personaje para el Lore y la carga.
/// Debe estar en cada GameObject de personaje en la escena 'SeleccionPersonaje'.
/// </summary>
public class CharacterData : MonoBehaviour
{
    [Tooltip("Nombre del Prefab a cargar en la escena de juego (Ej: 'PlayerPrefabA').")]
    public string prefabName;

    [Tooltip("El texto de Lore/Historia que se mostrará en el Canvas.")]
    [TextArea(5, 10)]
    public string loreText;
}