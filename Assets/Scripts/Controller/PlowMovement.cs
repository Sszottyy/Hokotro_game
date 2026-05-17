using SnowPlow.Controller.Pathfinding;
using SnowPlow.Model.Map;
using SnowPlow.Model.Players;
using SnowPlow.Model.Tools;
using SnowPlow.Model.Vehicles;
using UnityEngine;
using SnowPlowVehicle = SnowPlow.Model.Vehicles.SnowPlow;
using System.Collections;
using SnowPlow.Model.Players;
using Unity.Netcode;

[System.Serializable]
public class ToolVisualSet
{
    public PlowToolType toolType;
    public GameObject visualObject;
    public SpriteRenderer spriteRenderer;

    // [JÖVŐ ZENÉJE] Részecske effekt (Hó/Tűz)
    // public ParticleSystem toolEffect; 

    [Header("8 Directional Sprites")]
    public Sprite iso_UpRight;
    public Sprite iso_UpLeft;
    public Sprite iso_DownRight;
    public Sprite iso_DownLeft;
    public Sprite iso_Right;
    public Sprite iso_Left;
    public Sprite iso_Up;
    public Sprite iso_Down;

    [Header("Manual Offsets (X, Y pozíciók)")]
    public Vector2 offset_UpRight;
    public Vector2 offset_UpLeft;
    public Vector2 offset_DownRight;
    public Vector2 offset_DownLeft;
    public Vector2 offset_Right;
    public Vector2 offset_Left;
    public Vector2 offset_Up;
    public Vector2 offset_Down;

    [Header("Manual Rotations (Z forgatás fokokban)")]
    public float rot_UpRight;
    public float rot_UpLeft;
    public float rot_DownRight;
    public float rot_DownLeft;
    public float rot_Right;
    public float rot_Left;
    public float rot_Up;
    public float rot_Down;
}

public class PlowMovement : NetworkBehaviour
{
    public NetworkVariable<ulong> OwnerClientId =
    new NetworkVariable<ulong>();
    public Rigidbody2D myRigidBody2D;
    public float speedMultiplyer = 500f;
    private PlowToolType equippedToolType = PlowToolType.Sweaper;
    private Vector3 lastPosition;
    [Header("Isometric Sprites - Truck Body")]
    public SpriteRenderer spriteRenderer;
    public Sprite iso_UpRight, iso_UpLeft, iso_DownRight, iso_DownLeft, iso_Right, iso_Left, iso_Up, iso_Down;

    [Header("Tools Setup")]
    public ToolVisualSet[] toolVisuals;
    private ToolVisualSet activeToolVisual;

    [Header("Collider Setup")]
    public BoxCollider2D boxCollider;
    public Vector2 horizontalSize = new Vector2(6f, 3f);
    public Vector2 verticalSize = new Vector2(3f, 6f);

    private readonly SnowPlowTraversalPolicy traversalPolicy = new SnowPlowTraversalPolicy();
    private int touchingRoads = 0;
    private bool isBlocked = false;
    private float blockCooldown = 0f;
    private const float BlockCooldownTime = 0.1f;
    private float stunTimer = 0f;
    private const float StunDuration = 5f;
    private bool isOnIce = false;
    private const float IceFriction = 0.995f;
    private const float IceControlMultiplier = 0.15f;

    private SnowPlowVehicle plowModel;

    public void SetPlowModel(SnowPlowVehicle model)
    {
        Debug.Log("[PLOWMOVEMENT] SetPlowModel CALLED");

        plowModel = model;

        equippedToolType = plowModel != null
     ? plowModel.EquippedToolType
     : PlowToolType.Sweaper;

        Debug.Log("[PLOWMOVEMENT] model null? " + (plowModel == null));
        Debug.Log("[PLOWMOVEMENT] tool = " + equippedToolType);

        UpdateEquippedToolVisual();
    }
    public void SetEquippedToolType(PlowToolType type)
    {
        equippedToolType = type;
        UpdateEquippedToolVisual();
    }
    public SnowPlowVehicle GetPlowModel()
    {
        return plowModel;
    }

    public void UpdateEquippedToolVisual()
    {
        if (toolVisuals == null || toolVisuals.Length == 0) return;
        
        PlowToolType currentType = equippedToolType;
        activeToolVisual = null;

        // Kikapcsoljuk a felesleges fejeket, bekapcsoljuk az aktívat
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

                // [JÖVŐ ZENÉJE] Extra védelem, hogy ne lőjön a kikapcsolt fej
                // if (tool.toolEffect != null && tool.toolEffect.isPlaying) tool.toolEffect.Stop(); 
            }
        }

        // Frissítjük a grafikát
        if (myRigidBody2D.linearVelocity.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(myRigidBody2D.linearVelocity.y, myRigidBody2D.linearVelocity.x) * Mathf.Rad2Deg;
            UpdateSprite(angle);
        }
        else
        {
            UpdateSprite(0f);
        }
    }

    // --- AZ EREDETI BUSZOS FIZIKA (POLICY TILTÁS NÉLKÜL) ---
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Road")) touchingRoads++;

        if (other.CompareTag("Vehicle"))
        {
            stunTimer = StunDuration;
            myRigidBody2D.linearVelocity = Vector2.zero;
            return;
        }

        if (blockCooldown > 0f) return;

        VisualSegment vs = other.GetComponent<VisualSegment>();
        if (vs != null && vs.LanePosition != null)
        {
            isOnIce = vs.LanePosition.Lane[vs.LanePosition.SegmentIndex].HasIce;
            isBlocked = false;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Road")) touchingRoads--;

        VisualSegment vs = other.GetComponent<VisualSegment>();
        if (vs != null && vs.LanePosition != null)
        {
            if (vs.LanePosition.Lane[vs.LanePosition.SegmentIndex].HasIce) isOnIce = false;
        }
    }

    private void UpdateRemoteVisuals()
    {
        Vector3 delta =
            transform.position - lastPosition;

        if (delta.magnitude > 0.001f)
        {
            float angle =
                Mathf.Atan2(delta.y, delta.x)
                * Mathf.Rad2Deg;

            UpdateSprite(angle);
        }

        lastPosition = transform.position;
    }
    private void FixedUpdate()
    {

        UpdateRemoteVisuals();
        if (!IsOwner)
        {
            return;
        }
        if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
            myRigidBody2D.linearVelocity = Vector2.zero;
            return;
        }

        if (blockCooldown > 0f)
        {
            blockCooldown -= Time.fixedDeltaTime;
            if (blockCooldown <= 0f) isBlocked = false;
        }

        if (touchingRoads <= 0)
        {
            if (myRigidBody2D.linearVelocity.magnitude < 0.5f)
            {
                myRigidBody2D.linearVelocity = new Vector2(5f, 5f);
            }
            else
            {
                myRigidBody2D.linearVelocity = -myRigidBody2D.linearVelocity * 1.5f;
            }
            return;
        }

        if (isBlocked) return;

        float controlMultiplier = isOnIce ? IceControlMultiplier : 1f;

        if (Input.GetKey(KeyCode.W)) myRigidBody2D.linearVelocity += Vector2.up * speedMultiplyer * controlMultiplier * Time.fixedDeltaTime;
        if (Input.GetKey(KeyCode.S)) myRigidBody2D.linearVelocity += Vector2.down * speedMultiplyer * controlMultiplier * Time.fixedDeltaTime;
        if (Input.GetKey(KeyCode.A)) myRigidBody2D.linearVelocity += Vector2.left * speedMultiplyer * controlMultiplier * Time.fixedDeltaTime;
        if (Input.GetKey(KeyCode.D)) myRigidBody2D.linearVelocity += Vector2.right * speedMultiplyer * controlMultiplier * Time.fixedDeltaTime;

        if (isOnIce) myRigidBody2D.linearVelocity *= IceFriction;

        if (myRigidBody2D.linearVelocity.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(myRigidBody2D.linearVelocity.y, myRigidBody2D.linearVelocity.x) * Mathf.Rad2Deg;
            UpdateSprite(angle);
        }
        //Debug.Log("[PLOWMOVEMENT] plowModel null? " + (plowModel == null));
        Collider2D[] hits = Physics2D.OverlapCircleAll(
        transform.position,
        0.5f
            );

        foreach (Collider2D hit in hits)
        {
            VisualSegment vs = hit.GetComponent<VisualSegment>();

            if (vs != null && vs.LanePosition != null)
            {
                if (plowModel != null)
                {
                    plowModel.ApplyToolEffect(vs.LanePosition);
                }
            }
        }

        /* [JÖVŐ ZENÉJE] --- RÉSZECSKE EFFEKTEK BE- ÉS KIKAPCSOLÁSA MOZGÁS ALAPJÁN ---
        if (activeToolVisual != null && activeToolVisual.toolEffect != null)
        {
            if (myRigidBody2D.linearVelocity.magnitude > 0.1f)
            {
                if (!activeToolVisual.toolEffect.isPlaying) activeToolVisual.toolEffect.Play();
            }
            else
            {
                if (activeToolVisual.toolEffect.isPlaying) activeToolVisual.toolEffect.Stop();
            }
        }
        */
    }

    void UpdateSprite(float angle)
    {
        if (angle < 0) angle += 360f;

        bool isSideView = false;
        bool isToolBehind = false;
        Sprite truckSprite = null;
        Sprite toolSprite = null;
        Vector2 toolOffset = Vector2.zero;
        float toolRotation = 0f; // --- FORGATÁS VÁLTOZÓJA ---

        // 8 irányú logika (Offsetekkel, Forgatásokkal és Kitakarással)
        if (angle >= 337.5f || angle < 22.5f)
        {
            truckSprite = iso_Right;
            if (activeToolVisual != null) { toolSprite = activeToolVisual.iso_Right; toolOffset = activeToolVisual.offset_Right; toolRotation = activeToolVisual.rot_Right; }
            isSideView = true;
            isToolBehind = false;
        }
        else if (angle >= 22.5f && angle < 67.5f)
        {
            truckSprite = iso_UpRight;
            if (activeToolVisual != null) { toolSprite = activeToolVisual.iso_UpRight; toolOffset = activeToolVisual.offset_UpRight; toolRotation = activeToolVisual.rot_UpRight; }
            isSideView = false;
            isToolBehind = true;
        }
        else if (angle >= 67.5f && angle < 112.5f)
        {
            truckSprite = iso_Up;
            if (activeToolVisual != null) { toolSprite = activeToolVisual.iso_Up; toolOffset = activeToolVisual.offset_Up; toolRotation = activeToolVisual.rot_Up; }
            isSideView = false;
            isToolBehind = true;
        }
        else if (angle >= 112.5f && angle < 157.5f)
        {
            truckSprite = iso_UpLeft;
            if (activeToolVisual != null) { toolSprite = activeToolVisual.iso_UpLeft; toolOffset = activeToolVisual.offset_UpLeft; toolRotation = activeToolVisual.rot_UpLeft; }
            isSideView = false;
            isToolBehind = true;
        }
        else if (angle >= 157.5f && angle < 202.5f)
        {
            truckSprite = iso_Left;
            if (activeToolVisual != null) { toolSprite = activeToolVisual.iso_Left; toolOffset = activeToolVisual.offset_Left; toolRotation = activeToolVisual.rot_Left; }
            isSideView = true;
            isToolBehind = false;
        }
        else if (angle >= 202.5f && angle < 247.5f)
        {
            truckSprite = iso_DownLeft;
            if (activeToolVisual != null) { toolSprite = activeToolVisual.iso_DownLeft; toolOffset = activeToolVisual.offset_DownLeft; toolRotation = activeToolVisual.rot_DownLeft; }
            isSideView = false;
            isToolBehind = false;
        }
        else if (angle >= 247.5f && angle < 292.5f)
        {
            truckSprite = iso_Down;
            if (activeToolVisual != null) { toolSprite = activeToolVisual.iso_Down; toolOffset = activeToolVisual.offset_Down; toolRotation = activeToolVisual.rot_Down; }
            isSideView = false;
            isToolBehind = false;
        }
        else if (angle >= 292.5f && angle < 337.5f)
        {
            truckSprite = iso_DownRight;
            if (activeToolVisual != null) { toolSprite = activeToolVisual.iso_DownRight; toolOffset = activeToolVisual.offset_DownRight; toolRotation = activeToolVisual.rot_DownRight; }
            isSideView = false;
            isToolBehind = false;
        }

        spriteRenderer.sprite = truckSprite;

        if (activeToolVisual != null && activeToolVisual.spriteRenderer != null && toolSprite != null)
        {
            activeToolVisual.spriteRenderer.sprite = toolSprite;

            // --- X-Y Pozíció és Z Forgatás beállítása ---
            activeToolVisual.visualObject.transform.localPosition = new Vector3(toolOffset.x, toolOffset.y, 0f);
            activeToolVisual.visualObject.transform.localRotation = Quaternion.Euler(0f, 0f, toolRotation);

            // Dinamikus Rétegrendezés (Order in Layer automata beállítása)
            if (isToolBehind)
            {
                activeToolVisual.spriteRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
            }
            else
            {
                activeToolVisual.spriteRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            }

            /* [JÖVŐ ZENÉJE] --- PARTICLE EFFECT FORGATÁSA ÉS TAKARÁSA ---
            if (activeToolVisual.toolEffect != null)
            {
                float effectAngle = angle;
                if (activeToolVisual.toolType == PlowToolType.Sweaper || activeToolVisual.toolType == PlowToolType.IceBreaker)
                    effectAngle -= 90f; // Oldalra szór
                else if (activeToolVisual.toolType == PlowToolType.Salt || activeToolVisual.toolType == PlowToolType.Vomit)
                    effectAngle += 180f; // Hátra szór
                // Dragon (Sárkány) fixen előre (effectAngle = angle) marad

                activeToolVisual.toolEffect.transform.rotation = Quaternion.Euler(0, 0, effectAngle);

                ParticleSystemRenderer psRenderer = activeToolVisual.toolEffect.GetComponent<ParticleSystemRenderer>();
                if (psRenderer != null)
                {
                    psRenderer.sortingOrder = activeToolVisual.spriteRenderer.sortingOrder + 1; 
                }
            }
            */
        }

        // Dinamikus Collider méretezés az irány alapján
        if (boxCollider != null)
            boxCollider.size = isSideView ? horizontalSize : verticalSize;
    }
    private void Start()
    {
        lastPosition = transform.position;
        StartCoroutine(SetupVisualsDelayed());
    }

    private IEnumerator SetupVisualsDelayed()
    {
        while (plowModel == null)
        {
            yield return null;
        }

        Debug.Log(
    "[CLIENT VISUAL] updating visual: " +
    plowModel.EquippedToolType
);

        UpdateEquippedToolVisual();
    }
    public void LateInitialize()
    {
        Player player =
            GameManager.Instance.GetPlayer(
                OwnerClientId.Value);

        if (player == null)
        {
            Debug.LogWarning("LateInitialize: player null");
            return;
        }

        SnowPlowVehicle plow =
            player.GetOwnedSnowPlow();

        if (plow == null)
        {
            Debug.LogWarning("LateInitialize: plow null");
            return;
        }

        SetPlowModel(plow);

        Debug.Log(
    "[CLIENT] LateInitialize SUCCESS: " +
    plow.EquippedToolType);
        UpdateEquippedToolVisual();
    }
    public override void OnNetworkSpawn()
    {
        StartCoroutine(DelayedInit());
    }

    private IEnumerator DelayedInit()
    {
        // várunk 2 frame-et
        yield return null;
        yield return null;

        LateInitialize();
    }
}