using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Enemy : CellObject
{
    private static readonly int s_MovingHash = Animator.StringToHash("Moving");
    private static readonly int s_AttackHash = Animator.StringToHash("Attack");

    [SerializeField, FormerlySerializedAs("Health")]
    [Range(1, 10), Tooltip("Number of hits the player needs to land before this enemy dies.")]
    private int m_MaxHealth = 3;

    [SerializeField]
    [Range(1, 20), Tooltip("Food the player loses each time this enemy attacks.")]
    private int m_Damage = 3;

    [SerializeField]
    [Range(1f, 20f), Tooltip("Cells per second the enemy travels when it moves.")]
    private float m_MoveSpeed = 8f;

    private int m_CurrentHealth;
    private Animator m_Animator;
    private Coroutine m_MoveRoutine;

    public override bool IsAttackable => true;

    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
    }
    
    private void OnEnable()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null && gameManager.TurnManager != null)
        {
            gameManager.TurnManager.OnTick += TurnHappened;
        }
    }

    private void OnDisable()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null && gameManager.TurnManager != null)
        {
            gameManager.TurnManager.OnTick -= TurnHappened;
        }
        
        m_MoveRoutine = null;
    }

    public override void Init(Vector2Int coord)
    {
        base.Init(coord);
        m_CurrentHealth = m_MaxHealth;
    }
    
    public override bool PlayerWantsToEnter()
    {
        GameManager gameManager = GameManager.Instance;
        m_CurrentHealth -= 1;

        if (m_CurrentHealth > 0)
        {
            gameManager.SoundManager.PlayEnemyHit();
            return false;
        }

        gameManager.EffectsManager.PlayEnemyDeath(transform.position);
        gameManager.SoundManager.PlayEnemyDeath();
        gameManager.BoardManager.RemoveObject(this);

        return false;
    }

    bool MoveTo(Vector2Int coord)
    {
        var board = GameManager.Instance.BoardManager;
        var targetCell = board.GetCellData(coord);

        if (targetCell == null
            || !targetCell.Passable
            || targetCell.ContainedObject != null)
        {
            return false;
        }

        var currentCell = board.GetCellData(m_Cell);
        currentCell.ContainedObject = null;

        targetCell.ContainedObject = this;
        m_Cell = coord;

        if (m_MoveRoutine != null)
        {
            StopCoroutine(m_MoveRoutine);
        }
        m_MoveRoutine = StartCoroutine(MoveRoutine(board.CellToWorld(coord)));

        return true;
    }
    
    IEnumerator MoveRoutine(Vector3 target)
    {
        m_Animator.SetBool(s_MovingHash, true);

        Vector3 start = transform.position;
        float duration = 1f / m_MoveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        transform.position = target;
        m_MoveRoutine = null;
        m_Animator.SetBool(s_MovingHash, false);
    }

    void TurnHappened()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager.IsGameOver)
        {
            return;
        }

        var playerCell = gameManager.PlayerController.CellPosition;

        int xDist = playerCell.x - m_Cell.x;
        int yDist = playerCell.y - m_Cell.y;

        int absXDist = Mathf.Abs(xDist);
        int absYDist = Mathf.Abs(yDist);

        if ((xDist == 0 && absYDist == 1)
            || (yDist == 0 && absXDist == 1))
        {
            gameManager.SoundManager.PlayEnemyAttack();
            m_Animator.SetTrigger(s_AttackHash);
            gameManager.PlayerController.PlayHitAnimation();
            gameManager.ChangeFood(-m_Damage);
        }
        else
        {
            if (absXDist > absYDist)
            {
                if (!TryMoveInX(xDist))
                {
                    TryMoveInY(yDist);
                }
            }
            else
            {
                if (!TryMoveInY(yDist))
                {
                    TryMoveInX(xDist);
                }
            }
        }
    }

    bool TryMoveInX(int xDist)
    {
        if (xDist > 0)
        {
            return MoveTo(m_Cell + Vector2Int.right);
        }

        return MoveTo(m_Cell + Vector2Int.left);
    }

    bool TryMoveInY(int yDist)
    {
        if (yDist > 0)
        {
            return MoveTo(m_Cell + Vector2Int.up);
        }

        return MoveTo(m_Cell + Vector2Int.down);
    }
}
