using UnityEngine;

public static class RigidbodyCompat
{
    public static Vector3 GetVelocity(Rigidbody rb)
    {
#if UNITY_2024_1_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.linearVelocity;
#endif
    }

    public static void SetVelocity(Rigidbody rb, Vector3 v)
    {
#if UNITY_2024_1_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.linearVelocity = v;
#endif
    }
}
