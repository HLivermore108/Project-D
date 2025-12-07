using UnityEngine;

public static class FindHelpers
{
    // Finds the first instance of a type in a way that avoids the deprecated API.
    public static T FindGameManager<T>() where T : Object
    {
#if UNITY_2023_2_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
