using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
//using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using TMPro;
using UnityEngine;
using System.Collections;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class NetworkFunctions : MonoBehaviour
{
    [Header("Relay UI")]
    public TMP_InputField joinCodeInput;
    public TMP_Text joinCodeText;

    private MainMenu mainMenu;
    private UnityTransport transport;

    private async void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is NULL!");
            return;
        }

        transport =
            NetworkManager.Singleton
            .GetComponent<UnityTransport>();

        if (transport == null)
        {
            Debug.LogError(
                "UnityTransport missing!");
            return;
        }

        mainMenu =
            FindAnyObjectByType<MainMenu>();

        NetworkManager.Singleton
            .OnClientDisconnectCallback +=
            OnClientDisconnected;

       

        Debug.Log("[RELAY] READY");
    }

    public async void StartHost()
    {
        try
        {
            Allocation allocation =
                await RelayService.Instance
                .CreateAllocationAsync(4);

            string joinCode =
                await RelayService.Instance
                .GetJoinCodeAsync(
                    allocation.AllocationId);

            Debug.Log(
                $"[RELAY] JOIN CODE: {joinCode}");

            if (joinCodeText != null)
            {
                joinCodeText.text =
                    $"CODE: {joinCode}";
            }

            transport.SetHostRelayData(
    allocation.RelayServer.IpV4,
    (ushort)allocation.RelayServer.Port,
    allocation.AllocationIdBytes,
    allocation.Key,
    allocation.ConnectionData);

           

            NetworkManager.Singleton.StartHost();

            Debug.Log("[RELAY] HOST STARTED");
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"[RELAY HOST ERROR] {e}");
        }
    }

    public async void StartClient()
    {
        try
        {
            string joinCode =
                joinCodeInput.text;

            if (string.IsNullOrEmpty(joinCode))
            {
                Debug.LogError(
                    "Join code empty!");
                return;
            }

            JoinAllocation allocation =
                await RelayService.Instance
                .JoinAllocationAsync(joinCode);

            transport.SetClientRelayData(
     allocation.RelayServer.IpV4,
     (ushort)allocation.RelayServer.Port,
     allocation.AllocationIdBytes,
     allocation.Key,
     allocation.ConnectionData,
     allocation.HostConnectionData);

           

            NetworkManager.Singleton.StartClient();

            Debug.Log(
                "[RELAY] CLIENT CONNECTED");
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"[RELAY CLIENT ERROR] {e}");
        }
    }

    private void OnClientDisconnected(
        ulong clientId)
    {
        if (clientId !=
            NetworkManager.Singleton.LocalClientId)
            return;

        Debug.Log(
            "[NETWORK] Disconnected");

        if (mainMenu != null)
        {
            mainMenu.ReturnToMainMenu();
        }
    }

    public void Disconnect()
    {
        StartCoroutine(
            DisconnectRoutine());
    }

    private IEnumerator DisconnectRoutine()
    {
        if (NetworkManager.Singleton == null)
        {
            yield break;
        }

        if (LobbyNetworkHandler.Instance != null &&
            (NetworkManager.Singleton.IsClient ||
             NetworkManager.Singleton.IsHost))
        {
            LobbyNetworkHandler.Instance
                .RemovePlayerServerRpc(
                    NetworkManager.Singleton
                    .LocalClientId);
        }

        yield return new WaitForSeconds(0.2f);

        Debug.Log(
            "Shutting down network session...");

        NetworkManager.Singleton.Shutdown();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Players.Clear();
        }
    }
}