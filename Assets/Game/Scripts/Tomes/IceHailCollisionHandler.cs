using UnityEngine;

public class IceHailCollisionHandler : MonoBehaviour
{
    [Header("Hit SFX")]
    public AudioClip[] hailHitSFX;
    [Range(0f, 1f)] public float hailHitVolume = 0.7f;

    private float damage;
    private float slowAmount;
    private float slowDuration;

    public void Initialize(float damageAmount, float slowAmount, float slowDuration)
    {
        damage = damageAmount;
        this.slowAmount = slowAmount;
        this.slowDuration = slowDuration;
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                enemy.ApplySlow(slowAmount, slowDuration);
            }
        }

        PlayRandomHailSound();
    }

    private void PlayRandomHailSound()
    {
        if (hailHitSFX == null || hailHitSFX.Length == 0) return;

        AudioClip clip = hailHitSFX[Random.Range(0, hailHitSFX.Length)];

        GameObject tempAudio = new GameObject("TempHailAudio");
        tempAudio.transform.position = transform.position;

        AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
        tempSource.clip = clip;
        tempSource.spatialBlend = 1.0f;
        tempSource.rolloffMode = AudioRolloffMode.Linear;
        tempSource.volume = hailHitVolume;
        tempSource.minDistance = 1f;
        tempSource.maxDistance = 20f;

        tempSource.Play();
        Destroy(tempAudio, clip.length);
    }
}
