using UnityEngine;
using System.Collections.Generic;

public class QLearning : MonoBehaviour
{
    private Dictionary<string, float> qTable = new Dictionary<string, float>();
    private Movement movement;


    void Start()
    {
        movement = GetComponent<Movement>();
    }

    void ExecuteAction(string action)
    {
        switch (action)
        {
            case "moveForward":
                movement.Move(Vector3.forward);
                break;
            case "moveBack":
                movement.Move(Vector3.back);
                break;
            case "rotateLeft":
                movement.Rotate(-1f);
                break;
            case "rotateRight":
                movement.Rotate(1f);
                break;
        }
    }

    void Update()
    {
        string currentState = GetCurrentState();
        string bestAction = ChooseBestAction(currentState);
        ExecuteAction(bestAction);

    }

    string GetCurrentState()
    {

        return "";
    }

    string ChooseBestAction(string state)
    {
        return "moveForward";
    }


    public void UpdateQValue(float reward, string oldState, string action, string newState)
    {

    }
}