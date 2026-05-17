using UnityEngine;
using System.Collections.Generic;
using SnowPlow.Model.Map;
using SnowPlow.Model.Vehicles;

public class HouseSpawner : MonoBehaviour
{
    // A shared global list that all instances of HouseSpawner can see and update
    private static readonly List<Vector3> SpawnedHousePositions = new List<Vector3>();

    /// <summary>
    /// Clears the global registry. Call this when generating or reloading a map.
    /// </summary>
    public static void ClearRegistry()
    {
        SpawnedHousePositions.Clear();
    }

    [Header("Distance Settings")]
    [Tooltip("Increasing this moves houses further away from roads and roundabouts.")]
    [SerializeField] private float houseClearanceBuffer = 3.5f;

    [Tooltip("The minimum allowed distance between two different houses to prevent crowding.")]
    [SerializeField] private float minDistanceBetweenHouses = 5f;

    private Car _car;
    private MapVisualizer _mapVisualizer;
    private GameObject _homePrefab;
    private GameObject _workPrefab;
    private float _zOffset;

    private GameObject _homeInstance;
    private GameObject _workInstance;

    public void Initialize(Car car, MapVisualizer mapVisualizer, GameObject homePrefab, GameObject workPrefab, float zOffset)
    {
        _car = car;
        _mapVisualizer = mapVisualizer;
        _homePrefab = homePrefab;
        _workPrefab = workPrefab;
        _zOffset = zOffset;
    }

    private void Start()
    {
        TrySpawnHouses();
    }

    private void Update()
    {
        if (_homeInstance == null || _workInstance == null)
        {
            TrySpawnHouses();
        }
    }

    private void TrySpawnHouses()
    {
        if (_car == null || _mapVisualizer == null) return;

        if (_homeInstance == null && _car.Home != null)
        {
            _homeInstance = SpawnHouseWithFallback(_car.Home, _homePrefab, "Home_House");
        }

        if (_workInstance == null && _car.Work != null)
        {
            _workInstance = SpawnHouseWithFallback(_car.Work, _workPrefab, "Work_House");
        }

        if (_homeInstance != null && _workInstance != null)
        {
            enabled = false;
        }
    }

    private GameObject SpawnHouseWithFallback(LanePosition lanePosition, GameObject prefab, string name)
    {
        if (prefab == null || lanePosition?.Lane == null) return null;

        if (lanePosition.SegmentIndex >= lanePosition.Lane.Segments.Count) return null;
        LaneSegment targetLogicSegment = lanePosition.Lane.Segments[lanePosition.SegmentIndex];

        if (!_mapVisualizer.SegmentDirectory.TryGetValue(targetLogicSegment, out VisualSegment visSeg)) return null;

        Vector3 laneForward = visSeg.transform.up;
        Vector3 standardOutwardDir = new Vector3(-laneForward.y, laneForward.x, 0).normalized;

        if (visSeg.IsLeftmost && !visSeg.IsRightmost)
        {
            standardOutwardDir = -standardOutwardDir;
        }

        float halfRoadWidth = visSeg.transform.localScale.x * 0.5f;
        float segmentLength = visSeg.transform.localScale.y;

        Vector3[] positionCandidates = new Vector3[]
        {
            // Option 1: Standard outward position
            visSeg.transform.position + standardOutwardDir * (halfRoadWidth + houseClearanceBuffer),

            // Option 2: Flip to opposite side
            visSeg.transform.position - standardOutwardDir * (halfRoadWidth + houseClearanceBuffer),

            // Option 3: Shift backward along the road and outward
            visSeg.transform.position - (laneForward * segmentLength * 0.6f) + standardOutwardDir * (halfRoadWidth + houseClearanceBuffer),

            // Option 4: Shift backward and flip sides
            visSeg.transform.position - (laneForward * segmentLength * 0.6f) - standardOutwardDir * (halfRoadWidth + houseClearanceBuffer)
        };

        Vector3 finalSpawnPos = Vector3.zero;
        bool foundValidSpot = false;

        foreach (Vector3 candidate in positionCandidates)
        {
            Vector3 adjustedCandidate = PushOutFromRoundabout(lanePosition.Lane.StartNode, candidate, houseClearanceBuffer);
            adjustedCandidate = PushOutFromRoundabout(lanePosition.Lane.EndNode, adjustedCandidate, houseClearanceBuffer);

            if (IsPositionSafe(adjustedCandidate, targetLogicSegment, houseClearanceBuffer))
            {
                finalSpawnPos = adjustedCandidate;
                foundValidSpot = true;
                break;
            }
        }

        if (!foundValidSpot)
        {
            finalSpawnPos = PushOutFromRoundabout(lanePosition.Lane.StartNode, positionCandidates[0], houseClearanceBuffer);
            finalSpawnPos = PushOutFromRoundabout(lanePosition.Lane.EndNode, finalSpawnPos, houseClearanceBuffer);
        }

        finalSpawnPos.z = _zOffset;

        // Register the position globally so future houses don't spawn right on top of it
        SpawnedHousePositions.Add(finalSpawnPos);

        GameObject house = Instantiate(prefab, finalSpawnPos, Quaternion.identity, _mapVisualizer.transform);
        house.name = $"{name}_Lane{lanePosition.Lane.Id}_Seg{lanePosition.SegmentIndex}";

        return house;
    }

    private bool IsPositionSafe(Vector3 targetPos, LaneSegment currentSeg, float buffer)
    {
        // NEW: 1. Validate against previously spawned houses
        foreach (Vector3 existingHousePos in SpawnedHousePositions)
        {
            // Ignore Z axis to accurately evaluate 2D proximity layout
            float distanceToHouse = Vector2.Distance(new Vector2(targetPos.x, targetPos.y), new Vector2(existingHousePos.x, existingHousePos.y));
            if (distanceToHouse < minDistanceBetweenHouses)
            {
                return false; // Neighbor is too close!
            }
        }

        // 2. Validate against Roundabouts
        NodeVisualData[] allNodes = Object.FindObjectsByType<NodeVisualData>(FindObjectsSortMode.None);
        foreach (var nodeVisual in allNodes)
        {
            float dist = Vector3.Distance(targetPos, nodeVisual.transform.position);
            if (dist < (nodeVisual.radius + buffer * 0.8f))
            {
                return false;
            }
        }

        // 3. Validate against other road segments
        foreach (var kvp in _mapVisualizer.SegmentDirectory)
        {
            if (kvp.Key == currentSeg) continue;

            VisualSegment otherVisSeg = kvp.Value;
            if (otherVisSeg == null) continue;

            Vector3 localPos = otherVisSeg.transform.InverseTransformPoint(targetPos);

            float safeHalfWidth = (otherVisSeg.transform.localScale.x * 0.5f) + (buffer * 0.5f);
            float safeHalfHeight = (otherVisSeg.transform.localScale.y * 0.5f) + (buffer * 0.5f);

            if (Mathf.Abs(localPos.x) < safeHalfWidth && Mathf.Abs(localPos.y) < safeHalfHeight)
            {
                return false;
            }
        }

        return true;
    }

    private Vector3 PushOutFromRoundabout(MapNode node, Vector3 currentSpawnPos, float buffer)
    {
        if (node == null || _mapVisualizer == null) return currentSpawnPos;

        Transform nodeTransform = _mapVisualizer.transform.Find($"Node_{node.Id}");
        if (nodeTransform == null) return currentSpawnPos;

        Vector3 roundaboutCenter = nodeTransform.position;
        float roundaboutRadius = 3f;

        if (nodeTransform.TryGetComponent<NodeVisualData>(out var visualData))
        {
            roundaboutRadius = visualData.radius;
        }

        float distanceToCenter = Vector3.Distance(currentSpawnPos, roundaboutCenter);
        float safeDistanceThreshold = roundaboutRadius + buffer;

        if (distanceToCenter < safeDistanceThreshold)
        {
            Vector3 pushDirection = (currentSpawnPos - roundaboutCenter).normalized;
            if (pushDirection == Vector3.zero) pushDirection = Vector3.up;

            return roundaboutCenter + pushDirection * safeDistanceThreshold;
        }

        return currentSpawnPos;
    }
}