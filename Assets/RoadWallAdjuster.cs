using UnityEngine;

public class RoadWallAdjuster : MonoBehaviour
{
    public float nodeRadius = 3f;

    void Start()
    {
        // Várunk egy pillanatot, hogy az út biztosan felvegye a torzított méretét
        Invoke("BuildPerfectUnscaledWalls", 0.1f);
    }

    private void BuildPerfectUnscaledWalls()
    {
        float length = transform.localScale.y;
        float width = transform.localScale.x;

        if (length <= 0) return;

        // Kiszámoljuk a látható szakasz hosszát
        float visibleLength = length - (nodeRadius * 2f);
        if (visibleLength <= 0) return; // Összeérnek a körforgalmak

        // --- A VARÁZSLAT: SKÁLÁZATLAN TARTÁLY LÉTREHOZÁSA ---
        GameObject wallsContainer = new GameObject("PhysicalWalls");
        wallsContainer.transform.SetParent(transform);

        // A szülő objektum (út) el van torzítva. Ezt ellensúlyozzuk úgy, 
        // hogy elosztjuk vele, így a világban ez a tartály pontosan 1x1x1 méretű lesz!
        wallsContainer.transform.localScale = new Vector3(1f / width, 1f / length, 1f);
        wallsContainer.transform.localPosition = Vector3.zero;
        wallsContainer.transform.localRotation = Quaternion.identity;

        // Most már pontos világméretekkel (MÉTEREKKEL) dolgozhatunk, pont mint a körforgalomnál!
        float halfVisible = visibleLength / 2f;

        // Az út széle (0.05-tel beljebb húzva)
        float exactEdgeX = (width / 2f) - 0.05f;

        // --- BAL OLDALI TÖKÉLETES VONAL ---
        EdgeCollider2D leftWall = wallsContainer.AddComponent<EdgeCollider2D>();
        leftWall.points = new Vector2[] {
            new Vector2(-exactEdgeX, -halfVisible),
            new Vector2(-exactEdgeX, halfVisible)
        };

        // --- JOBB OLDALI TÖKÉLETES VONAL ---
        EdgeCollider2D rightWall = wallsContainer.AddComponent<EdgeCollider2D>();
        rightWall.points = new Vector2[] {
            new Vector2(exactEdgeX, -halfVisible),
            new Vector2(exactEdgeX, halfVisible)
        };
    }
}