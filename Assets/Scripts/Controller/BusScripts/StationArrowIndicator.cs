using UnityEngine;

public class StationArrowIndicator : MonoBehaviour
{
    [Header("Arrow Sprites (8-directional, clockwise from Up)")]
    public Sprite arrow_Up;
    public Sprite arrow_UpRight;
    public Sprite arrow_Right;
    public Sprite arrow_DownRight;
    public Sprite arrow_Down;
    public Sprite arrow_DownLeft;
    public Sprite arrow_Left;
    public Sprite arrow_UpLeft;

    [Header("Settings")]
    public float arrowOffset = 1.5f;
    public float arrowScale = 35f;
    public string arrowSortingLayer = "Vehicles";
    public int arrowSortingOrder = 20;

    private SpriteRenderer arrowA;
    private SpriteRenderer arrowB;
    private Transform stationATransform;
    private Transform stationBTransform;

    public void SetStations(VisualSegment a, VisualSegment b)
    {
        Debug.Log($"[Arrows] SetStations called from: {new System.Diagnostics.StackTrace()}");
        stationATransform = a.transform;
        stationBTransform = b.transform;

        // Destroy ALL arrow children by name — reliable regardless of reference state
        foreach (Transform child in transform)
        {
            if (child.name == "ArrowToStationA" || child.name == "ArrowToStationB")
                Destroy(child.gameObject);
        }

        arrowA = null;
        arrowB = null;

        arrowA = CreateArrow("ArrowToStationA", Color.yellow);
        arrowB = CreateArrow("ArrowToStationB", Color.cyan);
    }

    void Update()
    {
        if (arrowA == null || arrowB == null) return;

        if (stationATransform != null)
            UpdateArrow(arrowA, stationATransform.position);

        if (stationBTransform != null)
            UpdateArrow(arrowB, stationBTransform.position);
    }

    private void UpdateArrow(SpriteRenderer arrow, Vector3 targetPos)
    {
        Vector2 dir = (targetPos - transform.position).normalized;

        // Set world position manually so parent rotation doesn't affect placement
        arrow.transform.position = transform.position + (Vector3)(dir * arrowOffset);

        // Keep upright regardless of bus rotation
        arrow.transform.rotation = Quaternion.identity;

        arrow.sprite = GetDirectionalSprite(dir);
    }

    private Sprite GetDirectionalSprite(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        float remapped = (450f - angle) % 360f;

        if (remapped >= 337.5f || remapped < 22.5f) return arrow_Up;
        else if (remapped >= 22.5f && remapped < 67.5f) return arrow_UpRight;
        else if (remapped >= 67.5f && remapped < 112.5f) return arrow_Right;
        else if (remapped >= 112.5f && remapped < 157.5f) return arrow_DownRight;
        else if (remapped >= 157.5f && remapped < 202.5f) return arrow_Down;
        else if (remapped >= 202.5f && remapped < 247.5f) return arrow_DownLeft;
        else if (remapped >= 247.5f && remapped < 292.5f) return arrow_Left;
        else return arrow_UpLeft;
    }

    private SpriteRenderer CreateArrow(string arrowName, Color tint)
    {
        GameObject obj = new GameObject(arrowName);

        // Parent to the bus — destroyed automatically with bus, no tracking needed
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = Vector3.one * arrowScale;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = arrowSortingLayer;
        sr.sortingOrder = arrowSortingOrder;
        sr.color = tint;
        return sr;
    }

    void Awake()
    {
        // Clean up any arrows baked into the prefab
        foreach (Transform child in transform)
        {
            if (child.name == "ArrowToStationA" || child.name == "ArrowToStationB")
                Destroy(child.gameObject);
        }
        arrowA = null;
        arrowB = null;
    }
}