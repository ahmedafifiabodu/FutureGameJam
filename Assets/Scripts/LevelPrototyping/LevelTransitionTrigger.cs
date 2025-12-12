using UnityEngine;

public class LevelTransitionTrigger : MonoBehaviour
{
    public GameObject objectToActivate;
    public GameObject objectToActivateSequel;
    public GameObject objectToDeactivate;
    public GameObject objectToDeactivateSequel;
    
    private void OnTriggerEnter(Collider other)
    {
        if (objectToActivate != null)
            objectToActivate.SetActive(true);

        if (objectToActivateSequel != null)
            objectToActivateSequel.SetActive(true);


        if (objectToDeactivate != null)
            objectToDeactivate.SetActive(false);

        if (objectToDeactivate != null)
            objectToDeactivateSequel.SetActive(false);
        ServiceLocator.Instance.GetService<GameStateManager>().StartParasiteMode();
        Destroy(gameObject);
    }
}
