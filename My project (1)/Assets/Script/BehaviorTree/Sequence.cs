using System.Collections.Generic;

namespace BehaviorTree
{
    public class Sequence : Node
    {
        private List<Node> nodes;

        public Sequence(List<Node> nodes)
        {
            this.nodes = nodes;
        }

        public override NodeState Evaluate()
        {
            bool anyRunning = false;

            foreach (var node in nodes)
            {
                var result = node.Evaluate();
                if (result == NodeState.Failure)
                {
                    state = NodeState.Failure;
                    return state;
                }
                if (result == NodeState.Running)
                {
                    anyRunning = true;
                }
            }

            state = anyRunning ? NodeState.Running : NodeState.Success;
            return state;
        }
    }
}
