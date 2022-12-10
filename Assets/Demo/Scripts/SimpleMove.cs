using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class SimpleMove : MonoBehaviour
{
    [SerializeField] private Transform refCam;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool useFixedMove;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float jumpHeight;

    public IObservable<Unit> OnJump => onJump;
    private Subject<Unit> onJump = new();

    private float JumpVelocity => Mathf.Sqrt(2f * Physics.gravity.magnitude * jumpHeight);

    private Vector2 inputs;

    private void Start()
    {
        Application.targetFrameRate = 60;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) {
            onJump.OnNext(Unit.Default);
        }

        inputs = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (useFixedMove) return;

        Move();
        TryJump();
    }

    private void FixedUpdate()
    {
        if (!useFixedMove) {
            rb.velocity = rb.velocity.WithXZ(0f, 0f);
            return;
        }
        FixedMove();
        TryJump();
    }

    private Vector3 CalculateMoveVector()
    {
        return (refCam.right * inputs.x + refCam.forward * inputs.y).WithY(0f) * moveSpeed;
    }

    private void Move()
    {
        transform.position += CalculateMoveVector() * Time.deltaTime;
    }

    private void FixedMove()
    {
        rb.velocity = CalculateMoveVector().WithY(rb.velocity.y);
    }

    private void TryJump()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        rb.velocity = rb.velocity.WithY(JumpVelocity);
    }
}
