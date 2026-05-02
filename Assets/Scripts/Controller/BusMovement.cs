using UnityEngine;
using SnowPlow.Controller.Pathfinding;
using SnowPlow.Model.Map;

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
    public Vector2 horizontalSize = new Vector2(6f, 3f);
    public Vector2 verticalSize = new Vector2(3f, 6f);

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
    private const float StunDuration = 5f;
    private bool isOnIce = false;
    private const float IceFriction = 0.995f;    // how little it slows down (closer to 1 = more slippery)
    private const float IceControlMultiplier = 0.15f; // how much control the player has on ice

    public void SetStations(VisualSegment a, VisualSegment b)
    {
        stationA = a;
        stationB = b;
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Bus] Trigger entered: {other.gameObject.name} tag={other.gameObject.tag}");

        if (other.CompareTag("Road")) touchingRoads++;

        // Vehicle collision check — no cooldown needed, stun handles it
        if (other.CompareTag("Vehicle"))
        {
            Debug.Log("[Bus] Vehicle hit! Starting stun.");
            stunTimer = StunDuration;
            myRigidBody2D.linearVelocity = Vector2.zero;
            return;
        }

        if (blockCooldown > 0f) return;

        VisualSegment vs = other.GetComponent<VisualSegment>();
        if (vs != null && vs.LanePosition != null)
        {

            // Ice check
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

        // Station detection
        if (vs != null)
        {
            if (vs == stationA || vs == stationB)
            {
                currentStation = vs;
                Debug.Log($"[Bus] Arrived at station: {vs.gameObject.name}");
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Road")) touchingRoads--;

        // Clear block when fully leaving the blocked segment
        VisualSegment vs = other.GetComponent<VisualSegment>();
        if (vs != null && vs.LanePosition != null)
        {
            if (!traversalPolicy.CanEnterSegment(vs.LanePosition))
            {
                isBlocked = false;
            }

            // Clear ice when leaving the segment
            if (vs.LanePosition.Lane[vs.LanePosition.SegmentIndex].HasIce)
            {
                isOnIce = false;
            }
        }

        if (vs != null && vs == currentStation)
        {
            currentStation = null;
        }
    }

    private void FixedUpdate()
    {
        if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
            myRigidBody2D.linearVelocity = Vector2.zero;
            return;
        }

        if (blockCooldown > 0f)
            blockCooldown -= Time.fixedDeltaTime;

        if (touchingRoads <= 0)
        {
            myRigidBody2D.linearVelocity = -myRigidBody2D.linearVelocity * 1.5f;
            return;
        }

        if (isBlocked) return;

        // On ice: reduced control, velocity barely decays
        float controlMultiplier = isOnIce ? IceControlMultiplier : 1f;

        if (Input.GetKey(KeyCode.W))
            myRigidBody2D.linearVelocity += Vector2.up * speedMultiplyer * controlMultiplier * Time.fixedDeltaTime;

        if (Input.GetKey(KeyCode.S))
            myRigidBody2D.linearVelocity += Vector2.down * speedMultiplyer * controlMultiplier * Time.fixedDeltaTime;

        if (Input.GetKey(KeyCode.A))
            myRigidBody2D.linearVelocity += Vector2.left * speedMultiplyer * controlMultiplier * Time.fixedDeltaTime;

        if (Input.GetKey(KeyCode.D))
            myRigidBody2D.linearVelocity += Vector2.right * speedMultiplyer * controlMultiplier * Time.fixedDeltaTime;

        // On ice: preserve momentum; on road: let Unity's drag handle it naturally
        if (isOnIce)
            myRigidBody2D.linearVelocity *= IceFriction;

        if (myRigidBody2D.linearVelocity.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(myRigidBody2D.linearVelocity.y, myRigidBody2D.linearVelocity.x) * Mathf.Rad2Deg;
            UpdateSprite(angle);
        }

        // Station interaction
        if (Input.GetKeyDown(KeyCode.Space) && currentStation != null && myRigidBody2D.linearVelocity.magnitude < 0.1f)
        {
            BusStationPassengers passengers = currentStation.StationPassengers;
            if (passengers == null) return;

            bool isOtherStation = pickupStation != null && currentStation != pickupStation;

            if (isOtherStation && passengersOnBoard > 0)
            {
                // Drop off at the other station
                passengers.DropOffPassengers(passengersOnBoard);
                Debug.Log($"[Bus] Dropped off {passengersOnBoard} passengers at {currentStation.gameObject.name}.");
                passengersOnBoard = 0;
                pickupStation = null;
            }
            else if (!isOtherStation || passengersOnBoard == 0)
            {
                // Pick up — can press Space multiple times to keep picking up
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

        bool isSideView = false;

        if (angle >= 337.5f || angle < 22.5f) { spriteRenderer.sprite = iso_Right; isSideView = true; }
        else if (angle >= 22.5f && angle < 67.5f) { spriteRenderer.sprite = iso_UpRight; isSideView = false; }
        else if (angle >= 67.5f && angle < 112.5f) { spriteRenderer.sprite = iso_Up; isSideView = false; }
        else if (angle >= 112.5f && angle < 157.5f) { spriteRenderer.sprite = iso_UpLeft; isSideView = false; }
        else if (angle >= 157.5f && angle < 202.5f) { spriteRenderer.sprite = iso_Left; isSideView = true; }
        else if (angle >= 202.5f && angle < 247.5f) { spriteRenderer.sprite = iso_DownLeft; isSideView = false; }
        else if (angle >= 247.5f && angle < 292.5f) { spriteRenderer.sprite = iso_Down; isSideView = false; }
        else if (angle >= 292.5f && angle < 337.5f) { spriteRenderer.sprite = iso_DownRight; isSideView = false; }

        if (boxCollider != null)
            boxCollider.size = isSideView ? horizontalSize : verticalSize;
    }
}