import re

# Leer el archivo
with open('Assets/Scripts/Systems/TaskManager.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# El nuevo método a insertar
new_method = '''
    // Nuevo método que acepta el tag directamente (llamado por TrashObject antes de destruirse)
    public void NotifyTrashCleanedWithTag(string itemName, string tag)
    {
        if (gameEnded) return;

        string objectId = FindObjectIdByName(itemName);
        if (string.IsNullOrEmpty(objectId)) objectId = objectRegistry.Keys.FirstOrDefault(key => key.Contains(itemName));

        if (!string.IsNullOrEmpty(objectId) && remainingItemNames.Contains(objectId))
        {
            // Incrementar el contador específico según el tag
            if (tag == "Vidrio") cleanedGlass++;
            else if (tag == "Papeles") cleanedPaper++;
            else if (tag == "Plastico") cleanedPlastic++;
            else if (tag == "Peligrosos") cleanedHazardous++;
            else if (tag == "Bolsas" || tag == "Trash") cleanedBolsas++;

            Debug.Log($"📊 [{tag}] Limpiado → V:{cleanedGlass}/{totalGlass} P:{cleanedPaper}/{totalPaper} Pl:{cleanedPlastic}/{totalPlastic} Pe:{cleanedHazardous}/{totalHazardous} B:{cleanedBolsas}/{totalBolsas}");

            cleanedTrashItems++;
            remainingItemNames.Remove(objectId);
            objectRegistry.Remove(objectId);
            CheckCompletion();
        }
        else
        {
            Debug.LogWarning($"⚠️ Objeto {itemName} (Tag: {tag}) no encontrado en el registro");
        }
    }
'''

# Buscar el patrón y reemplazar
pattern = r'(\s+}\r?\n\r?\n\s+public void NotifyTrashCleaned\(string itemName\))'
replacement = r'}' + new_method + r'\n\n    public void NotifyTrashCleaned(string itemName)'

content = re.sub(pattern, replacement, content)

# Escribir el archivo
with open('Assets/Scripts/Systems/TaskManager.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print("✅ Método agregado exitosamente")
