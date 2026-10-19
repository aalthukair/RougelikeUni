using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectsManager : MonoBehaviour
{
    [Header("Effect Prefabs")]
    [SerializeField, Tooltip("Burst played when a wall is destroyed.")]
    private ParticleSystem m_WallDestroyedPrefab;

    [SerializeField, Tooltip("Burst played when the player picks up food.")]
    private ParticleSystem m_FoodCollectedPrefab;

    [SerializeField, Tooltip("Burst played when an enemy dies.")]
    private ParticleSystem m_EnemyDeathPrefab;

    [Header("Pooling")]
    [SerializeField, Range(1, 10), Tooltip("Instances of each effect created up front. The pool grows if more play at once.")]
    private int m_PoolSizePerEffect = 3;

    [SerializeField, Range(0, 100), Tooltip("Sorting order applied to every effect so particles draw above the board and sprites.")]
    private int m_SortingOrder = 20;

    private readonly Dictionary<ParticleSystem, Queue<ParticleSystem>> m_Pools =
        new Dictionary<ParticleSystem, Queue<ParticleSystem>>();

    private void Awake()
    {
        Prewarm(m_WallDestroyedPrefab);
        Prewarm(m_FoodCollectedPrefab);
        Prewarm(m_EnemyDeathPrefab);
    }

    public void PlayWallDestroyed(Vector3 position) => Play(m_WallDestroyedPrefab, position);

    public void PlayFoodCollected(Vector3 position) => Play(m_FoodCollectedPrefab, position);

    public void PlayEnemyDeath(Vector3 position) => Play(m_EnemyDeathPrefab, position);

    public void Play(ParticleSystem prefab, Vector3 position)
    {
        if (prefab == null)
        {
            return;
        }

        Queue<ParticleSystem> pool = GetPool(prefab);
        ParticleSystem effect = pool.Count > 0 ? pool.Dequeue() : CreateInstance(prefab);

        effect.transform.position = position;
        effect.gameObject.SetActive(true);
        effect.Play(true);

        StartCoroutine(ReturnWhenFinished(effect, pool));
    }

    private IEnumerator ReturnWhenFinished(ParticleSystem effect, Queue<ParticleSystem> pool)
    {
        yield return new WaitForSeconds(GetTotalDuration(effect));

        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.gameObject.SetActive(false);
        pool.Enqueue(effect);
    }

    private void Prewarm(ParticleSystem prefab)
    {
        if (prefab == null)
        {
            return;
        }

        Queue<ParticleSystem> pool = GetPool(prefab);
        while (pool.Count < m_PoolSizePerEffect)
        {
            pool.Enqueue(CreateInstance(prefab));
        }
    }

    private Queue<ParticleSystem> GetPool(ParticleSystem prefab)
    {
        if (!m_Pools.TryGetValue(prefab, out Queue<ParticleSystem> pool))
        {
            pool = new Queue<ParticleSystem>();
            m_Pools.Add(prefab, pool);
        }

        return pool;
    }

    private ParticleSystem CreateInstance(ParticleSystem prefab)
    {
        ParticleSystem effect = Instantiate(prefab, transform);
        effect.name = prefab.name;

        foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;
        }

        foreach (ParticleSystemRenderer particleRenderer in effect.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            particleRenderer.sortingOrder = m_SortingOrder;
        }

        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.gameObject.SetActive(false);
        return effect;
    }
    
    private static float GetTotalDuration(ParticleSystem effect)
    {
        float longest = 0f;

        foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule main = system.main;
            ParticleSystem.MinMaxCurve lifetime = main.startLifetime;

            float maxLifetime;
            switch (lifetime.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    maxLifetime = lifetime.constant;
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    maxLifetime = lifetime.constantMax;
                    break;
                default:
                    maxLifetime = lifetime.curveMultiplier;
                    break;
            }

            longest = Mathf.Max(longest, main.startDelay.constantMax + main.duration + maxLifetime);
        }

        return longest;
    }
}
