using Unity.Netcode;
using UnityEngine;

/*
 * SessionHud draws the connection controls with immediate-mode GUI and forwards
 * button clicks to SessionManager. Connection status is read from NetworkManager
 * so this stays a thin view layer.
 */

public class SessionHud : MonoBehaviour
{
    [SerializeField] SessionManager _sessionManager;

    const float Margin = 16f;
    const float StatusWidth = 220f;
    const float StatusHeight = 40f;
    const float ControlsWidth = 470f;
    const float ControlsHeight = 40f;
    const float ButtonWidth = 140f;
    const float ButtonHeight = 30f;

    static readonly GUILayoutOption[] ButtonSize =
    {
        GUILayout.Width(ButtonWidth),
        GUILayout.Height(ButtonHeight)
    };

    void OnGUI()
    {
        DrawStatus();
        DrawControls();
    }

    void DrawStatus()
    {
        GUILayout.BeginArea(new Rect(Margin, Margin, StatusWidth, StatusHeight));
        GUILayout.Label(IsConnected ? $"Connected: {LocalRole}" : "Disconnected");
        GUILayout.EndArea();
    }

    void DrawControls()
    {
        float centerX = (Screen.width - ControlsWidth) * 0.5f;
        GUILayout.BeginArea(new Rect(centerX, Margin, ControlsWidth, ControlsHeight));
        GUILayout.BeginHorizontal();

        if (IsConnected)
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Disconnect", ButtonSize))
                _sessionManager.Disconnect();

            GUILayout.FlexibleSpace();
        }
        else
        {
            if (GUILayout.Button("Host Session", ButtonSize))
                _sessionManager.StartHost();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Join Client", ButtonSize))
                _sessionManager.StartClient();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    bool IsConnected
    {
        get
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && (networkManager.IsClient || networkManager.IsServer);
        }
    }

    string LocalRole
    {
        get
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null) return "Disconnected";
            if (networkManager.IsHost) return "Host";
            if (networkManager.IsServer) return "Server";
            if (networkManager.IsClient) return "Client";

            return "Disconnected";
        }
    }
}
