using SnowPlow.Model.Map;
using UnityEngine;

public class VisualSegment : MonoBehaviour
{
    public LaneSegment LogicSegment { get; private set; }
    public LanePosition LanePosition { get; private set; }

    [Header("Vonalak")]
    public GameObject leftOuterLine;
    public GameObject rightOuterLine;
    public GameObject solidDivider;
    public GameObject dashedDivider;

    [Header("Rétegek")]
    public SpriteRenderer snowOverlay;
    public SpriteRenderer iceOverlay;

    public void Initialize(
        Lane lane,
        int segmentIndex,
        bool isLeftmost,
        bool isRightmost,
        bool isDirectionDivider)
    {
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
                snowOverlay.gameObject.SetActive(true);
                Debug.Log($"SnowLevel: {LogicSegment.SnowLevel}");
                float alpha = Mathf.Clamp01(LogicSegment.SnowLevel / 3f);
                snowOverlay.color = new Color(1, 1, 1, alpha);
            }
        }
    }
    private void Update()
    {
        UpdateVisuals();
    }
}