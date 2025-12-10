using UnityEngine;

public class LevelTransitionTrigger : MonoBehaviour
{
    public GameObject objectToActivate;
    public GameObject objectToDeactivate;
    
    private void OnTriggerEnter(Collider other)
    {

        if (objectToActivate != null)
            objectToActivate.SetActive(true);

        if (objectToDeactivate != null)
            objectToDeactivate.SetActive(false);
    }
}
