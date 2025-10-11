using UnityEngine;
using System.Collections; // Necesario para usar Coroutines

public class HeadLookRegistrar : MonoBehaviour
{
    private void Start()
    {
        // En lugar de buscar inmediatamente en Start(), iniciamos una Coroutine.
        // Esto garantiza que al menos un frame de Unity ha pasado después de la instanciación.
        StartCoroutine(TryRegisterHead());
    }

    private IEnumerator TryRegisterHead()
    {
        // 1. Esperar un frame
        // Esto es CRÍTICO para asegurar que el componente padre esté completamente activo.
        yield return null;

        // 2. Buscar el controlador en el padre
        // Busca en el componente padre más cercano, o sube en la jerarquía.
        MouseLookController controller = GetComponentInParent<MouseLookController>();

        if (controller != null)
        {
            // 3. Asignar el target
            // Usamos la función pública que definimos en MouseLookController.cs
            controller.SetHeadTarget(this.transform);
            Debug.Log("[HeadRegistrar] Registro de cabeza exitoso después de instanciación.");

            // Opcional: Eliminar este script una vez que ha cumplido su función.
            Destroy(this);
        }
        else
        {
            // Nota: El GetComponentInParent es lento si se usa en Update, pero es seguro aquí.
            Debug.LogError("[HeadRegistrar] ¡FALLO CRÍTICO! No se encontró el MouseLookController en el padre. Verifica que el script esté en la raíz del personaje instanciado.");
        }
    }
}