using UnityEngine;

public class NPCCarVisuals : MonoBehaviour
{
    [System.Serializable]
    public class CarVisualSet
    {
        public string name;

        [Header("Isometric Sprites")]
        public Sprite iso_UpRight;
        public Sprite iso_UpLeft;
        public Sprite iso_DownRight;
        public Sprite iso_DownLeft;
        public Sprite iso_Right;
        public Sprite iso_Left;
        public Sprite iso_Up;
        public Sprite iso_Down;
    }

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Car Looks")]
    [SerializeField] private CarVisualSet[] carLooks;

    private static int nextLookIndex = 0;

    private CarVisualSet activeLook;
    private Vector3 lastPosition;

    private void Start()
    {
        ChooseNextLook();
        lastPosition = transform.position;

        // Kezdő sprite, hogy spawn után rögtön legyen kinézete.
        UpdateSprite(270f);
    }

    private void LateUpdate()
    {
        Vector3 movement = transform.position - lastPosition;

        if (movement.magnitude > 0.001f)
        {
            float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
            UpdateSprite(angle);
        }

        // Ugyanaz a trükk, mint az NPC hókotró vizuálnál:
        // a mozgás script forgathatja a rootot, de a sprite-alapú kinézet miatt
        // a grafikát visszatesszük egyenesbe.
        transform.rotation = Quaternion.identity;

        lastPosition = transform.position;
    }

    private void ChooseNextLook()
    {
        if (carLooks == null || carLooks.Length == 0)
        {
            Debug.LogWarning("NPCCarVisuals: no car looks assigned.");
            return;
        }

        int index = nextLookIndex % carLooks.Length;
        nextLookIndex++;

        activeLook = carLooks[index];
    }

    private void UpdateSprite(float angle)
    {
        if (activeLook == null) return;
        if (spriteRenderer == null) return;

        if (angle < 0f)
        {
            angle += 360f;
        }

        Sprite selectedSprite = null;

        if (angle >= 337.5f || angle < 22.5f)
        {
            selectedSprite = activeLook.iso_Right;
        }
        else if (angle >= 22.5f && angle < 67.5f)
        {
            selectedSprite = activeLook.iso_UpRight;
        }
        else if (angle >= 67.5f && angle < 112.5f)
        {
            selectedSprite = activeLook.iso_Up;
        }
        else if (angle >= 112.5f && angle < 157.5f)
        {
            selectedSprite = activeLook.iso_UpLeft;
        }
        else if (angle >= 157.5f && angle < 202.5f)
        {
            selectedSprite = activeLook.iso_Left;
        }
        else if (angle >= 202.5f && angle < 247.5f)
        {
            selectedSprite = activeLook.iso_DownLeft;
        }
        else if (angle >= 247.5f && angle < 292.5f)
        {
            selectedSprite = activeLook.iso_Down;
        }
        else if (angle >= 292.5f && angle < 337.5f)
        {
            selectedSprite = activeLook.iso_DownRight;
        }

        if (selectedSprite != null)
        {
            spriteRenderer.sprite = selectedSprite;
        }
    }
}