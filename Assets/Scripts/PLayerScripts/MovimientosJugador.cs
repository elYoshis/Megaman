using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovimientosJugador : MonoBehaviour
{
    public enum Estado { Idle = 1, Caminando = 2, SubiendoEscalera = 3, Saltando = 4, Cayendo = 5, Dash = 6 }

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 8f;      

    [Header("Salto")]
    [SerializeField] private float jumpForce = 12f;     
    [SerializeField] private float gravityScale = 3f;   

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;     
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Escalera")]
    [SerializeField] private float climbSpeed = 5f;
    [SerializeField] private float climbHorizontalSpeed = 3f;
    [SerializeField] private string ladderTag = "Escalera";

    [Header("Deteccion de suelo")]
    [SerializeField] private Transform groundCheck;     
    [SerializeField] private float groundCheckRadius = 0.2f;    
    [SerializeField] private LayerMask groundLayer;    

    [Header("Visual (opcional)")]
    [SerializeField] private Transform visual;

    public bool IsGrounded => isGrounded;
    public bool IsDashing => isDashing;
    public bool IsFacingRight => isFacingRight;
    public float HorizontalSpeed => rb.velocity.x;
    public float VerticalSpeed => rb.velocity.y;
    public float JumpForce => jumpForce;

    public event Action<Estado> EstadoCambiado;

    private Rigidbody rb;
    private float horizontalInput;
    private float verticalInput;
    private bool isGrounded;
    private bool isFacingRight = true;

    bool isDashing;                 
    float dashDirection;            
    float dashTimer;               
    float cooldownTimer;

    private int contactosEscalera;
    private bool estaTocandoEscalera => contactosEscalera > 0;
    private bool estaEnEscalera;
    private float ladderRegrabTimer;

    public Estado _estado=Estado.Idle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;

        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;
        if (ladderRegrabTimer > 0f) ladderRegrabTimer -= Time.deltaTime;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) EndDash();
            return;
        }

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (horizontalInput > 0 && !isFacingRight) Flip();
        if (horizontalInput < 0 && isFacingRight) Flip();

        if (estaTocandoEscalera && !estaEnEscalera && ladderRegrabTimer <= 0f && Mathf.Abs(verticalInput) > 0.1f)
        {
            estaEnEscalera = true;
            rb.velocity = Vector3.zero;
        }

        if (estaEnEscalera && isGrounded && verticalInput < -0.1f)
        {
            estaEnEscalera = false;
        }

        if (Input.GetButtonDown("Jump"))
        {
            if (estaEnEscalera)
            {
                estaEnEscalera = false;
                ladderRegrabTimer = 0.2f;
                Jump();
            }
            else if (isGrounded)
            {
                Jump();
            }
        }
        if (Input.GetKeyDown(KeyCode.LeftShift) && cooldownTimer <= 0f)
        {
            estaEnEscalera = false;
            StartDash();
        }

       
    }

    private void FixedUpdate()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position,groundCheckRadius,groundLayer,QueryTriggerInteraction.Ignore);

        if (isDashing)
        {
            rb.velocity = new Vector3(dashDirection * dashSpeed, 0f, 0f);
            CambiarEstado(Estado.Dash);
            return;
        }

        if (estaEnEscalera)
        {
            SubirEscalera();
            CambiarEstado(Estado.SubiendoEscalera);
            return;
        }

        rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);

        rb.velocity = new Vector3(horizontalInput * moveSpeed, rb.velocity.y, 0f);

        ActualizarEstado();

        
    }

    private void ActualizarEstado()
    {
        if (isGrounded && rb.velocity.y <= 0.1f)
        {
            if (Mathf.Abs(horizontalInput) < 0.01f)
                CambiarEstado(Estado.Idle);
            else
                CambiarEstado(Estado.Caminando);
        }
        else
        {
            if (rb.velocity.y > 0.1f)
                CambiarEstado(Estado.Saltando);
            else
                CambiarEstado(Estado.Cayendo);
        }
    }



    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, 0f);
        CambiarEstado(Estado.Saltando);
    }

    private void StartDash()
    {
        if (!isGrounded) return;
        isDashing = true;
        dashTimer = dashDuration;

        if (horizontalInput != 0) dashDirection = Mathf.Sign(horizontalInput);
        else dashDirection = isFacingRight ? 1f : -1f;

        CambiarEstado(Estado.Dash);
    }

    private void EndDash()
    {
        isDashing = false;
        cooldownTimer = dashCooldown;      
        rb.velocity = Vector3.zero;        
    }

    private void CambiarEstado(Estado nuevoEstado)
    {
        if (_estado == nuevoEstado) return;
        _estado = nuevoEstado;
        EstadoCambiado?.Invoke(_estado);
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;

        Transform target = visual != null ? visual : transform;

        target.localRotation = Quaternion.Euler(0f, isFacingRight ? 0f : -180f, 0f);

        Debug.Log("Flip: mira a la derecha = " + isFacingRight);
    }

    private void SubirEscalera()
    {
        rb.velocity = new Vector3(horizontalInput * climbHorizontalSpeed, verticalInput * climbSpeed, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(ladderTag))
        {
            contactosEscalera++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(ladderTag))
        {
            contactosEscalera = Mathf.Max(0, contactosEscalera - 1);
            if (contactosEscalera == 0)
            {
                estaEnEscalera = false;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

    

}