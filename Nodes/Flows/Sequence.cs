﻿using Amlos.AI.References;
using Minerva.Module;
using System;
using System.Collections.Generic;

namespace Amlos.AI.Nodes
{
    /// <summary>
    /// node that will execute all its child
    /// </summary>
    [Serializable]
    [NodeTip("A sequence, always execute a list of nodes in order")]
    public sealed class Sequence : Flow, IListFlow
    {
        [ReadOnly] public List<NodeReference> events;
        [ReadOnly] TreeNode current;
        [ReadOnly] public bool hasTrue;

        public Sequence()
        {
            events = new();
        }

        public override State ReceiveReturnFromChild(bool @return)
        {
            if (events.IndexOf(current) == events.Count - 1)
            {
                return StateOf(hasTrue);
            }
            else
            {
                hasTrue |= @return;
                current = events[events.IndexOf(current) + 1];
                return SetNextExecute(current);
            }
        }

        public sealed override State Execute()
        {
            hasTrue = false;
            if (events.Count == 0)
            {
                return State.Failed;
            }
            current = events[0];
            return SetNextExecute(current);
        }

        public override void Initialize()
        {
            current = null;
            hasTrue = false;
            for (int i = 0; i < events.Count; i++)
            {
                NodeReference item = events[i];
                events[i] = behaviourTree.References[item];
            }
        }




        public void Add(TreeNode treeNode)
        {
            events.Add(treeNode);
            treeNode.parent.UUID = uuid;
        }

        public void Insert(int index, TreeNode treeNode)
        {
            events.Insert(index, treeNode);
            treeNode.parent.UUID = uuid;
        }

        public int IndexOf(TreeNode treeNode)
        {
            return events.IndexOf(treeNode);
        }
    }
}