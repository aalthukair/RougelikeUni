using System.Collections.Generic;
using UnityEngine;

public class CellObjectPool
{
    private readonly Transform m_Container;
    private readonly Dictionary<CellObject, Queue<CellObject>> m_Available = new Dictionary<CellObject, Queue<CellObject>>();
    private readonly Dictionary<CellObject, CellObject> m_PrefabOfInstance = new Dictionary<CellObject, CellObject>();

    public CellObjectPool(Transform container)
    {
        m_Container = container;
    }
    
    public void Prewarm(CellObject prefab, int count)
    {
        if (prefab == null)
        {
            return;
        }

        Queue<CellObject> available = GetQueue(prefab);
        while (available.Count < count)
        {
            available.Enqueue(CreateInstance(prefab));
        }
    }
    
    public T Get<T>(T prefab) where T : CellObject
    {
        Queue<CellObject> available = GetQueue(prefab);
        CellObject instance = available.Count > 0 ? available.Dequeue() : CreateInstance(prefab);

        instance.gameObject.SetActive(true);
        return (T)instance;
    }
    
    public void Release(CellObject instance)
    {
        if (instance == null || !instance.gameObject.activeSelf)
        {
            return;
        }

        if (!m_PrefabOfInstance.TryGetValue(instance, out CellObject prefab))
        {
            Debug.LogWarning($"{instance.name} was not created by the CellObjectPool, destroying it instead.");
            Object.Destroy(instance.gameObject);
            return;
        }

        instance.gameObject.SetActive(false);
        m_Available[prefab].Enqueue(instance);
    }

    private Queue<CellObject> GetQueue(CellObject prefab)
    {
        if (!m_Available.TryGetValue(prefab, out Queue<CellObject> available))
        {
            available = new Queue<CellObject>();
            m_Available.Add(prefab, available);
        }

        return available;
    }

    private CellObject CreateInstance(CellObject prefab)
    {
        CellObject instance = Object.Instantiate(prefab, m_Container);
        instance.name = prefab.name;
        instance.gameObject.SetActive(false);

        m_PrefabOfInstance.Add(instance, prefab);
        return instance;
    }
}
