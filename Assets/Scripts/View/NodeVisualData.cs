using SnowPlow.Model.Map;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NodeVisualData : MonoBehaviour
{
    public MapNode LogicalNode { get; private set; }
    public float radius = 3f;

    public void Initialize(MapNode node)
    {
        LogicalNode = node;
        gameObject.name = $"Node_{node.Id}";

        var circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider != null)
        {
            circleCollider.radius = radius / transform.localScale.x;
            circleCollider.isTrigger = true;
        }
    }

    public void BuildIntersectionWalls(Dictionary<MapNode, Vector3> nodePositions, float laneWidth)
    {
        List<RoadData> roadAngles = new List<RoadData>();

        foreach (var road in LogicalNode.ConnectedRoads)
        {
            MapNode otherNode = (road.NodeA == LogicalNode) ? road.NodeB : road.NodeA;
            Vector3 otherPos = nodePositions[otherNode];

            Vector3 dir = (otherPos - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            int totalLanes = road.LanesTowardsA.Count + road.LanesTowardsB.Count;
            // Itt csak azt számoljuk ki, hol fut az egyenes vonal a sávban
            float exactHalfWidth = (totalLanes * 0.5f * laneWidth) - 0.05f;

            roadAngles.Add(new RoadData { angle = angle, halfWidth = exactHalfWidth });
        }

        roadAngles = roadAngles.OrderBy(r => r.angle).ToList();

        for (int i = 0; i < roadAngles.Count; i++)
        {
            RoadData currentRoad = roadAngles[i];
            RoadData nextRoad = roadAngles[(i + 1) % roadAngles.Count];

            // A trigonometriát szigorúan a 3.0-ás peremre számoljuk!
            float safeRadius = Mathf.Max(radius, currentRoad.halfWidth + 0.001f);
            float currentHalfAngle = Mathf.Asin(currentRoad.halfWidth / safeRadius) * Mathf.Rad2Deg;

            float safeRadiusNext = Mathf.Max(radius, nextRoad.halfWidth + 0.001f);
            float nextHalfAngle = Mathf.Asin(nextRoad.halfWidth / safeRadiusNext) * Mathf.Rad2Deg;

            // Egy hajszálnyi (0.5 fokos) ráhagyás, ami a kerekített vonalvégekkel 
            // tökéletesen összeforrasztja a találkozási pontot, de nem csinál tüskét!
            float overlap = 0.5f;
            float startArcAngle = currentRoad.angle + currentHalfAngle - overlap;
            float endArcAngle = nextRoad.angle - nextHalfAngle + overlap;

            if (i == roadAngles.Count - 1) endArcAngle += 360f;

            CreateArcWall(startArcAngle, endArcAngle);
        }

        CreateInnerCircle();
    }

    private void CreateArcWall(float startAngle, float endAngle)
    {
        if (endAngle - startAngle < 0.5f) return;

        GameObject arcObj = new GameObject("ArcWall");
        arcObj.transform.SetParent(transform);

        arcObj.transform.localScale = new Vector3(
            1f / transform.localScale.x,
            1f / transform.localScale.y,
            1f
        );

        arcObj.transform.localPosition = new Vector3(0, 0, -0.1f);

        List<Vector2> colliderPoints = new List<Vector2>();
        List<Vector3> linePoints = new List<Vector3>();

        int segments = Mathf.CeilToInt((endAngle - startAngle) / 2f);
        float angleStep = (endAngle - startAngle) / segments;

        // VÉGRE: Semmilyen kivonás! Hajszálpontosan a szürke kör peremén fut.
        float visualRadius = radius;
        float colliderRadius = radius;

        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = (startAngle + i * angleStep) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(currentAngle);
            float sin = Mathf.Sin(currentAngle);

            colliderPoints.Add(new Vector2(cos * colliderRadius, sin * colliderRadius));
            linePoints.Add(new Vector3(cos * visualRadius, sin * visualRadius, 0));
        }

        //körforgalom fala, egyelőre hagyjuk
        //EdgeCollider2D edge = arcObj.AddComponent<EdgeCollider2D>();
        //edge.points = colliderPoints.ToArray();

        LineRenderer line = arcObj.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = linePoints.Count;
        line.SetPositions(linePoints.ToArray());

        line.startWidth = 0.1f;
        line.endWidth = 0.1f;
        line.numCapVertices = 8;
        line.numCornerVertices = 8;

        int sortOrder = 4; // Alapértelmezett, ha valamiért nem találná a szülőt
        SpriteRenderer parentSprite = GetComponent<SpriteRenderer>();
        if (parentSprite != null)
        {
            line.material = parentSprite.material;
            line.sortingLayerID = parentSprite.sortingLayerID;
            // Mindig a szürke aszfaltkör FÖLÉ tesszük eggyel!
            sortOrder = parentSprite.sortingOrder + 2; //a hóréteg miatt +2
        }

        line.startColor = Color.white;
        line.endColor = Color.white;
        line.sortingOrder = sortOrder;
    }

    private void CreateInnerCircle()
    {
        GameObject innerObj = new GameObject("InnerCircle");
        innerObj.transform.SetParent(transform);

        innerObj.transform.localScale = new Vector3(
            1f / transform.localScale.x,
            1f / transform.localScale.y,
            1f
        );

        innerObj.transform.localPosition = new Vector3(0, 0, -0.1f);

        int segments = 60;
        float angleStep = 360f / segments;
        float innerRadius = radius * 0.35f;

        List<Vector3> linePoints = new List<Vector3>();
        for (int i = 0; i < segments; i++)
        {
            float currentAngle = i * angleStep * Mathf.Deg2Rad;
            linePoints.Add(new Vector3(Mathf.Cos(currentAngle) * innerRadius, Mathf.Sin(currentAngle) * innerRadius, 0));
        }

        LineRenderer line = innerObj.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = linePoints.Count;
        line.SetPositions(linePoints.ToArray());

        line.loop = true;

        line.startWidth = 0.1f;
        line.endWidth = 0.1f;

        int sortOrder = 4;
        SpriteRenderer parentSprite = GetComponent<SpriteRenderer>();
        if (parentSprite != null)
        {
            line.material = parentSprite.material;
            line.sortingLayerID = parentSprite.sortingLayerID;
            sortOrder = parentSprite.sortingOrder + 2; //a hóréteg miatt nem +1 hanem +2
        }

        line.startColor = Color.white;
        line.endColor = Color.white;
        line.sortingOrder = sortOrder;
    }

    private struct RoadData { public float angle; public float halfWidth; }
}