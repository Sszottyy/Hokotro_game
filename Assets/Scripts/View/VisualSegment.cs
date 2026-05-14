using SnowPlow.Model.Map;
using UnityEngine;

public class VisualSegment : MonoBehaviour
{
    public LaneSegment LogicSegment { get; private set; }
    public LanePosition LanePosition { get; private set; }
    public BusStationPassengers StationPassengers { get; private set; }

    [Header("Vonalak")]
    public GameObject leftOuterLine;
    public GameObject rightOuterLine;
    public GameObject solidDivider;
    public GameObject dashedDivider;

    [Header("Rétegek")]
    public SpriteRenderer snowOverlay;
    public SpriteRenderer iceOverlay;

    [Header("Snow visuals")]
    public Sprite[] snowSprites;

    [Header("Ice visuals")]
    public Sprite iceSprite;

    private bool _isLeftmost;
    private bool _isRightmost;
    [Header("Bus Station")]
    public GameObject busStopPrefab;
    public GameObject[] passengerPrefabs;
    [Header("Bus Station")]
    public SpriteRenderer stationOverlay;

    public void Initialize(
        Lane lane,
        int segmentIndex,
        bool isLeftmost,
        bool isRightmost,
        bool isDirectionDivider)
    {
        _isLeftmost = isLeftmost;
        _isRightmost = isRightmost;

        if (lane == null)
        {
            LogicSegment = null;
            LanePosition = null;
            return;
        }

        LanePosition = new LanePosition(lane, segmentIndex);
        LogicSegment = lane[segmentIndex];

        if (leftOuterLine != null) leftOuterLine.SetActive(isLeftmost);
        if (rightOuterLine != null) rightOuterLine.SetActive(isRightmost);

        if (!isRightmost)
        {
            if (isDirectionDivider && solidDivider != null)
            {
                solidDivider.SetActive(true);
            }
            else if (dashedDivider != null)
            {
                dashedDivider.SetActive(true);
            }
        }
        iceOverlay.sprite = iceSprite;

        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (LogicSegment == null) return;
        if (LogicSegment.HasIce)
        {
            if (iceOverlay != null) iceOverlay.gameObject.SetActive(true);
            if (snowOverlay != null) snowOverlay.color = new Color(1, 1, 1, 0);
        }
        else
        {
            if (iceOverlay != null) iceOverlay.gameObject.SetActive(false);

            if (snowOverlay != null)
            {
                int index = Mathf.Clamp(LogicSegment.SnowLevel, 0, snowSprites.Length - 1);
                if (index == 0)
                {
                    snowOverlay.gameObject.SetActive(false);
                    return;
                }

                snowOverlay.gameObject.SetActive(true);
                snowOverlay.sprite = snowSprites[index];
                snowOverlay.gameObject.SetActive(index > 0);

                float t = LogicSegment.SnowLevel / 3f;

                Color snowColor = Color.Lerp(
                    new Color(0.85f, 0.85f, 0.9f),
                    Color.white,
                    t
                );

                snowOverlay.color = snowColor;

                snowOverlay.transform.localScale = Vector3.one * (1f + t * 0.1f);
            }
        }
    }
    /*private void Update()
    {
        UpdateVisuals();
    }*/

    public void MarkAsStationLine()
    {
        // --- Hatch overlay only ---
        if (stationOverlay == null)
        {
            GameObject overlayObj = new GameObject("StationOverlay");
            overlayObj.transform.SetParent(transform, false);
            overlayObj.transform.localPosition = Vector3.zero;
            overlayObj.transform.localRotation = Quaternion.identity;
            overlayObj.transform.localScale = Vector3.one;

            stationOverlay = overlayObj.AddComponent<SpriteRenderer>();
            stationOverlay.sortingLayerName = "Road";
            stationOverlay.sortingOrder = 2;

            stationOverlay.drawMode = SpriteDrawMode.Sliced;
            stationOverlay.size = new Vector2(1f, 1f);
        }

        Vector3 worldPos = transform.position;

        int offsetX = Mathf.RoundToInt(worldPos.x * 64);
        int offsetY = Mathf.RoundToInt(worldPos.y * 64);

        stationOverlay.sprite = GenerateHatchSprite(64, 64, offsetX, offsetY);
        stationOverlay.gameObject.SetActive(true);
    }

    public void MarkAsStation()
    {
        // Create hatch/line overlay
        MarkAsStationLine();

        // --- Bus stop sign ---
        if (busStopPrefab == null) return;

        // Outward direction: perpendicular to the segment's forward, toward the outside of the road
        Vector3 outwardDir = new Vector3(-transform.up.y, transform.up.x, 0).normalized;
        outwardDir *= _isRightmost ? 1f : -1f;

        // Use the outer line's position as the exact segment edge
        GameObject outerLine = _isRightmost ? rightOuterLine : leftOuterLine;

        Vector3 signOrigin;
        if (outerLine != null)
        {
            signOrigin = outerLine.transform.position;
        }
        else
        {
            // Fallback to collider bounds if outer line is missing
            Collider2D ownCollider = GetComponent<Collider2D>();
            float extent = ownCollider != null ? Mathf.Abs(Vector3.Dot(ownCollider.bounds.extents, outwardDir)) : 0.5f;
            signOrigin = transform.position + outwardDir * extent;
        }

        // Instantiate sign, upright, detached from segment rotation
        GameObject stopSign = Instantiate(busStopPrefab, transform.parent);
        stopSign.name = "BusStopSign";
        stopSign.transform.rotation = Quaternion.identity;
        stopSign.transform.position = signOrigin + outwardDir * 1.3f;

        SpriteRenderer sr = stopSign.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Vehicles";
            sr.sortingOrder = 9;
        }

        // Calculate the exact same position the sign was placed at
        Vector3 signWorldPosition = signOrigin + outwardDir * 1.3f;
        // Add passenger spawner to this segment
        StationPassengers = gameObject.AddComponent<BusStationPassengers>();
        StationPassengers.passengerPrefabs = passengerPrefabs;
        StationPassengers.Initialize(signWorldPosition); // ← use sign position, not outerLine
    }

    private Sprite GenerateHatchSprite(int width, int height, int offsetX, int offsetY)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        Color yellow = new Color(1f, 0.85f, 0f, 0.85f);

        // Fill transparent
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                tex.SetPixel(x, y, transparent);

        int stripeSpacing = 18; // gap between stripes

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Diagonal stripe: /
                int diag = (x + offsetX + y + offsetY) % stripeSpacing;
                if (diag == 0)
                {
                    tex.SetPixel(x, y, yellow);
                }
            }
        }

        tex.wrapMode = TextureWrapMode.Clamp; // add this before tex.Apply()
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            width
        );
    }
}