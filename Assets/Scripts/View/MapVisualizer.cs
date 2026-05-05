using System.Collections.Generic;
using Assets.Scripts.Controller;
using SnowPlow.Model.Map;
using SnowPlow.Model.Map.Generator;
using UnityEngine;

public class MapVisualizer : MonoBehaviour
{
    [Header("Prefabs (Views)")]
    public GameObject intersectionPrefab;
    public GameObject segmentPrefab;

    [Header("City Settings")]
    public float gridSpacing = 20f;
    public float laneWidth = 1.0f;

    [SerializeField] private SnowController snowController;

    // A Járművek ezen keresztül tudják majd, hova kell menniük!
    public Dictionary<LaneSegment, VisualSegment> SegmentDirectory = new Dictionary<LaneSegment, VisualSegment>();

    private Dictionary<MapNode, Vector3> _nodePositions = new Dictionary<MapNode, Vector3>();
    private Dictionary<MapNode, NodeVisualData> _visualNodes = new Dictionary<MapNode, NodeVisualData>();

    public void Visualize(MapData data)
    {
        SegmentDirectory.Clear();
        _visualNodes.Clear();

        GenerateGridPositions(data);

        // VISSZAKAPCSOLVA: A szépítő gumi-fizika elrendezés!
        SolveLayout(data);

        foreach (var node in data.Nodes)
        {
            Vector3 pos = _nodePositions[node];
            GameObject instance = Instantiate(intersectionPrefab, pos, Quaternion.identity, transform);

            if (instance.TryGetComponent<NodeVisualData>(out var visualData))
            {
                visualData.Initialize(node);
                _visualNodes[node] = visualData;
            }
        }

        foreach (var road in data.Roads)
        {
            BuildRoadVisuals(road);
        }

        foreach (var vNode in _visualNodes.Values)
        {
            vNode.BuildIntersectionWalls(_nodePositions, laneWidth);
        }
        
        InitSnowSystem(data);

        // AdjustCamera2D(data);
    }

    private void GenerateGridPositions(MapData data)
    {
        _nodePositions.Clear();
        foreach (var node in data.Nodes)
        {
            if (data.GridHints.TryGetValue(node, out var coord))
            {
                _nodePositions[node] = new Vector3(coord.x * gridSpacing, coord.y * gridSpacing, 0);
            }
        }
    }

    private void SolveLayout(MapData data, int iterations = 100)
    {
        // 1.0f, mivel a generátorban 20-ra emeltük a SegmentsPerGridUnit számot
        float unitPerSegment = 1.0f;

        for (int i = 0; i < iterations; i++)
        {
            float temperature = Mathf.Lerp(1f, 0.01f, (float)i / iterations);

            for (int j = 0; j < data.Nodes.Count; j++)
            {
                for (int k = j + 1; k < data.Nodes.Count; k++)
                {
                    var aNode = data.Nodes[j];
                    var bNode = data.Nodes[k];

                    Vector3 delta = _nodePositions[bNode] - _nodePositions[aNode];
                    float dist = delta.magnitude + 0.1f;

                    if (dist < gridSpacing)
                    {
                        float force = (gridSpacing / dist) * 0.5f * temperature;
                        Vector3 move = delta.normalized * force;
                        _nodePositions[aNode] -= move;
                        _nodePositions[bNode] += move;
                    }
                }
            }

            foreach (var road in data.Roads)
            {
                Vector3 a = _nodePositions[road.NodeA];
                Vector3 b = _nodePositions[road.NodeB];

                Vector3 delta = b - a;
                float currentDist = delta.magnitude + 0.1f;

                float targetDist = road.SegmentCount * unitPerSegment;

                float force = (currentDist - targetDist) * 0.08f * temperature;
                Vector3 move = delta.normalized * force;

                _nodePositions[road.NodeA] += move;
                _nodePositions[road.NodeB] -= move;
            }
        }
    }

    private void BuildRoadVisuals(Road road)
    {
        Vector3 posA = _nodePositions[road.NodeA];
        Vector3 posB = _nodePositions[road.NodeB];

        Vector3 roadDirection = (posB - posA).normalized;
        Vector3 roadRight = new Vector3(-roadDirection.y, roadDirection.x, 0);

        float actualDist = Vector3.Distance(posA, posB);
        float stepSize = actualDist / road.SegmentCount;

        int lanesA = road.LanesTowardsA.Count;
        int lanesB = road.LanesTowardsB.Count;
        int totalLanes = lanesA + lanesB;

        float roadWidthOffset = (totalLanes - 1) * laneWidth * 0.5f;

        List<Lane> allLanes = new List<Lane>();
        allLanes.AddRange(road.LanesTowardsA);
        allLanes.AddRange(road.LanesTowardsB);

        for (int l = 0; l < totalLanes; l++)
        {
            Lane logicLane = allLanes[l];

            Vector3 laneStartPosition = _nodePositions[logicLane.StartNode];
            Vector3 laneEndPosition = _nodePositions[logicLane.EndNode];
            Vector3 laneDirection = (laneEndPosition - laneStartPosition).normalized;

            Vector3 laneOffset = roadRight * (l * laneWidth - roadWidthOffset);

            for (int s = 0; s < road.SegmentCount; s++)
            {
                Vector3 spawnPos =
                    laneStartPosition
                    + laneDirection * (s * stepSize + stepSize / 2f)
                    + laneOffset;

                GameObject segmentObj = Instantiate(segmentPrefab, spawnPos, Quaternion.identity, transform);

                segmentObj.transform.up = laneDirection;
                segmentObj.transform.localScale = new Vector3(laneWidth, stepSize, 1);
                segmentObj.name = $"Segment_{road.Id}_Lane{logicLane.Id}_S{s}";

                VisualSegment visSeg = segmentObj.GetComponent<VisualSegment>();

                visSeg.Initialize(
                    logicLane,
                    s,
                    true,
                    true,
                    true
                );

                LaneSegment logicSegment = logicLane.Segments[s];

                if (!SegmentDirectory.ContainsKey(logicSegment))
                {
                    SegmentDirectory.Add(logicSegment, visSeg);
                }
            }
        }
    }

    private void AdjustCamera2D(MapData data) //ez lehet fölösleges, de ha az egész mapot látni akarjuk akkor jól jöhet
    {
        if (data.Nodes.Count == 0) return;

        Bounds bounds = new Bounds(_nodePositions[data.Nodes[0]], Vector3.zero);
        foreach (var pos in _nodePositions.Values)
        {
            bounds.Encapsulate(pos);
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
            cam.transform.rotation = Quaternion.identity;

            float screenRatio = (float)Screen.width / Screen.height;
            float targetRatio = bounds.size.x / bounds.size.y;
            float padding = 5f;

            if (screenRatio >= targetRatio) cam.orthographicSize = (bounds.size.y / 2f) + padding;
            else cam.orthographicSize = (bounds.size.x / 2f) / screenRatio + padding;
        }
    }

    private void InitSnowSystem(MapData data)
    {
        List<Lane> lanes = new List<Lane>();

        foreach (var road in data.Roads)
        {
            lanes.AddRange(road.LanesTowardsA);
            lanes.AddRange(road.LanesTowardsB);
        }

        snowController.Init(lanes);
    }

}