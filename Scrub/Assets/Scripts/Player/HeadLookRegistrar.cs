using UnityEngine;
using System.Collections; // Necesario para usar Coroutines

public class HeadLookRegistrar : MonoBehaviour
{
    private void Start()
    {
        // En lugar de buscar inmediatamente en Start(), iniciamos una Coroutine.
        // Esto garantiza que al menos un frame de Unity ha pasado despu�s de la instanciaci�n.
        StartCoroutine(TryRegisterHead());
    }

    private IEnumerator TryRegisterHead()
    {
        // 1. Esperar un frame
        // Esto es CR�TICO para asegurar que el componente padre est� completamente activo.
        yield return null;

        // 2. Buscar el controlador en el padre
        // Busca en el componente padre m�s cercano, o sube en la jerarqu�a.
        MouseLookController controller = GetComponentInParent<MouseLookController>();

        if (controller != null)
        {
            // 3. Asignar el target
            // Usamos la funci�n p�blica que definimos en MouseLookController.cs
            controller.SetHeadTarget(this.transform);
            Debug.Log("[HeadRegistrar] Registro de cabeza exitoso despu�s de instanciaci�n.");

            // Opcional: Eliminar este script una vez que ha cumplido su funci�n.
            Destroy(this);
        }
        else
        {
            // Nota: El GetComponentInParent es lento si se usa en Update, pero es seguro aqu�.
            Debug.LogError("[HeadRegistrar] �FALLO CR�TICO! No se encontr� el MouseLookController en el padre. Verifica que el script est� en la ra�z del personaje instanciado.");
        }
    }
}