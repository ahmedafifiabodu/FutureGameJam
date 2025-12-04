using UnityEngine;

public class PrototypeElevator : MonoBehaviour
{
    public bool canMove = false;

    [SerializeField] private float speed = 2f;
    [SerializeField] private int startPoint = 0;
    [SerializeField] private Transform[] points;

    private int i;
    private bool reverse;
    
    void Start()
    {
        transform.position = points[startPoint].position;
        i = startPoint;
    }
    private void OnTriggerEnter(Collider other)
    {
        canMove = true;
    }

    void FixedUpdate()
    {
        if (!canMove) return;

        if (Vector3.Distance(transform.position, points[i].position) < 0.01f)
        {
            if (i == points.Length - 1)
            {
                reverse = true;
                i--;
            }
            else if (i == 0)
            {
                reverse = false;
                i++;
            }
            else
            {
                i += reverse ? -1 : 1;
            }
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            points[i].position,
            speed * Time.deltaTime
        );
    }
}
