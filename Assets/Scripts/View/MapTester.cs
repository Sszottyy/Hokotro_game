using SnowPlow.Controller.Spawning;
using SnowPlow.Model.Map.Generator;
using UnityEngine;

public class MapTester : MonoBehaviour
{
    public MapVisualizer visualizer;
    public VehicleSpawner vehicleSpawner;

    [Header("Generálási Paraméterek")]
    public int intersections = 10;

    [SerializeField] private int _seed;

    void Start()
    {
        RunTest();
    }

    public void RunTest()
    {
        // 1. Meglévő vizualizáció törlése (ha van)
        for (int i = visualizer.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(visualizer.transform.GetChild(i).gameObject);
        }

        // 2. Új logikai modell generálása
        MapGenerator generator = new MapGenerator(_seed);
        var data = generator.Generate(intersections);

        // 3. Megjelenítés
        visualizer.Visualize(data);

        // 4. Járművek spawnolása
        vehicleSpawner.Initialize(data, visualizer);

        Debug.Log($"Teszt lefutott: {data.Nodes.Count} csomópont, {data.Roads.Count} út jött létre.");
    }
}