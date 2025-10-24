using UnityEngine;

public class Gibbing : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] explosionSounds;
    void Start()
    {
        audioSource.pitch = Random.Range(0.8f, 1.1f);
        audioSource.PlayOneShot(explosionSounds[Random.Range(0, explosionSounds.Length)], 0.5f);
    }
}
