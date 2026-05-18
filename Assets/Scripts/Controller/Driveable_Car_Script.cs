using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Driveable_Car_Script : NetworkBehaviour
{
    public Rigidbody2D myRigidBody2D;
    public float speedMultiplyer;

    // Ebbe számoljuk, hány útelemhez érünk éppen hozzá
    private int touchingRoads = 0;

    // Amikor az autó ráhajt egy útra vagy körforgalomra
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Road"))
        {
            touchingRoads++;
        }
    }

    // Amikor az autó elhagy egy utat
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Road"))
        {
            touchingRoads--;
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;
        // HA LEMENTÜNK AZ ÚTRÓL (a kék felületre értünk)
        if (touchingRoads <= 0)
        {
            // Azonnal visszapattintjuk az autót az útra (irányt váltunk)
            // A 1.5-ös szorzó ad egy kis "rugós" lökést visszafelé
            myRigidBody2D.linearVelocity = -myRigidBody2D.linearVelocity * 1.5f;
            return; // Itt kilépünk, nem engedjük, hogy a játékos gázt adjon a kék felületen!
        }

        // --- INNENTŐL A TE EREDETI MOZGÁS KÓDOD JÖN ---

        if (Input.GetKey(KeyCode.W))
        {
            myRigidBody2D.linearVelocity += Vector2.up * speedMultiplyer * Time.fixedDeltaTime;
        }

        if (Input.GetKey(KeyCode.S))
        {
            myRigidBody2D.linearVelocity += Vector2.down * speedMultiplyer * Time.fixedDeltaTime;
        }

        if (Input.GetKey(KeyCode.A))
        {
            myRigidBody2D.linearVelocity += Vector2.left * speedMultiplyer * Time.fixedDeltaTime;
        }

        if (Input.GetKey(KeyCode.D))
        {
            myRigidBody2D.linearVelocity += Vector2.right * speedMultiplyer * Time.fixedDeltaTime;
        }

        if (myRigidBody2D.linearVelocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(myRigidBody2D.linearVelocity.y, myRigidBody2D.linearVelocity.x) * Mathf.Rad2Deg;
            myRigidBody2D.rotation = angle - 90f;
        }
    }
}