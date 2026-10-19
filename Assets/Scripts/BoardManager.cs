using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class BoardManager : MonoBehaviour
{
    public class CellData
    {
        public bool Passable { get; set; }
        public CellObject ContainedObject { get; set; }
    }
    
    //AI ASSISTANCE NOTICE
    //I used an LLM to help generate tooltips and serializations for each field + ranges
    
    [Header("Board")]
    [SerializeField, FormerlySerializedAs("Width")]
    [Range(5, 30), Tooltip("Board width in cells, including the outer wall.")]
    private int m_Width = 8;

    [SerializeField, FormerlySerializedAs("Height")]
    [Range(5, 30), Tooltip("Board height in cells, including the outer wall.")]
    private int m_Height = 8;

    [SerializeField, FormerlySerializedAs("GroundTiles")]
    [Tooltip("Walkable tiles, picked at random for each inner cell.")]
    private Tile[] m_GroundTiles;

    [SerializeField, FormerlySerializedAs("WallTiles")]
    [Tooltip("Impassable tiles, picked at random for the outer border.")]
    private Tile[] m_WallTiles;

    [Header("Cell Object Prefabs")]
    [SerializeField, FormerlySerializedAs("FoodPrefab")]
    [Tooltip("Food items. One is picked at random for each food spawned.")]
    private FoodObject[] m_FoodPrefabs;

    [SerializeField, FormerlySerializedAs("WallPrefab")]
    [Tooltip("Breakable wall placed inside the board.")]
    private WallObject m_WallPrefab;

    [SerializeField]
    [Tooltip("Enemy types. One is picked at random for each enemy spawned.")]
    private Enemy[] m_EnemyPrefabs;

    [SerializeField, FormerlySerializedAs("ExitCellPrefab")]
    [Tooltip("Exit placed in the top-right corner that leads to the next level.")]
    private ExitCellObject m_ExitCellPrefab;

    [Header("Spawn Counts (random between min and max, inclusive)")]
    [SerializeField, Range(0, 20), Tooltip("Fewest breakable walls placed per level.")]
    private int m_MinWallCount = 6;

    [SerializeField, Range(0, 20), Tooltip("Most breakable walls placed per level.")]
    private int m_MaxWallCount = 9;

    [SerializeField, Range(0, 20), Tooltip("Fewest food items placed per level.")]
    private int m_MinFoodCount = 5;

    [SerializeField, Range(0, 20), Tooltip("Most food items placed per level.")]
    private int m_MaxFoodCount = 5;

    [SerializeField, Range(0, 10), Tooltip("Fewest enemies placed per level.")]
    private int m_MinEnemyCount = 1;

    [SerializeField, Range(0, 10), Tooltip("Most enemies placed per level.")]
    private int m_MaxEnemyCount = 3;

    [SerializeField, Range(1, 8), Tooltip("Enemies never spawn closer than this many steps to the player's start cell.")]
    private int m_EnemyMinSpawnDistance = 3;

    private CellData[,] m_BoardData;
    private Tilemap m_Tilemap;
    private Grid m_Grid;
    private List<Vector2Int> m_EmptyCellsList;
    private readonly List<int> m_CandidateCells = new List<int>();
    private CellObjectPool m_Pool;

    public Vector2Int PlayerStartCell => new Vector2Int(1, 1);

    private void Awake()
    {
        m_Tilemap = GetComponentInChildren<Tilemap>();
        m_Grid = GetComponentInChildren<Grid>();
        m_EmptyCellsList = new List<Vector2Int>();

        CreatePool();
    }

    private void OnValidate()
    {
        m_MaxWallCount = Mathf.Max(m_MinWallCount, m_MaxWallCount);
        m_MaxFoodCount = Mathf.Max(m_MinFoodCount, m_MaxFoodCount);
        m_MaxEnemyCount = Mathf.Max(m_MinEnemyCount, m_MaxEnemyCount);
    }
    
    private void CreatePool()
    {
        Transform container = new GameObject("CellObjectPool").transform;
        m_Pool = new CellObjectPool(container);

        m_Pool.Prewarm(m_ExitCellPrefab, 1);
        m_Pool.Prewarm(m_WallPrefab, m_MaxWallCount);

        if (m_FoodPrefabs != null)
        {
            foreach (FoodObject foodPrefab in m_FoodPrefabs)
            {
                m_Pool.Prewarm(foodPrefab, m_MaxFoodCount);
            }
        }

        if (m_EnemyPrefabs != null)
        {
            foreach (Enemy enemyPrefab in m_EnemyPrefabs)
            {
                m_Pool.Prewarm(enemyPrefab, m_MaxEnemyCount);
            }
        }
    }

    public void Init()
    {
        m_EmptyCellsList.Clear();
        m_BoardData = new CellData[m_Width, m_Height];

        for (int y = 0; y < m_Height; ++y)
        {
            for(int x = 0; x < m_Width; ++x)
            {
                Tile tile;
                m_BoardData[x, y] = new CellData();

                if(x == 0 || y == 0 || x == m_Width - 1 || y == m_Height - 1)
                {
                    tile = m_WallTiles[Random.Range(0, m_WallTiles.Length)];
                    m_BoardData[x, y].Passable = false;
                }
                else
                {
                    tile = m_GroundTiles[Random.Range(0, m_GroundTiles.Length)];
                    m_BoardData[x, y].Passable = true;

                    m_EmptyCellsList.Add(new Vector2Int(x, y));
                }

                m_Tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        m_EmptyCellsList.Remove(PlayerStartCell);

        Vector2Int endCoord = new Vector2Int(m_Width - 2, m_Height - 2);
        if (m_ExitCellPrefab != null)
        {
            AddObject(m_Pool.Get(m_ExitCellPrefab), endCoord);
        }
        m_EmptyCellsList.Remove(endCoord);

        GenerateWall();
        GenerateFood();
        GenerateEnemies();
    }

    public Vector3 CellToWorld(Vector2Int cellIndex)
    {
        return m_Grid.GetCellCenterWorld((Vector3Int)cellIndex);
    }

    public CellData GetCellData(Vector2Int cellIndex)
    {
        if (cellIndex.x < 0 || cellIndex.x >= m_Width
                            || cellIndex.y < 0 || cellIndex.y >= m_Height)
        {
            return null;
        }

        return m_BoardData[cellIndex.x, cellIndex.y];
    }

    void GenerateWall()
    {
        if (m_WallPrefab == null)
            return;

        int wallCount = Random.Range(m_MinWallCount, m_MaxWallCount + 1);
        for (int i = 0; i < wallCount; ++i)
        {
            if (!TryTakeRandomEmptyCell(0, out Vector2Int coord))
                return;

            AddObject(m_Pool.Get(m_WallPrefab), coord);
        }
    }

    void GenerateFood()
    {
        if (m_FoodPrefabs == null || m_FoodPrefabs.Length == 0)
            return;

        int foodCount = Random.Range(m_MinFoodCount, m_MaxFoodCount + 1);
        for (int i = 0; i < foodCount; ++i)
        {
            if (!TryTakeRandomEmptyCell(0, out Vector2Int coord))
                return;

            int foodIndex = Random.Range(0, m_FoodPrefabs.Length);
            AddObject(m_Pool.Get(m_FoodPrefabs[foodIndex]), coord);
        }
    }

    void GenerateEnemies()
    {
        if (m_EnemyPrefabs == null || m_EnemyPrefabs.Length == 0)
            return;

        int enemyCount = Random.Range(m_MinEnemyCount, m_MaxEnemyCount + 1);
        for (int i = 0; i < enemyCount; ++i)
        {
            if (!TryTakeRandomEmptyCell(m_EnemyMinSpawnDistance, out Vector2Int coord))
                return;

            int enemyIndex = Random.Range(0, m_EnemyPrefabs.Length);
            AddObject(m_Pool.Get(m_EnemyPrefabs[enemyIndex]), coord);
        }
    }
    
    bool TryTakeRandomEmptyCell(int minDistanceFromStart, out Vector2Int coord)
    {
        m_CandidateCells.Clear();
        for (int i = 0; i < m_EmptyCellsList.Count; ++i)
        {
            Vector2Int offset = m_EmptyCellsList[i] - PlayerStartCell;
            if (Mathf.Abs(offset.x) + Mathf.Abs(offset.y) >= minDistanceFromStart)
            {
                m_CandidateCells.Add(i);
            }
        }

        if (m_CandidateCells.Count == 0)
        {
            coord = default;
            return false;
        }

        int index = m_CandidateCells[Random.Range(0, m_CandidateCells.Count)];
        coord = m_EmptyCellsList[index];
        m_EmptyCellsList.RemoveAt(index);
        return true;
    }

    public Tile GetCellTile(Vector2Int cellIndex)
    {
        return m_Tilemap.GetTile<Tile>(new Vector3Int(cellIndex.x, cellIndex.y, 0));
    }

    public void SetCellTile(Vector2Int cellIndex, Tile tile)
    {
        m_Tilemap.SetTile(new Vector3Int(cellIndex.x, cellIndex.y, 0), tile);
    }

    void AddObject(CellObject obj, Vector2Int coord)
    {
        CellData data = m_BoardData[coord.x, coord.y];
        obj.transform.position = CellToWorld(coord);
        data.ContainedObject = obj;
        obj.Init(coord);
    }
    
    public void RemoveObject(CellObject obj)
    {
        CellData data = GetCellData(obj.Cell);
        if (data != null && data.ContainedObject == obj)
        {
            data.ContainedObject = null;
        }

        m_Pool.Release(obj);
    }

    public void Clean()
    {
        if(m_BoardData == null)
            return;
        for (int y = 0; y < m_BoardData.GetLength(1); ++y)
        {
            for (int x = 0; x < m_BoardData.GetLength(0); ++x)
            {
                var cellData = m_BoardData[x, y];

                if (cellData.ContainedObject != null)
                {
                    m_Pool.Release(cellData.ContainedObject);
                    cellData.ContainedObject = null;
                }

                SetCellTile(new Vector2Int(x,y), null);
            }
        }
    }
}
