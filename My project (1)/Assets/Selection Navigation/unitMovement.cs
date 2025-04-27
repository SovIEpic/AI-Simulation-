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

    //Use ray casting for character direction
    private void Update()
    {
        if (Input.GetMouseButtonDown(1)) //check if player click on right button
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition); //shoot ray cast from camera to the position of the mouse

            if(Physics.Raycast(ray, out hit, Mathf.Infinity, ground)) //check if ray is hitting ground
            {
                agent.SetDestination(hit.point); // set destination to the point of click
            }
        }
    }
}
