with open('Assets/Scripts/Systems/TaskManager.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Encontrar la línea donde está NotifyTrashCleaned
target_line = next((i for i, line in enumerate(lines) if 'public void NotifyTrashCleaned(string itemName)' in line), None)

if target_line is None:
    print("❌ No se encontró el método NotifyTrashCleaned")
    exit(1)

# El nuevo método
new_method = '''    // Nuevo método que acepta el tag directamente (llamado por TrashObject antes de destruirse)
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

# Insertar el nuevo método antes de NotifyTrashCleaned
lines.insert(target_line, new_method)

# Escribir el archivo
with open('Assets/Scripts/Systems/TaskManager.cs', 'w', encoding='utf-8') as f:
    f.writelines(lines)

print(f"✅ Método NotifyTrashCleanedWithTag agregado en la línea {target_line}")
