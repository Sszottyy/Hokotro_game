using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;

public class UnityServicesInitializer : MonoBehaviour
{
    async void Awake()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance
                .SignInAnonymouslyAsync();
        }

        Debug.Log("[UNITY SERVICES] READY");
    }
}