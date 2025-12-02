using UnityEngine;
using UnityEditor;

public class TrashCanDebugger : EditorWindow
{
    [MenuItem("Tools/Debug Trash Cans")]
    public static void ShowWindow()
    {
        GetWindow<TrashCanDebugger>("Trash Can Debugger");
    }

    private void OnGUI()
    {
        GUILayout.Label("Basureros en la Escena", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Listar Todos los Basureros"))
        {
            ListAllTrashCans();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Configurar Automáticamente por Color"))
        {
            AutoConfigureByColor();
        }

        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "1. 'Listar Todos' muestra la configuración actual de cada basurero.\n" +
            "2. 'Configurar Automáticamente' intenta configurar los tags basándose en el color del basurero.",
            MessageType.Info
        );
    }

    private void ListAllTrashCans()
    {
        TrashCan[] trashCans = FindObjectsOfType<TrashCan>();
        
        Debug.Log("=== 🗑️ BASUREROS EN LA ESCENA ===");
        Debug.Log($"Total encontrados: {trashCans.Length}\n");

        for (int i = 0; i < trashCans.Length; i++)
        {
            TrashCan can = trashCans[i];
            string tags = can.acceptedTrashTags != null && can.acceptedTrashTags.Length > 0 
                ? string.Join(", ", can.acceptedTrashTags) 
                : "NINGUNO";
            
            Color color = can.binColor;
            string colorName = GetColorName(color);

            Debug.Log($"[{i + 1}] {can.gameObject.name}\n" +
                     $"   📍 Posición: {can.transform.position}\n" +
                     $"   🎨 Color: {colorName} (R:{color.r:F2} G:{color.g:F2} B:{color.b:F2})\n" +
                     $"   🏷️ Display Name: {can.displayName}\n" +
                     $"   ✅ Tags Aceptados: [{tags}]\n");
        }
    }

    private void AutoConfigureByColor()
    {
        TrashCan[] trashCans = FindObjectsOfType<TrashCan>();
        int configured = 0;

        foreach (TrashCan can in trashCans)
        {
            Color color = can.binColor;
            string[] newTags = null;
            string newDisplayName = null;

            // Detectar por color (aproximado)
            if (IsColorClose(color, Color.green, 0.3f))
            {
                newTags = new string[] { "Vidrio" };
                newDisplayName = "VIDRIO";
            }
            else if (IsColorClose(color, Color.yellow, 0.3f))
            {
                newTags = new string[] { "Plastico" };
                newDisplayName = "PLÁSTICO";
            }
            else if (IsColorClose(color, Color.blue, 0.3f))
            {
                newTags = new string[] { "Papeles" };
                newDisplayName = "PAPEL / CARTÓN";
            }
            else if (IsColorClose(color, Color.red, 0.3f))
            {
                newTags = new string[] { "Peligrosos" };
                newDisplayName = "PELIGROSOS";
            }
            else if (IsColorClose(color, Color.gray, 0.3f) || IsColorClose(color, Color.black, 0.3f))
            {
                newTags = new string[] { "Trash", "Bolsas" };
                newDisplayName = "RESIDUOS";
            }

            if (newTags != null)
            {
                Undo.RecordObject(can, "Auto-configure TrashCan");
                can.acceptedTrashTags = newTags;
                can.displayName = newDisplayName;
                EditorUtility.SetDirty(can);
                configured++;
                
                Debug.Log($"✅ Configurado: {can.gameObject.name} → {newDisplayName} [{string.Join(", ", newTags)}]");
            }
            else
            {
                Debug.LogWarning($"⚠️ No se pudo determinar el tipo de: {can.gameObject.name} (Color: {color})");
            }
        }

        Debug.Log($"\n🎉 Configuración automática completada: {configured}/{trashCans.Length} basureros configurados.");
        
        if (configured > 0)
        {
            EditorUtility.DisplayDialog("Configuración Completa", 
                $"Se configuraron {configured} de {trashCans.Length} basureros.\n\nRevisa la consola para más detalles.", 
                "OK");
        }
    }

    private string GetColorName(Color color)
    {
        if (IsColorClose(color, Color.red, 0.2f)) return "Rojo";
        if (IsColorClose(color, Color.green, 0.2f)) return "Verde";
        if (IsColorClose(color, Color.blue, 0.2f)) return "Azul";
        if (IsColorClose(color, Color.yellow, 0.2f)) return "Amarillo";
        if (IsColorClose(color, Color.gray, 0.2f)) return "Gris";
        if (IsColorClose(color, Color.black, 0.2f)) return "Negro";
        if (IsColorClose(color, Color.white, 0.2f)) return "Blanco";
        return "Personalizado";
    }

    private bool IsColorClose(Color a, Color b, float threshold)
    {
        float distance = Mathf.Sqrt(
            Mathf.Pow(a.r - b.r, 2) +
            Mathf.Pow(a.g - b.g, 2) +
            Mathf.Pow(a.b - b.b, 2)
        );
        return distance < threshold;
    }
}
