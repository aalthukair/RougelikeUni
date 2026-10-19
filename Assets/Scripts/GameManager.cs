using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene References")]
    [SerializeField, FormerlySerializedAs("BoardManager")]
    private BoardManager m_BoardManager;

    [SerializeField, FormerlySerializedAs("PlayerController")]
    private PlayerController m_PlayerController;

    [SerializeField, FormerlySerializedAs("UIDoc")]
    private UIDocument m_UIDoc;

    [SerializeField, Tooltip("Plays sound effects. Found automatically in the scene if left empty.")]
    private SoundManager m_SoundManager;

    [SerializeField, Tooltip("Plays pooled particle effects. Found automatically in the scene if left empty.")]
    private EffectsManager m_EffectsManager;

    [Header("Food")]
    [SerializeField, Range(1, 200), Tooltip("Food the player starts each new game with.")]
    private int m_StartingFood = 20;

    [SerializeField, Range(1, 10), Tooltip("Food spent every turn.")]
    private int m_FoodCostPerTurn = 1;

    private int m_FoodAmount;
    private int m_CurrentLevel = 1;
    private bool m_IsGameOver;

    private Label m_FoodLabel;
    private VisualElement m_GameOverPanel;
    private Label m_GameOverMessage;

    public BoardManager BoardManager => m_BoardManager;
    public PlayerController PlayerController => m_PlayerController;
    public SoundManager SoundManager => m_SoundManager;
    public EffectsManager EffectsManager => m_EffectsManager;
    public TurnManager TurnManager { get; private set;}
    public bool IsGameOver => m_IsGameOver;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        TurnManager = new TurnManager();
        TurnManager.OnTick += OnTurnHappen;

        if (m_SoundManager == null)
        {
            m_SoundManager = FindAnyObjectByType<SoundManager>();
        }

        if (m_EffectsManager == null)
        {
            m_EffectsManager = FindAnyObjectByType<EffectsManager>();
        }

        if (m_SoundManager == null || m_EffectsManager == null)
        {
            Debug.LogError("GameManager needs a SoundManager and an EffectsManager in the scene.", this);
        }
    }

    void Start()
    {
        m_FoodLabel = m_UIDoc.rootVisualElement.Q<Label>("FoodLabel");

        m_GameOverPanel = m_UIDoc.rootVisualElement.Q<VisualElement>("GameOverPanel");
        m_GameOverMessage = m_GameOverPanel.Q<Label>("GameOverMessage");

        StartNewGame();
    }

    public void StartNewGame()
    {
        m_GameOverPanel.style.visibility = Visibility.Hidden;
        m_IsGameOver = false;

        m_CurrentLevel = 1;
        m_FoodAmount = m_StartingFood;
        UpdateFoodLabel();

        m_BoardManager.Clean();
        m_BoardManager.Init();

        m_PlayerController.Init();
        m_PlayerController.Spawn(m_BoardManager, m_BoardManager.PlayerStartCell);
    }

    public void NewLevel()
    {
        m_BoardManager.Clean();
        m_BoardManager.Init();
        m_PlayerController.Spawn(m_BoardManager, m_BoardManager.PlayerStartCell);

        m_CurrentLevel++;
    }

    void OnTurnHappen()
    {
        ChangeFood(-m_FoodCostPerTurn);
    }

    public void ChangeFood(int amount)
    {
        if (m_IsGameOver)
        {
            return;
        }

        m_FoodAmount += amount;
        UpdateFoodLabel();

        if (m_FoodAmount <= 0)
        {
            GameOver();
        }
    }

    void UpdateFoodLabel()
    {
        m_FoodLabel.text = "Food : " + Mathf.Max(0, m_FoodAmount);
    }

    void GameOver()
    {
        m_IsGameOver = true;
        m_PlayerController.GameOver();
        m_SoundManager.PlayGameOver();

        m_GameOverPanel.style.visibility = Visibility.Visible;
        m_GameOverMessage.text = "Game Over!\n\nSurvived " + m_CurrentLevel + " days";
    }
}
