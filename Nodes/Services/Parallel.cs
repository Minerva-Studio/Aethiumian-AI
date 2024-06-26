﻿using Amlos.AI.References;
using System;

namespace Amlos.AI.Nodes
{
    [NodeTip("Run a parallel subtree")]
    [Serializable]
    public sealed class Parallel : Service
    {
        public NodeReference subtreeHead;
        private bool isRunning;

        public override bool IsReady => !isRunning;

        public override State Execute()
        {
            isRunning = true;
            return SetNextExecute(subtreeHead);
        }

        public override void Initialize()
        {
            behaviourTree.GetNode(ref subtreeHead);
        }

        public override void UpdateTimer()
        {
            //nothing
        }

        public override void OnRegistered()
        {
            isRunning = false;
        }

        public override void OnUnregistered()
        {
            isRunning = false;
        }
    }
    /**
     * - Sequence
     *   - store enemyCount from GetEnemyCount(); [Node]
     *   - condition
     *     - if enemyCount > 3
     *     - true: ()
     */
}
