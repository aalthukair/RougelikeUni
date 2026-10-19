using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    [SerializeField, Range(1f, 20f), Tooltip("Cells per second the player travels when moving between cells.")]
    private float m_MoveSpeed = 8f;

    [SerializeField, Tooltip("Key that starts a new game after a game over. Enter also works.")]
    private Key m_RestartKey = Key.R;

    private BoardManager m_Board;
    private Vector2Int m_CellPosition;
    private bool m_IsGameOver;
    private bool m_IsMoving;
    private Coroutine m_MoveRoutine;

    private Animator m_Animator;

    public Vector2Int CellPosition => m_CellPosition;

    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
    }
    
    public void PlayHitAnimation()
    {
        m_Animator.SetTrigger("Hit");
    }

    public void Init()
    {
        m_IsGameOver = false;
    }

    public void Spawn(BoardManager boardManager, Vector2Int cell)
    {
        m_Board = boardManager;
        MoveTo(cell, true);
    }

    public void GameOver()
    {
        m_IsGameOver = true;
    }
    
    public void MoveTo(Vector2Int cell, bool immediate)
    {
        m_CellPosition = cell;
        Vector3 target = m_Board.CellToWorld(m_CellPosition);

        if (m_MoveRoutine != null)
        {
            StopCoroutine(m_MoveRoutine);
            m_MoveRoutine = null;
        }

        if (immediate)
        {
            transform.position = target;
            SetMoving(false);
        }
        else
        {
            m_MoveRoutine = StartCoroutine(MoveRoutine(target));
        }
    }

    private IEnumerator MoveRoutine(Vector3 target)
    {
        SetMoving(true);

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
        SetMoving(false);

        if (!m_IsGameOver)
        {
            var cellData = m_Board.GetCellData(m_CellPosition);
            if (cellData.ContainedObject != null)
                cellData.ContainedObject.PlayerEntered();
        }
    }

    private void SetMoving(bool isMoving)
    {
        m_IsMoving = isMoving;
        m_Animator.SetBool("Moving", isMoving);
    }

    private void Update()
    {
        if (m_IsGameOver)
        {
            if (WasRestartPressed())
            {
                GameManager.Instance.StartNewGame();
            }
            return;
        }
        
        if (m_IsMoving)
        {
            return;
        }

        if (!TryReadDirection(out Vector2Int direction))
        {
            return;
        }

        Vector2Int newCellTarget = m_CellPosition + direction;
        BoardManager.CellData cellData = m_Board.GetCellData(newCellTarget);

        if (cellData == null || !cellData.Passable)
        {
            return;
        }
        
        if (cellData.ContainedObject != null && cellData.ContainedObject.IsAttackable)
        {
            m_Animator.SetTrigger("Attack");
        }

        GameManager.Instance.TurnManager.Tick();
        
        if (m_IsGameOver)
        {
            return;
        }

        if (cellData.ContainedObject == null || cellData.ContainedObject.PlayerWantsToEnter())
        {
            MoveTo(newCellTarget, false);
            GameManager.Instance.SoundManager.PlayFootstep();
        }
    }

    private bool WasRestartPressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        bool restartKeyPressed = m_RestartKey != Key.None && keyboard[m_RestartKey].wasPressedThisFrame;
        return restartKeyPressed || keyboard.enterKey.wasPressedThisFrame;
    }

    private static bool TryReadDirection(out Vector2Int direction)
    {
        Keyboard keyboard = Keyboard.current;
        direction = Vector2Int.zero;

        if (keyboard == null)
        {
            return false;
        }

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
        {
            direction = Vector2Int.up;
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
        {
            direction = Vector2Int.down;
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
        {
            direction = Vector2Int.right;
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
        {
            direction = Vector2Int.left;
        }

        return direction != Vector2Int.zero;
    }
}
