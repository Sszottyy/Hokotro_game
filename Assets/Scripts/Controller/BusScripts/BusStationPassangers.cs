using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BusStationPassengers : NetworkBehaviour
{
    [Header("Spawning")]
    public GameObject[] passengerPrefabs;
    public float minSpawnInterval = 3f;
    public float maxSpawnInterval = 8f;
    public int maxPassengers = 6;
    public float spawnRadius = 0.7f; // how far from the sign passengers spread

    private List<GameObject> waitingPassengers = new List<GameObject>();
    private Vector3 signPosition;
    private bool isSpawning = false;

    public int PassengerCount => waitingPassengers.Count;

    public void Initialize(Vector3 busSignPosition)
    {
        Debug.Log(
    $"[Station] Initialize instance id: {GetInstanceID()} | obj: {gameObject.name}"
);
        signPosition = busSignPosition;
        Debug.Log(
        $"[Station] Initialize | prefabs null? {passengerPrefabs == null} | count: {(passengerPrefabs == null ? -1 : passengerPrefabs.Length)}"
    );
        StartSpawning();
    }

    public void StartSpawning()
    {
        if (!isSpawning)
        {
            isSpawning = true;
            StartCoroutine(SpawnRoutine());
        }
    }

    public void StopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();
    }

    // Called when bus picks up passengers — returns count and clears them
    public int BoardPassengers()
    {
        Debug.Log(
    $"[Station] BOARD instance id: {GetInstanceID()} | waiting: {waitingPassengers.Count}"
);
        Debug.Log(
    $"[Station] BOARD called on object: {gameObject.name} | waiting: {waitingPassengers.Count}"
);
        if (!Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Only server can board passengers!");
            return 0;
        }
        int count = waitingPassengers.Count;

        foreach (var p in waitingPassengers)
        {
            if (p == null)
                continue;

            NetworkObject no =
                p.GetComponent<NetworkObject>();

            if (no != null && no.IsSpawned)
            {
                no.Despawn(true);
            }
            else
            {
                Destroy(p);
            }
        }

        waitingPassengers.Clear();
        Debug.Log($"[Station] {count} passengers boarded.");
        return count;
    }

    // Called when bus drops off passengers at the other station
    //public void DropOffPassengers(int count)
    //{
    //    Debug.Log($"[Station] {count} passengers dropped off.");
    //    // Could spawn "arrived" passengers here if needed later
    //    BusMovement bus =
    //   FindFirstObjectByType<BusMovement>();

    //    if (bus == null)
    //        return;

    //    if (bus.BusModel == null)
    //        return;

    //    if (bus.BusModel.Owner == null)
    //        return;

    //    if (bus.BusModel.Owner.Team == null)
    //        return;

    //    // bus.BusModel.Owner.Team.Score += count;
    //    bus.BusModel.Owner.Team.AddScore(count);

    //    Debug.Log(
    //        $"[BUS SCORE] Team {bus.BusModel.Owner.Team.Name} +" +
    //        $"{count} score | total = {bus.BusModel.Owner.Team.Score}"
    //    );
    //}
    public void DropOffPassengers(BusMovement bus, int count)
    {
        Debug.Log($"[Station] {count} passengers dropped off.");

        if (bus == null)
            return;

        if (bus.BusModel == null)
            return;

        if (bus.BusModel.Owner == null)
            return;

        if (bus.BusModel.Owner.Team == null)
            return;

        bus.BusModel.Owner.Team.AddScore(count);

        Debug.Log(
            $"[BUS SCORE] Team {bus.BusModel.Owner.Team.Name} +" +
            $"{count} score | total = {bus.BusModel.Owner.Team.Score}"
        );
    }
    private IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            float interval = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(interval);

            if (
            waitingPassengers.Count < maxPassengers &&
            passengerPrefabs != null &&
            passengerPrefabs.Length > 0
                )
            { SpawnPassenger(); }
        }
    }

    private void SpawnPassenger()
    {
        Debug.Log(
    $"[Station] SPAWN instance id: {GetInstanceID()} | waiting: {waitingPassengers.Count}"
);
        waitingPassengers.RemoveAll(p => p == null);
        Debug.Log(
    $"[Station] SpawnPassenger called | prefab count: {passengerPrefabs?.Length}"
);
        if (!Unity.Netcode.NetworkManager.Singleton.IsServer)
            return;
        if (passengerPrefabs == null || passengerPrefabs.Length == 0) return;

        Vector3 offset = new Vector3(
            Random.Range(-spawnRadius, spawnRadius),
            Random.Range(-spawnRadius * 0.3f, spawnRadius * 0.3f),
            0
        );

        // Pick a random prefab
        GameObject prefab = passengerPrefabs[Random.Range(0, passengerPrefabs.Length)];

        GameObject passenger =
    Instantiate(prefab);

        passenger.name = "Passenger";

        passenger.transform.position =
            signPosition + offset;

        passenger.transform.rotation =
            Quaternion.identity;

        NetworkObject no =
            passenger.GetComponent<NetworkObject>();

        if (no != null &&
            Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            no.Spawn(true);
        }

        SpriteRenderer sr =
    passenger.GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
        {
            sr.sortingLayerName = "Vehicles";
            sr.sortingOrder = 8;
        }

        waitingPassengers.Add(passenger);

        Debug.Log(
            $"[Station] Passenger ADDED | count now: {waitingPassengers.Count} | object null? {passenger == null}"
        );
        Debug.Log(
        $"[Station] passenger active: {passenger.activeInHierarchy}"
        );
        Debug.Log(
         $"[Station] SPAWN on object: {gameObject.name} | waiting: {waitingPassengers.Count}"
        );
    }
    //[ServerRpc]
    //public void RequestBoardPassengersServerRpc(
    //ulong stationObjectId,
    //ServerRpcParams rpcParams = default)
    //{
    //    if (!NetworkManager.Singleton.SpawnManager
    //        .SpawnedObjects.TryGetValue(
    //            stationObjectId,
    //            out NetworkObject stationObj))
    //    {
    //        Debug.Log("[SERVER] Station object not found");
    //        return;
    //    }

    //    BusStationPassengers passengers =
    //stationObj.GetComponentInChildren<BusStationPassengers>();

    //    if (passengers == null)
    //    {
    //        passengers =
    //            stationObj.GetComponentInParent<BusStationPassengers>();
    //    }

    //    if (passengers == null)
    //    {
    //        Debug.Log("[SERVER] No BusStationPassengers component");
    //        return;
    //    }

    //    int boarded = passengers.BoardPassengers();

    //    passengersOnBoard.Value += boarded;

    //    Debug.Log(
    //        $"[SERVER BUS] Picked up {boarded}, total: {passengersOnBoard.Value}"
    //    );
    //}
    [ServerRpc (RequireOwnership = false)]
    public void RequestBoardPassengersServerRpc(
    ulong busObjectId,
    ServerRpcParams rpcParams = default)
    {
        Debug.Log(
        $"[SERVER] RequestBoardPassengersServerRpc CALLED | busId={busObjectId}"
            );

        if (!NetworkManager.Singleton.SpawnManager
            .SpawnedObjects.TryGetValue(
                busObjectId,
                out NetworkObject busObj))
        {
            Debug.Log("[SERVER] BUS OBJECT NOT FOUND");
            return;
        }
        Debug.Log(
       $"[SERVER] BUS OBJECT FOUND: {busObj.gameObject.name}"
            );
        BusMovement bus =
            busObj.GetComponent<BusMovement>();
        Debug.Log(
        $"[SERVER] BUS COMPONENT NULL? {bus == null}"
        );
        if (bus == null)
            return;

        int boarded = BoardPassengers();
        Debug.Log(
        $"[SERVER] BOARDED COUNT = {boarded}"
            );
        bus.PassengersOnBoard.Value += boarded;

        Debug.Log(
            $"[SERVER BUS] Picked up {boarded}"
        );
    }

    //[ServerRpc]
    //public void RequestDropOffPassengersServerRpc(
    //ulong stationObjectId,
    //ServerRpcParams rpcParams = default)
    //{
    //    if (!NetworkManager.Singleton.SpawnManager
    //        .SpawnedObjects.TryGetValue(
    //            stationObjectId,
    //            out NetworkObject stationObj))
    //    {
    //        Debug.Log("[SERVER] Station object not found");
    //        return;
    //    }

    //    BusStationPassengers passengers =
    //stationObj.GetComponentInChildren<BusStationPassengers>();

    //    if (passengers == null)
    //    {
    //        passengers =
    //            stationObj.GetComponentInParent<BusStationPassengers>();
    //    }

    //    if (passengers == null)
    //    {
    //        Debug.Log("[SERVER] No BusStationPassengers component");
    //        return;
    //    }

    //    int dropped = passengersOnBoard.Value;

    //    passengers.DropOffPassengers(dropped);

    //    if (busModel != null)
    //    {
    //        busModel.IncreasePassangers(dropped);
    //    }

    //    Debug.Log(
    //        $"[SERVER BUS] Dropped off {dropped}"
    //    );

    //    passengersOnBoard.Value = 0;
    //}
    [ServerRpc (RequireOwnership =false)]
    public void RequestDropOffPassengersServerRpc(
    ulong busObjectId,
    ServerRpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager
            .SpawnedObjects.TryGetValue(
                busObjectId,
                out NetworkObject busObj))
        {
            return;
        }

        BusMovement bus =
            busObj.GetComponent<BusMovement>();

        if (bus == null)
            return;

        int dropped = bus.PassengersOnBoard.Value;

        //DropOffPassengers(dropped);
        DropOffPassengers(bus, dropped);

        bus.PassengersOnBoard.Value = 0;

        Debug.Log(
            $"[SERVER BUS] Dropped off {dropped}"
        );
    }
}