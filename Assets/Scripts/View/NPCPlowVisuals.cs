using UnityEngine;
using SnowPlow.Model.Vehicles;
using SnowPlow.Model.Tools;
using SnowPlowVehicle = SnowPlow.Model.Vehicles.SnowPlow;

public class NPCPlowVisuals : MonoBehaviour
{
    [Header("Isometric Sprites - Truck Body")]
    public SpriteRenderer spriteRenderer;
    public Sprite iso_UpRight, iso_UpLeft, iso_DownRight, iso_DownLeft, iso_Right, iso_Left, iso_Up, iso_Down;

    [Header("Tools Setup")]
    public ToolVisualSet[] toolVisuals;
    private ToolVisualSet activeToolVisual;

    private SnowPlowVehicle plowModel;

    // Itt tároljuk el, hol volt a kocsi egy pillanattal ezelőtt
    private Vector3 lastPosition;

    private void Start()
    {
        lastPosition = transform.position;
    }

    public void SetPlowModel(SnowPlowVehicle model)
    {
        plowModel = model;
        UpdateEquippedToolVisual();
    }

    public void UpdateEquippedToolVisual()
    {
        if (plowModel == null || plowModel.EquippedTool == null) return;

        PlowToolType currentType = plowModel.EquippedTool.Type();
        activeToolVisual = null;

        foreach (var tool in toolVisuals)
        {
            if (tool.toolType == currentType)
            {
                tool.visualObject.SetActive(true);
                activeToolVisual = tool;
            }
            else
            {
                tool.visualObject.SetActive(false);
            }
        }

        UpdateSprite(0f);
    }

    // A TRÜKK: LateUpdate, ami minden más mozgató script után fut le!
    private void LateUpdate()
    {
        // 1. Kiszámoljuk a mozgást a pozícióváltozásból (mert a Rigidbody velocity itt 0)
        Vector3 movement = transform.position - lastPosition;

        if (movement.magnitude > 0.001f)
        {
            float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
            UpdateSprite(angle);
        }

        // 2. "Lefagyasztjuk" a fizikai forgatást! 
        // Bármit is csinált az NPCVehicleMover, mi visszatesszük egyenesbe a grafikát.
        transform.rotation = Quaternion.identity;

        // 3. Eltesszük a pozíciót a következő körre
        lastPosition = transform.position;
    }

    void UpdateSprite(float angle)
    {
        if (angle < 0) angle += 360f;

        bool isToolBehind = false;
        Sprite truckSprite = null;
        Sprite toolSprite = null;
        Vector2 toolOffset = Vector2.zero;

        if (angle >= 337.5f || angle < 22.5f) { truckSprite = iso_Right; if (activeToolVisual != null) { toolSprite = activeToolVisual.iso_Right; toolOffset = activeToolVisual.offset_Right; } isToolBehind = false; }
        else if (angle >= 22.5f && angle < 67.5f) { truckSprite = iso_UpRight; if (activeToolVisual != null) { toolSprite = activeToolVisual.iso_UpRight; toolOffset = activeToolVisual.offset_UpRight; } isToolBehind = true; }
        else if (angle >= 67.5f && angle < 112.5f) { truckSprite = iso_Up; if (activeToolVisual != null) { toolSprite = activeToolVisual.iso_Up; toolOffset = activeToolVisual.offset_Up; } isToolBehind = true; }
        else if (angle >= 112.5f && angle < 157.5f) { truckSprite = iso_UpLeft; if (activeToolVisual != null) { toolSprite = activeToolVisual.iso_UpLeft; toolOffset = activeToolVisual.offset_UpLeft; } isToolBehind = true; }
        else if (angle >= 157.5f && angle < 202.5f) { truckSprite = iso_Left; if (activeToolVisual != null) { toolSprite = activeToolVisual.iso_Left; toolOffset = activeToolVisual.offset_Left; } isToolBehind = false; }
        else if (angle >= 202.5f && angle < 247.5f) { truckSprite = iso_DownLeft; if (activeToolVisual != null) { toolSprite = activeToolVisual.iso_DownLeft; toolOffset = activeToolVisual.offset_DownLeft; } isToolBehind = false; }
        else if (angle >= 247.5f && angle < 292.5f) { truckSprite = iso_Down; if (activeToolVisual != null) { toolSprite = activeToolVisual.iso_Down; toolOffset = activeToolVisual.offset_Down; } isToolBehind = false; }
        else if (angle >= 292.5f && angle < 337.5f) { truckSprite = iso_DownRight; if (activeToolVisual != null) { toolSprite = activeToolVisual.iso_DownRight; toolOffset = activeToolVisual.offset_DownRight; } isToolBehind = false; }

        spriteRenderer.sprite = truckSprite;

        if (activeToolVisual != null && activeToolVisual.spriteRenderer != null && toolSprite != null)
        {
            activeToolVisual.spriteRenderer.sprite = toolSprite;
            activeToolVisual.visualObject.transform.localPosition = new Vector3(toolOffset.x, toolOffset.y, 0f);

            if (isToolBehind) activeToolVisual.spriteRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
            else activeToolVisual.spriteRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        }
    }
}