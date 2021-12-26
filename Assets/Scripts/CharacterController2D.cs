using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CharacterController2D : MonoBehaviour
{
	[SerializeField] private float m_JumpForce = 400f;							// Cantidad de fuerza añadida cuando salta
	[Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;			// Cantidad de velocidad añadida cuando se agacha
	[Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;	// Movimiento mas fluido
	[SerializeField] private bool m_AirControl = false;							// Si se puede mover en el aire
	[SerializeField] private LayerMask m_WhatIsGround;							// Layer que checkea que es piso para el zorrito
	[SerializeField] private Transform m_GroundCheck;							// Posicion que checkea si esta en el piso
	[SerializeField] private Transform m_CeilingCheck;							// Posicion que checkea con el techo
	[SerializeField] private Collider2D m_CrouchDisableCollider;                // Para desactivar el collider al agacharse

	[SerializeField] private GameObject jumpSound;
	[SerializeField] private AudioSource footStep;
	[SerializeField] private AudioSource crouch;
	[SerializeField] private AudioSource hurt;

	[SerializeField] private Text collectablesText;


	const float k_GroundedRadius = .2f; // Radio del collider que checkea con el piso
	private bool m_Grounded;            // Si esta o no en el piso
	const float k_CeilingRadius = .2f; // Radio que checkea si el zorrito se puede parar o no
	private Rigidbody2D m_Rigidbody2D;
	private PlayerMovement playerMovement;
	private Animator animator;
	private bool m_FacingRight = true;  // Siempre empieza mirando a la derecha
	private Vector3 m_Velocity = Vector3.zero;
	private float collectables = 0f; // Contador de recolectables

	[Header("Events")]
	[Space]

	public UnityEvent OnLandEvent;

	[System.Serializable]
	public class BoolEvent : UnityEvent<bool> { }

	public BoolEvent OnCrouchEvent;
	private bool m_wasCrouching = false; // Flag de si se agacha o no

    private void Start()
    {
		
	}
    private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();
		animator = GetComponent<Animator>();

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

    private void OnTriggerEnter2D(Collider2D collision) // Se activa al chocar un collider con otro con un on trigger
    {
		Animator aniCollectable = collision.GetComponent<Animator>();
		if (collision.tag == "Collectable")
        {
			aniCollectable.SetBool("IsCollected", true);
			Destroy(collision.gameObject,0.26f);
			if (m_wasCrouching)
            {
				collectables++;
				collectablesText.text = collectables.ToString();
			}
			else
            {
				collectables++;
				collectables -= 0.5f;
				collectablesText.text = collectables.ToString();
			}
		}
        else
        {
			aniCollectable.SetBool("IsCollected", false);
		}
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if(other.gameObject.tag == "Enemy")
        {
			if(m_Rigidbody2D.velocity.y < -1)
            {
				Destroy(other.gameObject);
				m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce+100));
            }
            else
            {	
				if(other.gameObject.transform.position.x < transform.position.x)
                {
					m_Rigidbody2D.AddForce(new Vector2(600f, 0f)); //si esta a mi derecha aplico una fuerza hacia mi izquierda
					animator.SetBool("IsHurt", true);
				}
                else
                {
					m_Rigidbody2D.AddForce(new Vector2(-600f, 0f)); //si esta a mi izquierda aplico una fuerza hacia mi derecha
					animator.SetBool("IsHurt", true);
				}
            }
			
        }
        else
        {
			animator.SetBool("IsHurt", false);
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
			} 
			else
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
			Instantiate(jumpSound);
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

	// Evento de sonido para caminar linkeado a la animacion
	private void FootStep()
    {
		footStep.Play();
    }
	// Evento de sonido para agacharse linkeado a la animacion
	public void Crouch()
	{
		crouch.Play();
	}

	// Evento de sonido cuando el player es herido
	public void Hurt()
    {
		hurt.Play();
    }
}
