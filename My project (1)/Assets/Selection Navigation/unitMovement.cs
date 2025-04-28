using UnityEngine;
using UnityEngine.AI;

public class unitMovement : MonoBehaviour
{
    Camera cam;
    NavMeshAgent agent;
    public LayerMask ground;

    private void Start()
    {
        cam = Camera.main;
        agent = GetComponent<NavMeshAgent>();

    }

    //use ray casting for character direction
    private void Update()
    {
        if (UnitSelectionManager.Instance.unitsSelected.Contains(gameObject))
            return;

        if (Input.GetMouseButtonDown(1)) // mouse right click
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground))
            {
                agent.SetDestination(hit.point); //set the ray casting hitting point as destination
            }
        }
    }
}
