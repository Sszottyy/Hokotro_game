using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SnowPlow.Model.Map;

public class MapDecorator : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Drop your Tree and Snowman prefabs here.")]
    [SerializeField] private GameObject[] decorationPrefabs;

    [Header("Spawn Settings")]
    [Tooltip("Total number of decorations to attempt to scatter across the map.")]
    [SerializeField] private int totalDecorations = 200;

    [Tooltip("Clearance distance from roads, roundabouts, and houses.")]
    [SerializeField] private float clearanceFromElements = 2.2f;

    [Tooltip("Minimum distance allowed between two decorations so trees don't clip into each other.")]
    [SerializeField] private float minDistanceBetweenDecorations = 1.5f;

    [SerializeField] private float zOffset = -0.1f;

    private MapVisualizer _mapVisualizer;
    private readonly List<Vector3> _spawnedDecorationPositions = new List<Vector3>();
    private readonly List<Vector3> _cachedHousePositions = new List<Vector3>();

    private void Start()
    {
        // Find the map visualizer automatically in the scene
        _mapVisualizer = Object.FindAnyObjectByType<MapVisualizer>();

        if (_mapVisualizer != null)
        {
            StartCoroutine(SpawnDecorationsDelayed());
        }
        else
        {
            Debug.LogError("MapDecorator: Could not find MapVisualizer in the scene!");
        }
    }

    private IEnumerator SpawnDecorationsDelayed()
    {
        // Wait until the end of the frame to make sure all houses have completed spawning
        yield return new WaitForEndOfFrame();

        if (decorationPrefabs == null || decorationPrefabs.Length == 0) yield break;

        _spawnedDecorationPositions.Clear();
        _cachedHousePositions.Clear();

        // 1. Cache all house positions once upfront to keep validation fast
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("House"))
            {
                _cachedHousePositions.Add(obj.transform.position);
            }
        }

        // 2. Calculate the total rectangular bounding area of your map nodes
        NodeVisualData[] allNodes = Object.FindObjectsByType<NodeVisualData>(FindObjectsSortMode.None);
        if (allNodes.Length == 0) yield break;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (NodeVisualData node in allNodes)
        {
            Vector3 pos = node.transform.position;
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        // Add extra padding boundary past the outermost roundabouts
        minX -= 20f; maxX += 20f;
        minY -= 20f; maxY += 20f;

        // 3. Scatter Loop
        int attempts = 0;
        int spawnedCount = 0;
        int maxAttempts = totalDecorations * 15; // Hard cap limit to prevent infinite loops

        while (spawnedCount < totalDecorations && attempts < maxAttempts)
        {
            attempts++;

            float randomX = Random.Range(minX, maxX);
            float randomY = Random.Range(minY, maxY);
            Vector3 candidatePos = new Vector3(randomX, randomY, zOffset);

            if (IsPositionSafe(candidatePos, allNodes))
            {
                GameObject randomPrefab = decorationPrefabs[Random.Range(0, decorationPrefabs.Length)];

                GameObject decoInstance = Instantiate(randomPrefab, candidatePos, Quaternion.identity, transform);
                decoInstance.name = $"Decoration_{randomPrefab.name}_{spawnedCount}";

                _spawnedDecorationPositions.Add(candidatePos);
                spawnedCount++;
            }
        }
    }

    private bool IsPositionSafe(Vector3 pos, NodeVisualData[] allNodes)
    {
        // 1. Check proximity to Roundabouts (Nodes)
        foreach (NodeVisualData nodeVisual in allNodes)
        {
            float dist = Vector3.Distance(pos, nodeVisual.transform.position);
            if (dist < (nodeVisual.radius + clearanceFromElements))
            {
                return false;
            }
        }

        // 2. Check alignment against Road Segments (Using local-space bounding box)
        foreach (KeyValuePair<LaneSegment, VisualSegment> kvp in _mapVisualizer.SegmentDirectory)
        {
            VisualSegment otherVisSeg = kvp.Value;
            if (otherVisSeg == null) continue;

            Vector3 localPos = otherVisSeg.transform.InverseTransformPoint(pos);

            float safeHalfWidth = (otherVisSeg.transform.localScale.x * 0.5f) + clearanceFromElements;
            float safeHalfHeight = (otherVisSeg.transform.localScale.y * 0.5f) + clearanceFromElements;

            if (Mathf.Abs(localPos.x) < safeHalfWidth && Mathf.Abs(localPos.y) < safeHalfHeight)
            {
                return false;
            }
        }

        // 3. Check proximity to already placed Houses
        foreach (Vector3 housePos in _cachedHousePositions)
        {
            float dist = Vector2.Distance(new Vector2(pos.x, pos.y), new Vector2(housePos.x, housePos.y));
            if (dist < clearanceFromElements)
            {
                return false;
            }
        }

        // 4. Check proximity to already placed Decorations (Prevents tree overlapping clusters)
        foreach (Vector3 decoPos in _spawnedDecorationPositions)
        {
            float dist = Vector2.Distance(new Vector2(pos.x, pos.y), new Vector2(decoPos.x, decoPos.y));
            if (dist < minDistanceBetweenDecorations)
            {
                return false;
            }
        }

        return true;
    }
}