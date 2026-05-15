using SnowPlow.Controller.NPCMovement;
using SnowPlow.Controller.NPCMovement;
using SnowPlow.Controller.Pathfinding;
using SnowPlow.Model.Map;
using Unity.Netcode;
using UnityEngine;

public class BusMovement : MonoBehaviour
{
    public Rigidbody2D myRigidBody2D;
    public float speedMultiplyer;

    [Header("Isometric Sprites")]
    public SpriteRenderer spriteRenderer;
    public Sprite iso_UpRight;
    public Sprite iso_UpLeft;
    public Sprite iso_DownRight;
    public Sprite iso_DownLeft;
    public Sprite iso_Right;
    public Sprite iso_Left;
    public Sprite iso_Up;
    public Sprite iso_Down;

    [Header("Collider")]
    public BoxCollider2D boxCollider;
    public Transform colliderPivot;
    public Vector2 colliderSize = new Vector2(6.0f, 3.0f);

    [Header("Station Logic")]
    public VisualSegment stationA;
    public VisualSegment stationB;

    private int passengersOnBoard = 0;
    private VisualSegment currentStation = null;
    private VisualSegment pickupStation = null;

    private readonly CarTraversalPolicy traversalPolicy = new CarTraversalPolicy();
    private int touchingRoads = 0;
    private bool isBlocked = false;
    private float blockCooldown = 0f;
    private const float BlockCooldownTime = 0.1f;
    private float stunTimer = 0f;
    private const float StunDuration = 2f;
    private bool isOnIce = false;
    private const float IceFriction = 0.995f;    // how little it slows down (closer to 1 = more slippery)
    private const float IceControlMultiplier = 0.15f; // how much control the player has on ice
    private float tripStartTime = 0f;

    // --- TRIP TRACKING VARIABLES ---
    private VisualSegment tripOriginStation = null; // The station where the round trip started
    private bool hasReachedMidpoint = false;        // True if we reached the "other" station
    private VisualSegment lastStationVisited = null; // Prevents re-triggering while inside collider

    private Bus busModel;
    public void SetBusModel(Bus model)
    {
        busModel = model;
    }

    void Awake()
    {
        myRigidBody2D.freezeRotation = true;
        if (boxCollider != null)
            boxCollider.size = colliderSize;
    }
    public void SetStations(VisualSegment a, VisualSegment b)
    {
        stationA = a;
        stationB = b;
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"TRIGGER with: {other.name} | tag: {other.tag}");
        if (other.CompareTag("Road")) touchingRoads++;

        if (other.CompareTag("Vehicle"))
        {
            Debug.Log("[Bus] Vehicle hit! Starting stun.");

            Stun();

            NPCVehicleMover otherNpcMover = other.GetComponentInParent<NPCVehicleMover>();
            if (otherNpcMover != null)
            {
                otherNpcMover.Stun();
            }

            return;
        }

        if (blockCooldown > 0f) return;

        //VisualSegment vs = other.GetComponent<VisualSegment>();
        VisualSegment vs = other.GetComponentInParent<VisualSegment>();
        if (vs != null)
        {
            Debug.Log($"FOUND VisualSegment: {vs.gameObject.name}");
        }
        else
        {
            Debug.Log("NO VisualSegment FOUND");
        }
        if (vs != null && vs.LanePosition != null)
        {
            isOnIce = vs.LanePosition.Lane[vs.LanePosition.SegmentIndex].HasIce;

            if (!traversalPolicy.CanEnterSegment(vs.LanePosition))
            {
                isBlocked = true;
                blockCooldown = BlockCooldownTime;
                myRigidBody2D.linearVelocity = -myRigidBody2D.linearVelocity * 1.5f;
            }
            else
            {
                isBlocked = false;
            }
        }

        if (vs != null)
        {
            if (vs == stationA || vs == stationB)
            {
                currentStation = vs;
                Debug.Log($"[Bus] Arrived at station: {vs.gameObject.name}");
                if (currentStation != lastStationVisited)
                {
                    HandleTripCounter(currentStation);
                    lastStationVisited = currentStation;
                }
            }
        }
    }


    private void HandleTripCounter(VisualSegment arrivedStation)
    {
        // 1. If we don't have an origin yet, set it.
        if (tripOriginStation == null)
        {
            tripOriginStation = arrivedStation;
            hasReachedMidpoint = false;
            tripStartTime = Time.time;                          // ← start timer
            Debug.Log($"[Bus] Trip started at {arrivedStation.gameObject.name}");
        }
        // 2. If we are at the OTHER station, we've reached the midpoint.
        else if (arrivedStation != tripOriginStation && !hasReachedMidpoint)
        {
            hasReachedMidpoint = true;
            Debug.Log($"[Bus] Midpoint reached at {arrivedStation.gameObject.name}");
        }
        // 3. If we return to the ORIGIN after reaching the midpoint, trip is complete.
        else if (arrivedStation == tripOriginStation && hasReachedMidpoint)
        {
            float elapsed = Time.time - tripStartTime;
            if (busModel != null)
            {
                busModel.IncreaseTripCount(elapsed);            // ← pass elapsed
                Debug.Log($"[Bus] Full trip complete in {elapsed:F1}s! Total: {busModel.CompletedTrips}");
            }
            hasReachedMidpoint = false;
            tripStartTime = Time.time;                          // ← reset for next trip
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Road"))
            touchingRoads = Mathf.Max(0, touchingRoads - 1); // ← Bug 3 fix, was: touchingRoads--

        //VisualSegment vs = other.GetComponent<VisualSegment>();
        VisualSegment vs = other.GetComponentInParent<VisualSegment>();
        if (vs != null)
        {
            Debug.Log($"Triggered: {vs.gameObject.name}");

            if (stationA != null)
                Debug.Log($"stationA: {stationA.gameObject.name}");

            if (stationB != null)
                Debug.Log($"stationB: {stationB.gameObject.name}");

            Debug.Log($"vs == stationA ? {vs == stationA}");
            Debug.Log($"vs == stationB ? {vs == stationB}");
        }
        if (vs != null && vs.LanePosition != null)
        {
            isBlocked = false; // ← Bug 2 fix, was: if (!traversalPolicy.CanEnterSegment(...)) isBlocked = false;

            if (vs.LanePosition.Lane[vs.LanePosition.SegmentIndex].HasIce)
            {
                isOnIce = false;
            }
        }

        if (vs != null && vs == currentStation)
        {
            currentStation = null;
        }

        if (vs != null && vs == lastStationVisited)
        {
            lastStationVisited = null;
        }
    }

    private void FixedUpdate()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();

        if (netObj != null && !netObj.IsOwner)
            return;
        if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
            myRigidBody2D.linearVelocity = Vector2.zero;
            return;
        }

        if (blockCooldown > 0f)
        {
            blockCooldown -= Time.fixedDeltaTime;
            if (blockCooldown <= 0f)
                isBlocked = false; // ← Bug 1 fix, auto-release if exit trigger was missed
        }

        if (touchingRoads <= 0)
        {
            myRigidBody2D.linearVelocity = -myRigidBody2D.linearVelocity * 1.5f;
            return;
        }

        if (isBlocked) return;

        float controlMultiplier = isOnIce ? IceControlMultiplier : 1f;

        if (Input.GetKey(KeyCode.W))
            myRigidBody2D.linearVelocity += Vector2.up * speedMultiplyer * controlMultiplier * Time.fixedDeltaTime;

        if (Input.GetKey(KeyCode.S))
            myRigidBody2D.linearVelocity += Vector2.down * speedMultiplyer * controlMultiplier * Time.fixedDeltaTime;

        if (Input.GetKey(KeyCode.A))
            myRigidBody2D.linearVelocity += Vector2.left * speedMultiplyer * controlMultiplier * Time.fixedDeltaTime;

        if (Input.GetKey(KeyCode.D))
            myRigidBody2D.linearVelocity += Vector2.right * speedMultiplyer * controlMultiplier * Time.fixedDeltaTime;

        if (isOnIce)
            myRigidBody2D.linearVelocity *= IceFriction;

        if (myRigidBody2D.linearVelocity.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(myRigidBody2D.linearVelocity.y, myRigidBody2D.linearVelocity.x) * Mathf.Rad2Deg;
            UpdateSprite(angle);
        }
    }

    private void Update()
    {
        // Station interaction
        if (Input.GetKeyDown(KeyCode.Space) && currentStation != null && myRigidBody2D.linearVelocity.magnitude < 0.1f)
        {
            Debug.Log($"SPACE pressed | station: {currentStation != null} | speed: {myRigidBody2D.linearVelocity.magnitude}");
            if (currentStation == null)
            {
                Debug.Log("No current station!");
                return;
            }
            BusStationPassengers passengers = currentStation.StationPassengers;
            if (passengers == null) return;

            bool isOtherStation = pickupStation != null && currentStation != pickupStation;

            if (isOtherStation)
            {
                passengers.DropOffPassengers(passengersOnBoard);
                Debug.Log($"[Bus] Dropped off {passengersOnBoard} passengers at {currentStation.gameObject.name}.");
                busModel.IncreasePassangers(passengersOnBoard);
                passengersOnBoard = 0;
                pickupStation = null;
            }
            else if (!isOtherStation || passengersOnBoard == 0)
            {
                int boarded = passengers.BoardPassengers();
                passengersOnBoard += boarded;
                pickupStation = currentStation;
                Debug.Log($"[Bus] Picked up {boarded}, total on board: {passengersOnBoard}.");
            }
        }
    }

    void UpdateSprite(float angle)
    {
        if (angle < 0) angle += 360f;

        if (angle >= 337.5f || angle < 22.5f) spriteRenderer.sprite = iso_Right;
        else if (angle >= 22.5f && angle < 67.5f) spriteRenderer.sprite = iso_UpRight;
        else if (angle >= 67.5f && angle < 112.5f) spriteRenderer.sprite = iso_Up;
        else if (angle >= 112.5f && angle < 157.5f) spriteRenderer.sprite = iso_UpLeft;
        else if (angle >= 157.5f && angle < 202.5f) spriteRenderer.sprite = iso_Left;
        else if (angle >= 202.5f && angle < 247.5f) spriteRenderer.sprite = iso_DownLeft;
        else if (angle >= 247.5f && angle < 292.5f) spriteRenderer.sprite = iso_Down;
        else if (angle >= 292.5f && angle < 337.5f) spriteRenderer.sprite = iso_DownRight;

        // Snap to nearest 45° and apply to the collider child
        if (colliderPivot != null)
        {
            float snapped = Mathf.Round(angle / 45f) * 45f;
            colliderPivot.localRotation = Quaternion.Euler(0f, 0f, snapped - 90f);
        }
    }

    public void Stun()
    {
        stunTimer = StunDuration;

        if (myRigidBody2D != null)
        {
            myRigidBody2D.linearVelocity = Vector2.zero;
            myRigidBody2D.angularVelocity = 0f;
        }
    }
}