using System;

namespace BehaviorTree
{
    public class ConditionNode : Node
    {
        private Func<bool> condition;

        public ConditionNode(Func<bool> condition) => this.condition = condition;

        public override NodeState Evaluate()
        {
            return condition() ? NodeState.Success : NodeState.Failure;
        }
    }
}
