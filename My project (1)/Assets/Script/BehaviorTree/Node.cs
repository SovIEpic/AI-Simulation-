using System.Collections.Generic;

namespace BehaviorTree
{
    public enum NodeState
    {
        SUCCESS,
        FAILURE,
        RUNNING
    }

    public abstract class Node
    {
        protected NodeState state;
        protected List<Node> children = new List<Node>();

        public Node() { }

        public Node(List<Node> children)
        {
            this.children = children;
        }

        public virtual NodeState Evaluate()
        {
            return NodeState.FAILURE;
        }
    }

    public class Selector : Node
    {
        public Selector() : base() { }
        public Selector(List<Node> children) : base(children) { }

        public override NodeState Evaluate()
        {
            foreach (Node node in children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.FAILURE:
                        continue;
                    case NodeState.SUCCESS:
                        state = NodeState.SUCCESS;
                        return state;
                    case NodeState.RUNNING:
                        state = NodeState.RUNNING;
                        return state;
                }
            }
            state = NodeState.FAILURE;
            return state;
        }
    }

    public class Sequence : Node
    {
        public Sequence() : base() { }
        public Sequence(List<Node> children) : base(children) { }

        public override NodeState Evaluate()
        {
            foreach (Node node in children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.FAILURE:
                        state = NodeState.FAILURE;
                        return state;
                    case NodeState.SUCCESS:
                        continue;
                    case NodeState.RUNNING:
                        state = NodeState.RUNNING;
                        return state;
                }
            }
            state = NodeState.SUCCESS;
            return state;
        }
    }

    public class ConditionNode : Node
    {
        public System.Func<bool> condition;

        public ConditionNode(System.Func<bool> condition) : base()
        {
            this.condition = condition;
        }

        public override NodeState Evaluate()
        {
            state = condition() ? NodeState.SUCCESS : NodeState.FAILURE;
            return state;
        }
    }
}