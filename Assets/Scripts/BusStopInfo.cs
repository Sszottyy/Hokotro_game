using Unity.Netcode;
using UnityEngine;

public class BusStopInfo : MonoBehaviour
{
    public NetworkVariable<int> segmentId = new NetworkVariable<int>(-1);
}
