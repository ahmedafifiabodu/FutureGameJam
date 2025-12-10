using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
[Header("Movement")]
public float moveSpeed;

public float jumpForce;
public float jumpCooldown;
public float airMultiplier;
bool readyToJump = true;


[Header("Keybinds")]
public KeyCode jumpKey = KeyCode.Space;

[Header("Ground Check")]
public bool grounded;
public float groundDrag = 2.0f;
List<Collision> groundCollisions = new List<Collision>{};

[Header("Jump Gravity")]
public float fallMultiplier = 2.0f;
public float lowJumpMultiplier = 1.5f;

public Transform orientation;

float horizontalInput;
float verticalInput;

Vector3 moveDirection;

Rigidbody rb;

void Start()
{
    rb = GetComponent<Rigidbody>();
    rb.freezeRotation = true;
}

void FixedUpdate()
{
    MovePlayer();

    if(!grounded)
    {
        if(rb.linearVelocity.y > 0.01f)
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * (fallMultiplier - 1f), ForceMode.Acceleration);
        }
        else if (rb.linearVelocity.y < 0.01f)
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * (fallMultiplier - 1f), ForceMode.Acceleration);
        }
    }
    grounded = false;
}

void Update()
{
    grounded = IsGrounded();
    MyInput();
    SpeedControl();

    if(grounded)
    {
        rb.linearDamping = groundDrag;
    }
    else
    {
        rb.linearDamping = 0;
    }
}

private void MyInput()
{
    horizontalInput = Input.GetAxisRaw("Horizontal");
    verticalInput = Input.GetAxisRaw("Vertical");
    
    if(Input.GetKey(jumpKey) && readyToJump && grounded)
    {
        readyToJump = false;

        Jump();

        Invoke(nameof(ResetJump), jumpCooldown);
    }
}

private void MovePlayer()
{
    moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
    if(grounded)
    {
        rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
    }
    else if(!grounded)
    {
        rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
    }

}

private void OnCollisionEnter(Collision collision)
{
    groundCollisions.Add(collision);
}

private void OnCollisionStay(Collision collision)
{
    if (!groundCollisions.Contains(collision))
        groundCollisions.Add(collision);
}

private void OnCollisionExit(Collision collision)
{
    groundCollisions.Remove(collision);
}

public bool IsGrounded()
{
    foreach (Collision collision in groundCollisions)
    {
        if (collision.contactCount == 0) continue;
        float upDot = Vector3.Dot(collision.GetContact(0).normal, Vector3.up);
        if (upDot <= -0.5f)
        {
            return true;
        }
    }
    return false;
}

private void SpeedControl()
{
    Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

    if(flatVel.magnitude > moveSpeed)
    {
        Vector3 limitedVel = flatVel.normalized* moveSpeed;
        rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
    }
}

private void Jump()
{
    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

    rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
}

private void ResetJump()
{
    readyToJump = true;
}

}
