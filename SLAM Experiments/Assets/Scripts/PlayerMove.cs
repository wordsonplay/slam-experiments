using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float speed = 5;  // m/s
    [SerializeField] private float turnSpeed = 60; // deg/s

    private Gamepad gamepad;
    private new Rigidbody2D rigidbody;

    void Start()
    {
        gamepad = Gamepad.current;
        rigidbody = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        Vector2 move = gamepad.leftStick.ReadValue();

        rigidbody.velocity = transform.up * speed * move.y;
        rigidbody.angularVelocity = turnSpeed * -move.x; 
    }
}
