using UnityEngine;
using UnityEngine.InputSystem;
public class Driveable_Car_Script : MonoBehaviour
{
    public Rigidbody2D myRigidBody2D;
    public float speedMultiplyer;
   

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        


    }

    

    private void FixedUpdate()
    {

        
        if (Input.GetKey(KeyCode.W))
        {
            myRigidBody2D.linearVelocity += Vector2.up * speedMultiplyer*Time.fixedDeltaTime;
        }

        if (Input.GetKey(KeyCode.S))
        {
            myRigidBody2D.linearVelocity += Vector2.down * speedMultiplyer* Time.fixedDeltaTime;
        }

        if (Input.GetKey(KeyCode.A))
        {
            myRigidBody2D.linearVelocity += Vector2.left * speedMultiplyer* Time.fixedDeltaTime;
        }

        if (Input.GetKey(KeyCode.D))
        {
            myRigidBody2D.linearVelocity += Vector2.right * speedMultiplyer* Time.fixedDeltaTime;
        }
        
        if (myRigidBody2D.linearVelocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(myRigidBody2D.linearVelocityY, myRigidBody2D.linearVelocityX) * Mathf.Rad2Deg;
            myRigidBody2D.rotation = angle;
        }

    }
}
