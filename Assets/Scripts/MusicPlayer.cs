using UnityEngine;


[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    [SerializeField, Tooltip("Looping background track. Leave empty to use the clip already set on the AudioSource.")]
    private AudioClip m_MusicClip;

    [SerializeField, Range(0f, 1f), Tooltip("Music volume. Kept low so sound effects stay audible.")]
    private float m_Volume = 0.25f;

    private AudioSource m_Source;

    private void Awake()
    {
        m_Source = GetComponent<AudioSource>();

        if (m_MusicClip != null)
        {
            m_Source.clip = m_MusicClip;
        }

        m_Source.loop = true;
        m_Source.volume = m_Volume;
    }

    private void Start()
    {
        if (m_Source.clip != null && !m_Source.isPlaying)
        {
            m_Source.Play();
        }
    }
    
    private void OnValidate()
    {
        if (m_Source != null)
        {
            m_Source.volume = m_Volume;
        }
    }
}
