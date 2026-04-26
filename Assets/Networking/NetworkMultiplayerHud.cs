using Unity.Netcode;
using UnityEngine;

public class NetworkMultiplayerHud : MonoBehaviour
{
    public static NetworkPlayerController2D FindLocalPlayer()
    {
        if (NetworkManager.Singleton == null)
            return null;

        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        NetworkPlayerController2D[] players = FindObjectsByType<NetworkPlayerController2D>(FindObjectsSortMode.None);
        foreach (NetworkPlayerController2D player in players)
        {
            if (player.OwnerClientId == localClientId)
                return player;
        }

        return null;
    }
}
