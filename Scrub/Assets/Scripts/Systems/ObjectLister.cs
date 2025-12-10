using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ObjectLister : MonoBehaviour
{
    [Header("Resumen de Cantidades")]
    public int totalObjects = 0;
    public int totalTrash = 0;
    public int totalDirt = 0;

    [Header("Listas por Categoría")]
    public List<GameObject> vidrioList = new List<GameObject>();
    public List<GameObject> plasticoList = new List<GameObject>();
    public List<GameObject> papelesList = new List<GameObject>();
    public List<GameObject> peligrososList = new List<GameObject>();
    public List<GameObject> organicoList = new List<GameObject>();
    public List<GameObject> bolsasList = new List<GameObject>();
    public List<GameObject> manchasList = new List<GameObject>();
    public List<GameObject> memoriesList = new List<GameObject>();
    public List<GameObject> sinCategoriaList = new List<GameObject>();

    void Start()
    {
        ListAllObjects();
    }

    [ContextMenu("Listar Objetos Ahora")]
    public void ListAllObjects()
    {
        // 1. Limpiar listas anteriores
        ClearLists();

        // 2. Buscar TODOS los objetos relevantes
        // Buscamos por componentes comunes para no dejarnos nada
        var allTrash = FindObjectsByType<TrashObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var allDirt = FindObjectsByType<DirtSpot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var allMemories = FindObjectsByType<MemorieObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        // También buscamos por tag para asegurar objetos que quizás no tengan el script TrashObject pero sí el tag
        var allCarryables = FindObjectsByType<Carryable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        // 3. Clasificar Basura y Objetos
        HashSet<GameObject> processedObjects = new HashSet<GameObject>();

        // Procesar TrashObjects
        foreach (var obj in allTrash)
        {
            if (processedObjects.Contains(obj.gameObject)) continue;
            CategorizeObject(obj.gameObject);
            processedObjects.Add(obj.gameObject);
        }

        // Procesar Carryables (por si hay alguno que no sea TrashObject)
        foreach (var obj in allCarryables)
        {
            if (processedObjects.Contains(obj.gameObject)) continue;
            CategorizeObject(obj.gameObject);
            processedObjects.Add(obj.gameObject);
        }

        // 4. Clasificar Manchas
        foreach (var dirt in allDirt)
        {
            manchasList.Add(dirt.gameObject);
        }

        // 5. Clasificar Memorias
        foreach (var mem in allMemories)
        {
            memoriesList.Add(mem.gameObject);
        }

        // 6. Calcular Totales
        totalDirt = manchasList.Count;
        totalTrash = vidrioList.Count + plasticoList.Count + papelesList.Count + peligrososList.Count + organicoList.Count + bolsasList.Count + sinCategoriaList.Count;
        totalObjects = totalTrash + totalDirt + memoriesList.Count;

        Debug.Log($"[ObjectLister] Listado completado. Total Objetos: {totalObjects} (Basura: {totalTrash}, Manchas: {totalDirt})");
    }

    private void CategorizeObject(GameObject obj)
    {
        string tag = obj.tag;

        switch (tag)
        {
            case "Vidrio": vidrioList.Add(obj); break;
            case "Plastico": plasticoList.Add(obj); break;
            case "Papeles": papelesList.Add(obj); break;
            case "Peligrosos": peligrososList.Add(obj); break;
            case "Organico": organicoList.Add(obj); break;
            case "Bolsas": bolsasList.Add(obj); break;
            case "Memorie": break; // Ya se procesan aparte
            case "Untagged": sinCategoriaList.Add(obj); break;
            default: sinCategoriaList.Add(obj); break;
        }
    }

    private void ClearLists()
    {
        vidrioList.Clear();
        plasticoList.Clear();
        papelesList.Clear();
        peligrososList.Clear();
        organicoList.Clear();
        bolsasList.Clear();
        manchasList.Clear();
        memoriesList.Clear();
        sinCategoriaList.Clear();
        
        totalObjects = 0;
        totalTrash = 0;
        totalDirt = 0;
    }
}
