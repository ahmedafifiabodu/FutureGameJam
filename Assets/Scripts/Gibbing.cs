using UnityEngine;

public class Gibbing : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] explosionSounds;
    void Start()
    {
        audioSource.PlayOneShot(explosionSounds[Random.Range(0, explosionSounds.Length)]);
    }
}
