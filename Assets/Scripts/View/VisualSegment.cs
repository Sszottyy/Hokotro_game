using SnowPlow.Model.Map;
using Unity.VisualScripting;
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

    [Header("Snow visuals")]
    public Sprite[] snowSprites;

    [Header("Ice visuals")]
    public Sprite iceSprite;

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
                //float alpha = Mathf.Clamp01(LogicSegment.SnowLevel / 3f);
                //snowOverlay.color = new Color(1, 1, 1, alpha);
            }
        }
    }
    private void Update()
    {
        UpdateVisuals();
    }
}