using Unity.Netcode;

/*
 * SessionManager owns the small session surface: start as host, join as client,
 * and disconnect.
 */

public class SessionManager : NetworkBehaviour
{
    public void Disconnect() => NetworkManager.Singleton.Shutdown();

    public void StartClient() => NetworkManager.Singleton.StartClient();

    public void StartHost() => NetworkManager.Singleton.StartHost();
}
