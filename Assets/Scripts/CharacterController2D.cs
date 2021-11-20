using UnityEngine;
using UnityEngine.Events;

public class CharacterController2D : MonoBehaviour
{
	[SerializeField] private float m_JumpForce = 400f;							// Cantidad de fuerza añadida cuando salta
	[Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;			// Cantidad de velocidad añadida cuando se agacha
	[Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;	// Movimiento mas fluido
	[SerializeField] private bool m_AirControl = false;							// Si se puede mover en el aire
	[SerializeField] private LayerMask m_WhatIsGround;							// Layer que checkea que es piso para el zorrito
	[SerializeField] private Transform m_GroundCheck;							// Posicion que checkea si esta en el piso
	[SerializeField] private Transform m_CeilingCheck;							// Posicion que checkea con el techo
	[SerializeField] private Collider2D m_CrouchDisableCollider;				// Para desactivar el collider al agacharse

	const float k_GroundedRadius = .2f; // Radio del collider que checkea con el piso
	private bool m_Grounded;            // Si esta o no en el piso
	const float k_CeilingRadius = .2f; // Radio que checkea si el zorrito se puede parar o no
	private Rigidbody2D m_Rigidbody2D;
	private bool m_FacingRight = true;  // Siempre empieza mirando a la derecha
	private Vector3 m_Velocity = Vector3.zero;

	[Header("Events")]
	[Space]

	public UnityEvent OnLandEvent;

	[System.Serializable]
	public class BoolEvent : UnityEvent<bool> { }

	public BoolEvent OnCrouchEvent;
	private bool m_wasCrouching = false;

	private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();

		if (OnLandEvent == null)
			OnLandEvent = new UnityEvent();

		if (OnCrouchEvent == null)
			OnCrouchEvent = new BoolEvent();
	}

	private void FixedUpdate()
	{
		bool wasGrounded = m_Grounded;
		m_Grounded = false;

		// Zorrito esta a tierra si el circulo toca cualquier collider
		Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].gameObject != gameObject)
			{
				m_Grounded = true;
				if (!wasGrounded)
					OnLandEvent.Invoke();
			}
		}
	}


	public void Move(float move, bool crouch, bool jump)
	{
		// Si esta agachado checkea si se puede parar
		if (!crouch)
		{
			// Si hay techo impide pararse
			if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
			{
				crouch = true;
			}
		}

		// Solo se puede controlar al zorrito si esta en tierra o si airControl es verdadero
		if (m_Grounded || m_AirControl)
		{

			if (crouch)
			{
				if (!m_wasCrouching)
				{
					m_wasCrouching = true;
					OnCrouchEvent.Invoke(true);
				}

				// Se reduce la velocidad al agacharse
				move *= m_CrouchSpeed;

				// Desactiva un collider al agacharse
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = false;
			} else
			{
				// Activa un collider cuando no esta agachado
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = true;

				if (m_wasCrouching)
				{
					m_wasCrouching = false;
					OnCrouchEvent.Invoke(false);
				}
			}

			// Zorrito se mueve con la velocidad target
			Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
			m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

			// Para mirar donde corresponde
			if (move > 0 && !m_FacingRight)
			{
				Flip();
			}
			else if (move < 0 && m_FacingRight)
			{
				Flip();
			}
		}
		// Si se puede saltar
		if (m_Grounded && jump)
		{
			m_Grounded = false;
			m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
		}
	}

	// Dar vuelta al zorrito multiplicando por -1 la posicion en x
	private void Flip()
	{
		m_FacingRight = !m_FacingRight;
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}
