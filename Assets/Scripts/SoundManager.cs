using UnityEngine;


[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField, Tooltip("One is picked at random each time the player steps into a new cell.")]
    private AudioClip[] m_FootstepClips;

    [SerializeField, Tooltip("Played when the player hits a wall.")]
    private AudioClip[] m_WallAttackClips;

    [SerializeField, Tooltip("Played when the player hits an enemy without killing it. Falls back to the wall attack clips if empty.")]
    private AudioClip[] m_EnemyHitClips;

    [SerializeField, Tooltip("Played when the player picks up food.")]
    private AudioClip[] m_FoodPickupClips;

    [Header("Enemy")]
    [SerializeField, Tooltip("Played when an enemy attacks the player.")]
    private AudioClip[] m_EnemyAttackClips;

    [SerializeField, Tooltip("Played when an enemy's health reaches 0.")]
    private AudioClip[] m_EnemyDeathClips;

    [Header("Game State")]
    [SerializeField, Tooltip("Played once when the player runs out of food.")]
    private AudioClip m_GameOverClip;

    [Header("Mixing")]
    [SerializeField, Range(0f, 1f), Tooltip("Volume scale passed to PlayOneShot for every effect.")]
    private float m_SfxVolume = 0.8f;

    [SerializeField, Range(0f, 0.3f), Tooltip("Random pitch offset (+/-) applied to each effect so repeated sounds don't feel robotic.")]
    private float m_PitchVariation = 0.05f;

    private AudioSource m_Source;

    private void Awake()
    {
        m_Source = GetComponent<AudioSource>();
        m_Source.playOnAwake = false;
        m_Source.loop = false;
    }

    public void PlayFootstep() => PlayRandom(m_FootstepClips);

    public void PlayWallAttack() => PlayRandom(m_WallAttackClips);

    public void PlayEnemyHit()
    {
        bool hasEnemyHitClips = m_EnemyHitClips != null && m_EnemyHitClips.Length > 0;
        PlayRandom(hasEnemyHitClips ? m_EnemyHitClips : m_WallAttackClips);
    }

    public void PlayFoodPickup() => PlayRandom(m_FoodPickupClips);

    public void PlayEnemyAttack() => PlayRandom(m_EnemyAttackClips);

    public void PlayEnemyDeath() => PlayRandom(m_EnemyDeathClips);

    public void PlayGameOver() => Play(m_GameOverClip, false);

    private void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        Play(clips[Random.Range(0, clips.Length)], true);
    }

    private void Play(AudioClip clip, bool varyPitch)
    {
        if (clip == null)
        {
            return;
        }

        m_Source.pitch = varyPitch ? 1f + Random.Range(-m_PitchVariation, m_PitchVariation) : 1f;
        m_Source.PlayOneShot(clip, m_SfxVolume);
    }
}
