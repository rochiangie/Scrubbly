using UnityEngine;
using UnityEngine.UI; // NECESARIO para usar la clase 'Text'
using System.Collections;
using TMPro; // Opcional: Descomenta si usas TextMeshPro (Recomendado)

public class csShowAllEffect : MonoBehaviour
{
    // ================== VARIABLES ==================

    [Header("Efectos")]
    public string[] EffectNames;
    public string[] Effect2Names;
    public Transform[] Effect; // Prefabs de los efectos

    [Header("UI")]
    // CORRECCIÓN: Reemplazamos GUIText por Text (UI Canvas) o TextMeshProUGUI
    public Text Text1; // O usa public TextMeshProUGUI Text1; si usas TextMeshPro

    // El índice 'i' debe ser privado y ajustado al tamaño de la lista
    private int currentIndex = 0;

    // Variables de control de bucle (no es bueno usarlas como variables de clase, pero las mantendremos)
    private int a = 0;


    // ================== UNITY LIFECYCLE ==================

    void Start()
    {
        // 1. Validar para evitar errores de índice
        if (Effect.Length == 0) return;

        // 2. Inicializar el índice al inicio de la lista
        currentIndex = 0;

        // 3. Mostrar el primer efecto
        ShowCurrentEffect();
    }


    void Update()
    {
        // 1. Actualizar el texto del UI
        if (Text1 != null && EffectNames.Length > currentIndex)
        {
            // Se usa String.Format para asegurar que la concatenación sea correcta
            Text1.text = string.Format("{0}: {1}", currentIndex + 1, EffectNames[currentIndex]);
        }

        // 2. Controlar la navegación de efectos
        if (Input.GetKeyDown(KeyCode.Z)) // Efecto Anterior
        {
            currentIndex--;
            if (currentIndex < 0)
                currentIndex = EffectNames.Length - 1; // Envuelve al final de la lista

            ShowCurrentEffect();
        }

        if (Input.GetKeyDown(KeyCode.X)) // Siguiente Efecto
        {
            currentIndex++;
            if (currentIndex >= EffectNames.Length)
                currentIndex = 0; // Envuelve al inicio de la lista

            ShowCurrentEffect();
        }

        if (Input.GetKeyDown(KeyCode.C)) // Reinstanciar Efecto Actual
        {
            ShowCurrentEffect();
        }
    }


    // ================== LÓGICA DE INSTANCIACIÓN ==================

    void ShowCurrentEffect()
    {
        if (Effect.Length == 0) return;
        if (currentIndex < 0 || currentIndex >= Effect.Length) return;

        // 1. Verificar si este efecto requiere una posición especial
        bool needsSpecialPosition = false;

        // El bucle original buscaba EffectNames[i] en Effect2Names
        for (a = 0; a < Effect2Names.Length; a++)
        {
            // Usamos un control de límites simple
            if (EffectNames.Length > currentIndex && Effect2Names.Length > a)
            {
                if (EffectNames[currentIndex] == Effect2Names[a])
                {
                    needsSpecialPosition = true;
                    break;
                }
            }
        }

        // 2. Instanciar el efecto
        if (needsSpecialPosition)
        {
            // Instancia en una posición baja (asumimos 0, 0.01f, 0)
            Instantiate(Effect[currentIndex], new Vector3(0, 0.01f, 0), Quaternion.identity);
        }
        else
        {
            // Instancia en la posición normal (asumimos 0, 5, 0)
            Instantiate(Effect[currentIndex], new Vector3(0, 5, 0), Quaternion.identity);
        }
    }
}