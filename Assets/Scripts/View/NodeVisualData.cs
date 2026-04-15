using SnowPlow.Model.Map;
using UnityEngine;

public class NodeVisualData : MonoBehaviour
{
    public MapNode LogicalNode { get; private set; }

    public void Initialize(MapNode node)
    {
        LogicalNode = node;
        gameObject.name = $"Node_{node.Id}";
    }
}
