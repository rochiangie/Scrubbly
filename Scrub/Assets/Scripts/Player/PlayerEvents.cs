using System;
using UnityEngine;

public class PlayerEvents : MonoBehaviour
{
    public static event Action OnPickup;
    public static event Action OnOpenDoor;

    public static void TriggerPickup() => OnPickup?.Invoke();
    public static void TriggerOpenDoor() => OnOpenDoor?.Invoke();
}

