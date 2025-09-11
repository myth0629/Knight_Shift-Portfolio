using UnityEngine;

namespace EnemyAI
{
    public enum NodeState { Success, Failure, Running }

    public abstract class BTNode
    {
        protected EnemyBlackboard bb;
        public BTNode(EnemyBlackboard blackboard) { bb = blackboard; }
        public abstract NodeState Evaluate();
    }

    // Composite Nodes
    public class Sequence : BTNode
    {
        private BTNode[] children;
        public Sequence(EnemyBlackboard bb, params BTNode[] children) : base(bb) { this.children = children; }
        public override NodeState Evaluate()
        {
            foreach (var c in children)
            {
                var result = c.Evaluate();
                if (result != NodeState.Success) return result; // Failure or Running
            }
            return NodeState.Success;
        }
    }

    public class Selector : BTNode
    {
        private BTNode[] children;
        public Selector(EnemyBlackboard bb, params BTNode[] children) : base(bb) { this.children = children; }
        public override NodeState Evaluate()
        {
            foreach (var c in children)
            {
                var result = c.Evaluate();
                if (result != NodeState.Failure) return result; // Success or Running
            }
            return NodeState.Failure;
        }
    }
}
