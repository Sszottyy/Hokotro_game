using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;
using System.Collections;

public class NetworkFunctions : MonoBehaviour
{
    [Header("Connection UI")]
    public TMP_InputField ipInput;
    public TMP_InputField portInput;
    private MainMenu mainMenu;
    private UnityTransport transport;

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is NULL!");
            return;
        }

        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (transport == null)
        {
            Debug.LogError("UnityTransport component missing from NetworkManager!");
        }
        mainMenu = FindObjectOfType<MainMenu>(true);

        NetworkManager.Singleton.OnClientDisconnectCallback +=
            OnClientDisconnected;
    }

    public void StartHost()
    {
        if (transport == null)
        {
            Debug.LogError("Transport is NULL!");
            return;
        }

        ushort port = transport.ConnectionData.Port;

        string portText = portInput.text;

        if (!string.IsNullOrEmpty(portText))
        {
            if (!ushort.TryParse(portText, out port))
            {
                Debug.LogError("Invalid port!");
                return;
            }
        }

        transport.SetConnectionData("0.0.0.0", port);

        Debug.Log($"HOST STARTING ON PORT: {port}");

        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        if (transport == null)
        {
            Debug.LogError("Transport is NULL!");
            return;
        }

        string ip = ipInput.text;

        if (string.IsNullOrEmpty(ip))
        {
            ip = "127.0.0.1";
        }

        ushort port = transport.ConnectionData.Port;

        string portText = portInput.text;

        if (!string.IsNullOrEmpty(portText))
        {
            if (!ushort.TryParse(portText, out port))
            {
                Debug.LogError("Invalid port!");
                return;
            }
        }

        transport.SetConnectionData(ip, port);

        Debug.Log($"CLIENT CONNECTING TO: {ip}:{port}");

        NetworkManager.Singleton.StartClient();
    }


    private void OnClientDisconnected(ulong clientId)
    {
        // csak a saját kliens disconnectje érdekel
        if (clientId != NetworkManager.Singleton.LocalClientId)
            return;

        Debug.Log("[NETWORK] Connection failed or disconnected");

        if (mainMenu != null)
        {
            mainMenu.ReturnToMainMenu();
        }
    }
    public void Disconnect()
    {
        StartCoroutine(DisconnectRoutine());
    }

    private IEnumerator DisconnectRoutine()
    {
        if (NetworkManager.Singleton == null)
        {
            yield break;
        }

        // Szólunk a szervernek, hogy törölje a játékost
        if (LobbyNetworkHandler.Instance != null &&
            (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost))
        {
            LobbyNetworkHandler.Instance.RemovePlayerServerRpc(
                NetworkManager.Singleton.LocalClientId
            );
        }

        // Kis várakozás, hogy az RPC átérjen
        yield return new WaitForSeconds(0.2f);

        Debug.Log("Shutting down network session...");

        NetworkManager.Singleton.Shutdown();

        // Hostnál lobby reset
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Players.Clear();
        }
    }
}