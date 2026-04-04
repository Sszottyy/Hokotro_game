using SnowPlow.Model.Map;
using SnowPlow.Model.Map.Generator;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapVisualizer : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject intersectionPrefab;
    public GameObject roadRendererPrefab; // LineRenderer prefab

    [Header("City Settings")]
    public float gridSpacing = 20f;
    public float curveAmplitude = 5f; // Bézier görbület mértéke

    private Dictionary<MapNode, Vector3> _nodePositions = new Dictionary<MapNode, Vector3>();

    public void Visualize(MapData data)
    {
        GenerateGridPositions(data);
        SolveLayout(data);

        // --- Node-ok (Kereszteződések) létrehozása ---
        foreach (var node in data.Nodes)
        {
            Vector3 pos = _nodePositions[node];
            // Minden node kereszteződés típusú
            GameObject instance = Instantiate(intersectionPrefab, pos, Quaternion.identity, transform);
            instance.name = $"Node_{node.Id}";

            // Ha van ilyen scripted, inicializálja az adatokat
            if (instance.TryGetComponent<NodeVisualData>(out var visualData))
            {
                visualData.Initialize(node);
            }

            // Alapértelmezett szín beállítása (2D SpriteRenderer vagy MeshRenderer)
            var renderer = instance.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = Color.darkGreen;
        }

        // --- Utak létrehozása ---
        foreach (var road in data.Roads)
        {
            CreateRoadVisual(road);
        }

        // --- 2D KAMERA AUTOMATIKUS BEÁLLÍTÁSA ---
        AdjustCamera2D(data);
    }

    private void GenerateGridPositions(MapData data)
    {
        _nodePositions.Clear();

        foreach (var node in data.Nodes)
        {
            if (data.GridHints.TryGetValue(node, out var coord))
            {
                // X és Y koordináták használata a 2D síkban, Z = 0
                _nodePositions[node] = new Vector3(
                    coord.x * gridSpacing + Random.Range(-1f, 1f),
                    coord.y * gridSpacing + Random.Range(-1f, 1f),
                    0
                );
            }
            else
            {
                // Fallback véletlenszerű pozíció 2D-ben
                _nodePositions[node] = new Vector3(
                    Random.Range(0f, gridSpacing * 5),
                    Random.Range(0f, gridSpacing * 5),
                    0
                );
            }
        }
    }

    private void SolveLayout(MapData data, int iterations = 100)
    {
        // Egy szegmens hossza világ-egységben (Unity unit)
        // Ezt kedvedre állíthatod, pl. 1 szegmens = 2 egység
        float unitPerSegment = 2f;

        for (int i = 0; i < iterations; i++)
        {
            float temperature = Mathf.Lerp(1f, 0.01f, (float)i / iterations);

            // 1. TASZÍTÁS (Repulsion)
            // Megakadályozza, hogy a csomópontok egymásba másszanak
            for (int j = 0; j < data.Nodes.Count; j++)
            {
                for (int k = j + 1; k < data.Nodes.Count; k++)
                {
                    var aNode = data.Nodes[j];
                    var bNode = data.Nodes[k];

                    Vector3 delta = _nodePositions[bNode] - _nodePositions[aNode];
                    float dist = delta.magnitude + 0.1f;

                    // Ha közelebb vannak, mint a minimális spacing, ellökik egymást
                    if (dist < gridSpacing)
                    {
                        float force = (gridSpacing / dist) * 0.5f * temperature;
                        Vector3 move = delta.normalized * force;
                        _nodePositions[aNode] -= move;
                        _nodePositions[bNode] += move;
                    }
                }
            }

            // 2. VONZÁS (Attraction) - EZ FÜGG MOST MÁR A SEGMENTCOUNT-TÓL
            foreach (var road in data.Roads)
            {
                Vector3 a = _nodePositions[road.NodeA];
                Vector3 b = _nodePositions[road.NodeB];

                Vector3 delta = b - a;
                float currentDist = delta.magnitude + 0.1f;

                // A cél-távolság az út szegmenseinek száma szorozva az egységnyi hosszal
                float targetDist = road.SegmentCount * unitPerSegment;

                // Rugóerő számítása: minél nagyobb az eltérés a cél-távolságtól, annál nagyobb az erő
                float force = (currentDist - targetDist) * 0.08f * temperature;
                Vector3 move = delta.normalized * force;

                _nodePositions[road.NodeA] += move;
                _nodePositions[road.NodeB] -= move;
            }
        }
    }

    private void CreateRoadVisual(Road road)
    {
        Vector3 startPos = _nodePositions[road.NodeA];
        Vector3 endPos = _nodePositions[road.NodeB];

        Vector3 dir = endPos - startPos;
        // 2D merőleges vektor kiszámítása
        Vector3 perp = new Vector3(-dir.y, dir.x, 0).normalized;

        // Bézier kontrollpontok (a road.Id-t használjuk seed-nek a Perlin noise-hoz)
        Vector3 control1 = startPos + dir * 0.33f + perp * (Mathf.PerlinNoise(road.Id * 0.123f, 0) - 0.5f) * curveAmplitude;
        Vector3 control2 = startPos + dir * 0.66f + perp * (Mathf.PerlinNoise(road.Id * 0.123f, 1.5f) - 0.5f) * curveAmplitude;

        GameObject roadObj = Instantiate(roadRendererPrefab, transform);
        roadObj.name = $"Road_{road.Id}";

        LineRenderer line = roadObj.GetComponent<LineRenderer>();

        // Felbontás az út hossza alapján
        int visualSegments = Mathf.Clamp(Mathf.RoundToInt(dir.magnitude / 1.5f), 12, 40);
        line.positionCount = visualSegments + 1;
        line.useWorldSpace = true;

        // Ívhossz-alapú paraméterezés és pontok beállítása
        float[] ts = ArcLengthParameterization(startPos, control1, control2, endPos, visualSegments);
        for (int i = 0; i <= visualSegments; i++)
        {
            line.SetPosition(i, CubicBezier(startPos, control1, control2, endPos, ts[i]));
        }

        // --- SÁVSZÉLESSÉG BEÁLLÍTÁSA ---
        // Itt használjuk a Road osztályod publikus IReadOnlyList tulajdonságait
        int totalLaneCount = road.LanesTowardsA.Count + road.LanesTowardsB.Count;

        // Alapértelmezett sávszélesség-szorzó (pl. 1.2 egység sávonként)
        float laneWidthFactor = 1.2f;
        float finalWidth = Mathf.Max(1.0f, totalLaneCount * laneWidthFactor);

        line.startWidth = finalWidth;
        line.endWidth = finalWidth;

        // Megjelenítési sorrend (utak a node-ok alá)
        line.sortingOrder = -1;

        // Opcionális: Szín beállítása a sávszám alapján (pl. a főutak sötétebbek)
        line.material.color = totalLaneCount > 2 ? new Color(0.1f, 0.1f, 0.1f) : Color.gray;
    }

    private void AdjustCamera2D(MapData data)
    {
        if (data.Nodes.Count == 0) return;

        Bounds bounds = new Bounds(_nodePositions[data.Nodes[0]], Vector3.zero);
        foreach (var pos in _nodePositions.Values)
        {
            bounds.Encapsulate(pos);
        }

        Camera cam = Camera.main;
        cam.orthographic = true;
        cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
        cam.transform.rotation = Quaternion.identity;

        float screenRatio = (float)Screen.width / Screen.height;
        float targetRatio = bounds.size.x / bounds.size.y;
        float padding = 5f;

        if (screenRatio >= targetRatio)
            cam.orthographicSize = (bounds.size.y / 2f) + padding;
        else
            cam.orthographicSize = (bounds.size.x / 2f) / screenRatio + padding;
    }

    // --- Matematikai segédfüggvények ---

    private Vector3 CubicBezier(Vector3 p0, Vector3 c0, Vector3 c1, Vector3 p1, float t)
    {
        float u = 1 - t;
        return u * u * u * p0 + 3 * u * u * t * c0 + 3 * u * t * t * c1 + t * t * t * p1;
    }

    private float[] ArcLengthParameterization(Vector3 p0, Vector3 c0, Vector3 c1, Vector3 p1, int segments)
    {
        int steps = 100;
        float[] distances = new float[steps + 1];
        Vector3 prev = p0;
        float totalLength = 0;

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 current = CubicBezier(p0, c0, c1, p1, t);
            totalLength += Vector3.Distance(prev, current);
            distances[i] = totalLength;
            prev = current;
        }

        float[] ts = new float[segments + 1];
        ts[0] = 0f;
        for (int i = 1; i <= segments; i++)
        {
            float target = totalLength * (i / (float)segments);
            // Egyszerű keresés a távolság-táblázatban
            int low = 0;
            while (low < steps && distances[low] < target) low++;
            ts[i] = (float)low / steps;
        }
        return ts;
    }
}