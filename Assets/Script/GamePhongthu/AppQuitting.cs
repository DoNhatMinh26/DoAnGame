using UnityEngine;

public class AppQuitting : MonoBehaviour
{
    public static bool isQuitting = false;

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }
}