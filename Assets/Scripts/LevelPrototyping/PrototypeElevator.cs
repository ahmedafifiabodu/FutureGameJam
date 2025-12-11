using UnityEngine;

public class PrototypeElevator : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private Transform targetPoint;
    private bool moving;

    void Start()
    {
        moving = true;
    }

    void Update()
    {
        if (!moving || targetPoint == null)
            return;
        transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPoint.position) < 0.01f)
        {
            moving = false;
        }
    }
}
