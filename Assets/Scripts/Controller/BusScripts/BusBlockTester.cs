using System.Collections.Generic;
using SnowPlow.Model.Map;
using UnityEngine;

public class BusBlockTester : MonoBehaviour
{
    [SerializeField] private MapVisualizer mapVisualizer;

    [Header("Test Settings")]
    [SerializeField] private int snowySegmentCount = 3;
    [SerializeField] private int icySegmentCount = 3;
    [SerializeField] private int accidentSegmentCount = 3;

    void Start()
    {
        Invoke(nameof(SetupTestSegments), 0.5f);
    }

    private void SetupTestSegments()
    {
        List<LaneSegment> allSegments = new List<LaneSegment>(mapVisualizer.SegmentDirectory.Keys);

        if (allSegments.Count == 0)
        {
            Debug.LogWarning("[Tester] No segments found!");
            return;
        }

        int index = 0;

        // Snowy segments
        for (int i = 0; i < snowySegmentCount && index < allSegments.Count; i++, index++)
        {
            LaneSegment seg = allSegments[index];
            seg.AddSnow(3);
            mapVisualizer.SegmentDirectory[seg].UpdateVisuals();
            Debug.Log($"[Tester] Snowy: {mapVisualizer.SegmentDirectory[seg].gameObject.name}");
        }

        // Icy segments
        for (int i = 0; i < icySegmentCount && index < allSegments.Count; i++, index++)
        {
            LaneSegment seg = allSegments[index];
            seg.SetIce(true);
            mapVisualizer.SegmentDirectory[seg].UpdateVisuals();
            Debug.Log($"[Tester] Icy: {mapVisualizer.SegmentDirectory[seg].gameObject.name}");
        }

        // Accident segments
        for (int i = 0; i < accidentSegmentCount && index < allSegments.Count; i++, index++)
        {
            LaneSegment seg = allSegments[index];
            seg.SetAccident(true);
            mapVisualizer.SegmentDirectory[seg].UpdateVisuals();
            Debug.Log($"[Tester] Accident: {mapVisualizer.SegmentDirectory[seg].gameObject.name}");
        }

        Debug.Log($"[Tester] Done. Snowy: {snowySegmentCount}, Icy: {icySegmentCount}, Accident: {accidentSegmentCount}");
    }
}