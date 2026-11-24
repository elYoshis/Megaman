using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MovimientosJugador))]

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Ajustes de Motion Time")]
    [Tooltip("Velocidad del _ciclo al caminar")]
    [SerializeField] private float factorCicloCaminar = 0.2f;

    [Tooltip("Velocidad del _ciclo al subir o bajar la escalera")]
    [SerializeField] private float factorCicloEscalera = 0.25f;

    [Tooltip("Velocidad vertical negativa a la que la animación de caída llega al 100%")]
    [SerializeField] private float velocidadCaidaMax = 15f;

    private MovimientosJugador player;

    private static readonly int EstadoHash = Animator.StringToHash("Estado");
    private static readonly int CicloHash = Animator.StringToHash("Ciclo");

    float _ciclo;

    private void Awake()
    {
        player = GetComponent<MovimientosJugador>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        ActualizarAnimacion(player._estado);
    }

    private void OnEnable()
    {
        player.EstadoCambiado += ActualizarAnimacion;
    }

    private void OnDisable()
    {
        player.EstadoCambiado -= ActualizarAnimacion;
    }

    private void Update()
    {
        switch (player._estado)
        {
            case MovimientosJugador.Estado.Caminando:
                _ciclo += Mathf.Abs(player.HorizontalSpeed) * factorCicloCaminar * Time.deltaTime;
                _ciclo = Mathf.Repeat(_ciclo, 1f);
                animator.SetFloat(CicloHash, _ciclo);
                break;

            case MovimientosJugador.Estado.SubiendoEscalera:
                // Positivo al subir, negativo al bajar (reversa), 0 si está quieto en la escalera
                _ciclo += player.VerticalSpeed * factorCicloEscalera * Time.deltaTime;
                _ciclo = Mathf.Repeat(_ciclo, 1f);
                animator.SetFloat(CicloHash, _ciclo);
                break;

            case MovimientosJugador.Estado.Saltando:
                // 0.0 al despegar (JumpForce) -> 0.99 en el punto más alto (0)
                float progresoSalto = Mathf.InverseLerp(player.JumpForce, 0f, player.VerticalSpeed);
                _ciclo = Mathf.Clamp(progresoSalto, 0f, 0.99f);
                animator.SetFloat(CicloHash, _ciclo);
                break;

            case MovimientosJugador.Estado.Cayendo:
                // 0.0 al empezar a caer (0) -> 0.99 en caída rápida (-velocidadCaidaMax)
                float progresoCaida = Mathf.InverseLerp(0f, -velocidadCaidaMax, player.VerticalSpeed);
                _ciclo = Mathf.Clamp(progresoCaida, 0f, 0.99f);
                animator.SetFloat(CicloHash, _ciclo);
                break;
        }
    }

    private void ActualizarAnimacion(MovimientosJugador.Estado nuevoEstado)
    {
        animator.SetInteger(EstadoHash, (int)nuevoEstado);

        if (nuevoEstado == MovimientosJugador.Estado.Idle)
        {
            _ciclo = 0f;
            animator.SetFloat(CicloHash, _ciclo);
        }
    }

}
