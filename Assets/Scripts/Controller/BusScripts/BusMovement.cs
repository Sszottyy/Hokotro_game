using SnowPlow.Controller.NPCMovement;
using SnowPlow.Controller.NPCMovement;
using SnowPlow.Controller.Pathfinding;
using SnowPlow.Model.Map;
using Unity.Netcode;
using UnityEngine;

public class BusMovement : NetworkBehaviour
{
    public NetworkVariable<int> PassengersOnBoard
    => passengersOnBoard;
    private Vector3 lastPosition;

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
    //public Vector2 colliderSize = new Vector2(6.0f, 3.0f);
    public Vector2 colliderSize = new Vector2(6.0f, 3.0f);

    [Header("Station Logic")]
    //public VisualSegment stationA;
    //public VisualSegment stationB;
    //private LanePosition stationAPosition;
    // private LanePosition stationBPosition;

    //private int passengersOnBoard = 0;
    private NetworkVariable<int> passengersOnBoard =
    new NetworkVariable<int>(0);
    
    private GameObject currentStation = null;
    private GameObject pickupStation = null;

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
    //private VisualSegment tripOriginStation = null; // The station where the round trip started
    private LanePosition tripOriginPosition = null;
    private bool hasReachedMidpoint = false;        // True if we reached the "other" station
    private GameObject lastStationVisited = null; // Prevents re-triggering while inside collider

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
        if (a == null || b == null)
        {
            Debug.LogError("SetStations received null station!");
            return;
        }

        Debug.Log($"Bus stations set: {a.name} | {b.name}");
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(
    $"TRIGGER with: {other.name} | tag: {other.tag} | layer: {LayerMask.LayerToName(other.gameObject.layer)}"
);
        //Debug.Log($"TRIGGER with: {other.name} | tag: {other.tag}");
        

        VisualSegment roadSegment =
            other.GetComponentInParent<VisualSegment>();

        if (roadSegment != null)
        {
            touchingRoads++;
        }

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
        VisualSegment vs =
    other.GetComponentInParent<VisualSegment>();
        Debug.Log(
    $"Station detected? {vs != null} | object: {other.name}"
);

        Debug.LogError("nigger "+(vs==null));

        Debug.Log($"VisualSegment direct: {vs != null}");
        if (vs != null)
        {
            //Debug.Log($"FOUND VisualSegment: {vs.gameObject.name}");
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

                LaneSegment segment = vs.LanePosition.Lane[vs.LanePosition.SegmentIndex];

                segment.RegisterVehiclePassForIceFormation();
                Debug.Log(
                    $"ICE PASS COUNT: {segment.PassedVehicleCount}"
                    );
                Debug.Log(
                 $"SEGMENT ENTER: lane={vs.LanePosition.Lane.Id} " +
                $"segment={vs.LanePosition.SegmentIndex} " +
                $"count={segment.PassedVehicleCount}"
                );

                vs.UpdateVisuals();
            }
        }

        //check for bus stop
        if(other.gameObject.tag.Equals("BusStop") && currentStation==null)
        {
            currentStation = other.gameObject;
        }
    }


    void OnTriggerExit2D(Collider2D other)
    {
        

        VisualSegment roadSegment =
            other.GetComponentInParent<VisualSegment>();

        if (roadSegment != null)
        {
            touchingRoads =
                Mathf.Max(0, touchingRoads - 1);
        }
        if (roadSegment != null && roadSegment.LanePosition != null)
        {
            isBlocked = false; // ← Bug 2 fix, was: if (!traversalPolicy.CanEnterSegment(...)) isBlocked = false;

            if (roadSegment.LanePosition.Lane[roadSegment.LanePosition.SegmentIndex].HasIce)
            {
                isOnIce = false;
            }
        }

        if (other.gameObject.tag.Equals("BusStop"))
        {
            if (other.gameObject == currentStation)
            {
                currentStation = null;
                lastStationVisited = other.gameObject;
            }
        }
    }

    /*private bool IsStation(VisualSegment vs)
    {
        if (vs == null || vs.LanePosition == null)
            return false;

        return
            SamePosition(vs.LanePosition, stationAPosition) ||
            SamePosition(vs.LanePosition, stationBPosition);
    }*/

    /*private bool SamePosition(LanePosition a, LanePosition b)
    {
        if (a == null || b == null)
            return false;

        return
            a.Lane == b.Lane &&
            a.SegmentIndex == b.SegmentIndex;
    }*/

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
    private void HandleTripCounter(VisualSegment arrivedStation)
    {
        LanePosition arrivedPosition = arrivedStation.LanePosition;

        // 1. első állomás
        if (tripOriginPosition == null)
        {
            tripOriginPosition = arrivedPosition;

            hasReachedMidpoint = false;
            tripStartTime = Time.time;

            Debug.Log($"[Bus] Trip started at {arrivedStation.gameObject.name}");
        }

        // 2. másik állomás elérése
        else if (
            //!SamePosition(arrivedPosition, tripOriginPosition)
            arrivedPosition != tripOriginPosition
            && !hasReachedMidpoint)
        {
            hasReachedMidpoint = true;

            Debug.Log($"[Bus] Midpoint reached at {arrivedStation.gameObject.name}");
        }

        // 3. visszaérkezés az eredeti állomásra
        else if (
            //SamePosition(arrivedPosition, tripOriginPosition)
            arrivedPosition == tripOriginPosition
            && hasReachedMidpoint)
        {
            float elapsed = Time.time - tripStartTime;

            if (busModel != null)
            {
                busModel.IncreaseTripCount(elapsed);

                Debug.Log(
                    $"[Bus] Full trip complete in {elapsed:F1}s! Total: {busModel.CompletedTrips}"
                );
            }

            hasReachedMidpoint = false;
            tripStartTime = Time.time;
        }
    }

    

    private void FixedUpdate()
    {
        UpdateRemoteVisuals();
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
        NetworkObject netObj = GetComponent<NetworkObject>();

        if (netObj != null && !netObj.IsOwner)
            return;

        //Debug.Log($"UPDATE RUNNING | owner={IsOwner}");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("SPACE DETECTED");
        }
        //Debug.Log("UPDATE RUNNING");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("SPACE DETECTED");
        }
        if (Input.GetKeyDown(KeyCode.Space)
            && currentStation != null)
        {
            Debug.Log(
                $"SPACE pressed | station: {currentStation != null}"
            );
            Debug.Log(
            $"CLIENT CALLING BOARD RPC | bus net id = {NetworkObjectId}"
            );
            //BusStationPassengers passengers = null;
            //    currentStation.StationPassengers;
            BusStationPassengers passengers = currentStation.GetComponent<BusStationPassengers>();

            if (passengers == null)
            {
                Debug.Log("No StationPassengers component!");
                return;
            }

            NetworkObject stationNetObj = null;
            if(!currentStation.TryGetComponent<NetworkObject>(out stationNetObj))
            {
                Debug.Log("Station net object is a nigger hihihiha <- CR7 reference");
                return;
            }


            bool isOtherStation =
                pickupStation != null
                && currentStation != pickupStation;

            // DROPOFF
            if (isOtherStation)
            {
                //RequestDropOffPassengersServerRpc(
                //    stationNetObj.NetworkObjectId
                //);
                passengers.RequestDropOffPassengersServerRpc(
                    NetworkObjectId
                );

                Debug.LogError(
                    $"[Bus] Requested dropoff at {currentStation.gameObject.name}"
                );

                pickupStation = null;
            }

            // PICKUP
            else if (passengersOnBoard.Value == 0)
            {
                Debug.Log(
                        $"CALLING SERVER RPC | objId={stationNetObj.NetworkObjectId}"
                    );
                //RequestBoardPassengersServerRpc(
                //    stationNetObj.NetworkObjectId
                //);
                passengers.RequestBoardPassengersServerRpc(
                 NetworkObjectId
                );

                Debug.LogError(
                    $"[Bus] Requested pickup at {currentStation.gameObject.name}"
                );

                pickupStation = currentStation;
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
    void Start()
    {
        lastPosition = transform.position;

        Debug.Log(
            $"BUS START | obj={gameObject.name} | enabled={enabled}"
        );
    }

    void OnEnable()
    {
        Debug.Log(
            $"BUS ENABLED | obj={gameObject.name}"
        );
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