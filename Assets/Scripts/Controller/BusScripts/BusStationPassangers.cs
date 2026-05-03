using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BusStationPassengers : MonoBehaviour
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
        signPosition = busSignPosition;
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
        int count = waitingPassengers.Count;

        foreach (var p in waitingPassengers)
            if (p != null) Destroy(p);

        waitingPassengers.Clear();
        Debug.Log($"[Station] {count} passengers boarded.");
        return count;
    }

    // Called when bus drops off passengers at the other station
    public void DropOffPassengers(int count)
    {
        Debug.Log($"[Station] {count} passengers dropped off.");
        // Could spawn "arrived" passengers here if needed later
    }

    private IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            float interval = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(interval);

            if (waitingPassengers.Count < maxPassengers && passengerPrefabs != null)
                SpawnPassenger();
        }
    }

    private void SpawnPassenger()
    {
        if (passengerPrefabs == null || passengerPrefabs.Length == 0) return;

        Vector3 offset = new Vector3(
            Random.Range(-spawnRadius, spawnRadius),
            Random.Range(-spawnRadius * 0.3f, spawnRadius * 0.3f),
            0
        );

        // Pick a random prefab
        GameObject prefab = passengerPrefabs[Random.Range(0, passengerPrefabs.Length)];

        GameObject passenger = Instantiate(prefab, transform.parent, false);
        passenger.name = "Passenger";
        passenger.transform.position = signPosition + offset;
        passenger.transform.rotation = Quaternion.identity;

        SpriteRenderer sr = passenger.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Vehicles";
            sr.sortingOrder = 8;
        }

        waitingPassengers.Add(passenger);
    }
}