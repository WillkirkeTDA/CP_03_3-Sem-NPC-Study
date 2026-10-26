using System.Collections;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    [Header("Enemy Attributes")]
    [SerializeField] private GameObject player;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float distanceWakeUp = 4f;
    [SerializeField] private float distanceAttack = 1f;
    [SerializeField] private int life = 3;

    private enum EnemyState
    {
        Idle,
        Chase,
        Attack,
        Return,
        Dead
    }

    private EnemyState state = EnemyState.Idle;

    private Vector3 initialPosition;

    private bool attacking;
    private bool invulnerable;

    private Animator animator;
    private SpriteRenderer spriteRender;
    private Collider2D enemyCollider;
    private Coroutine attackCoroutine;

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRender = GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();

        // Guarda a posição inicial do inimigo.
        initialPosition = transform.position;
    }

    void Start()
    {
        // Procura o jogador automaticamente.
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    void Update()
    {
        // Não faz nada se o jogador não existir ou o inimigo morrer.
        if (player == null || state == EnemyState.Dead)
        {
            UpdateAnimations();
            return;
        }

        // Area de detecção.
        float distanceFromStart = Vector2.Distance(
         transform.position,
         player.transform.position);

        // Calcula a distância atual entre o inimigo e o jogador.
        float distanceToPlayer = Vector2.Distance(
            transform.position,
            player.transform.position
        );

        // Verifica se o jogador está dentro da área de detecção.
        bool playerDetected = distanceFromStart <= distanceWakeUp;

        if (playerDetected)
        {
            // Se o jogador estiver perto, o inimigo ataca.
            if (distanceToPlayer <= distanceAttack)
            {
                state = EnemyState.Attack;

                FaceTarget(player.transform.position);
                Attack();
            }
            else
            {
                // Se estava atacando, interrompe o ataque.
                StopAttack();

                // Persegue o jogador enquanto ele estiver na área.
                state = EnemyState.Chase;
                Move(player.transform.position);
            }
        }
        else
        {
            // O jogador saiu da área: interrompe o ataque.
            StopAttack();

            // Verifica se o inimigo ainda precisa voltar para casa.
            float distanceToStart = Vector2.Distance(
                transform.position,
                initialPosition
            );

            if (distanceToStart > 0.05f)
            {
                // Volta para a posição inicial.
                state = EnemyState.Return;
                Move(initialPosition);
            }
            else
            {
                // Chegou em casa: fica parado esperando o jogador.
                transform.position = initialPosition;
                state = EnemyState.Idle;
            }
        }

        UpdateAnimations();
    }

    void Move(Vector3 target)
    {
        // Move o inimigo suavemente até o destino.
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );

        FaceTarget(target);
    }

    void FaceTarget(Vector3 target)
    {
        // Vira o sprite na direção do alvo.
        if (spriteRender != null &&
            Mathf.Abs(target.x - transform.position.x) > 0.01f)
        {
            spriteRender.flipX = target.x > transform.position.x;
        }
    }

    void Attack()
    {
        // Evita iniciar vários ataques ao mesmo tempo.
        if (!attacking)
        {
            attackCoroutine = StartCoroutine(Attacking());
        }
    }

    IEnumerator Attacking()
    {
        attacking = true;

        // Espera antes de causar dano.
        yield return new WaitForSeconds(0.5f);

        // Só causa dano se o jogador ainda estiver perto.
        if (player != null && state == EnemyState.Attack)
        {
            float distance = Vector2.Distance(
                transform.position,
                player.transform.position
            );

            if (distance <= distanceAttack)
            {
                PlayerControls playerControls =
                    player.GetComponent<PlayerControls>();

                if (playerControls != null)
                {
                    playerControls.TakeDamage(1);
                }
            }
        }

        // Espera antes de permitir outro ataque.
        yield return new WaitForSeconds(1f);

        attacking = false;
        attackCoroutine = null;
    }

    void StopAttack()
    {
        // Cancela o ataque atual, se existir.
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        attacking = false;
    }

    void UpdateAnimations()
    {
        if (animator == null)
        {
            return;
        }

        // Ativa a animação de movimento durante a perseguição e o retorno.
        animator.SetBool(
            "pWakeUp",
            state == EnemyState.Chase ||
            state == EnemyState.Return
        );

        animator.SetBool("pAttack", attacking);
        animator.SetBool("pDeath", state == EnemyState.Dead);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Não recebe dano se já morreu ou está invulnerável.
        if (state == EnemyState.Dead || invulnerable)
        {
            return;
        }

        if (other.CompareTag("Punch"))
        {
            invulnerable = true;
            StartCoroutine(Damage());
        }
    }

    IEnumerator Damage()
    {
        life--;

        Debug.Log("Vida do inimigo: " + life);

        if (life <= 0)
        {
            state = EnemyState.Dead;

            StopAttack();

            // Desativa o colisor ao morrer.
            if (enemyCollider != null)
            {
                enemyCollider.enabled = false;
            }

            if (animator != null)
            {
                animator.SetBool("pDeath", true);
            }

            // Espera a animação de morte.
            yield return new WaitForSeconds(1f);

            Destroy(gameObject);
        }
        else
        {
            // Fica vermelho quando recebe dano.
            if (spriteRender != null)
            {
                spriteRender.color = Color.red;
            }

            yield return new WaitForSeconds(0.25f);

            if (spriteRender != null)
            {
                spriteRender.color = Color.white;
            }

            invulnerable = false;
        }
    }

    private void OnDrawGizmos()
    { // Cor da área de detecção.
        Gizmos.color = Color.yellow;
        // Desenha a área ao redor da posição atual do inimigo.
        Gizmos.DrawWireSphere(transform.position, distanceWakeUp);
    }


}