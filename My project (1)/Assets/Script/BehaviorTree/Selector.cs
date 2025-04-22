using System.Collections.Generic;

namespace BehaviorTree
{
    public class Selector : Node
    {
        private List<Node> nodes;

        public Selector(List<Node> nodes) => this.nodes = nodes;

        public override NodeState Evaluate()
        {
            foreach (var node in nodes)
            {
                switch (node.Evaluate())
                {
                    case NodeState.Success:
                        state = NodeState.Success;
                        return state;
                    case NodeState.Running:
                        state = NodeState.Running;
                        return state;
                }
            }

            state = NodeState.Failure;
            return state;
        }
    }
}
