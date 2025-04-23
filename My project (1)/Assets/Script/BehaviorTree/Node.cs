using UnityEngine;

namespace BehaviorTree
{
    public abstract class Node
    {
        public enum NodeState { Running, Success, Failure }
        protected NodeState state;
        public abstract NodeState Evaluate();
    }
}

