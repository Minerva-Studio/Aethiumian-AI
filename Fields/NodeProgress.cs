﻿using Amlos.AI.Nodes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Amlos.AI.References
{
    /// <summary>
    /// class representing the progress of ai
    /// </summary>
    public class NodeProgress : IDisposable, IAsyncEnumerable<float>
    {
        struct Node : IAsyncEnumerator<float>
        {
            private NodeProgress nodeProgress;
            private CancellationToken cancellationToken;

            public Node(NodeProgress nodeProgress, CancellationToken cancellationToken)
            {
                this.nodeProgress = nodeProgress;
                this.cancellationToken = cancellationToken;
            }

            public float Current => nodeProgress.node.behaviourTree.CurrentStageDuration;

            public async ValueTask DisposeAsync()
            {
                await Task.CompletedTask;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
                if (!nodeProgress.isValid)
                {
                    return false;
                }
                await Task.Yield();
                return true;
            }
        }

        readonly TreeNode node;
        bool hasReturned;
        bool returnVal;
        bool disposed;

        /// <summary>
        /// action will execute when the node is forced to stop
        /// </summary>
        public event System.Action InterruptStopAction { add => node.OnInterrupted += value; remove => node.OnInterrupted -= value; }
        public bool isValid => !hasReturned && !disposed;

        /// <summary>
        /// waiting coroutine for script
        /// </summary>
        Coroutine coroutine;
        MonoBehaviour behaviour;

        public NodeProgress(TreeNode node)
        {
            this.node = node;
            this.node.OnInterrupted += Dispose;
        }


        /// <summary>
        /// pause the behaviour tree
        /// </summary>
        public void Pause()
        {
            node.behaviourTree.Pause();
        }

        /// <summary>
        /// resume ai running
        /// </summary>
        public void Resume()
        {
            node.behaviourTree.Resume();
        }

        /// <summary>
        /// end this node
        /// </summary>
        /// <param name="return">the return value of the node</param>
        public bool End(bool @return)
        {
            //do not return again if has returned
            if (!isValid)
            {
                return false;
            }
            if (node is not Nodes.Action action)
            {
                return false;
            }
            //return hasReturned = action.behaviourTree.ReceiveReturn(node, @return);
            return hasReturned = action.ReceiveEndSignal(@return);
        }

        /// <summary>
        /// end the node execution when the mono behaviour is destroyed
        /// </summary>
        /// <param name="monoBehaviour"></param>
        /// <param name="ret"></param>
        public void RunAndReturn(MonoBehaviour monoBehaviour, bool ret = true)
        {
            coroutine = node.AIComponent.StartCoroutine(Wait());
            behaviour = monoBehaviour;
            InterruptStopAction += BreakRunAndReturn;
            returnVal = ret;
            IEnumerator Wait()
            {
                while (monoBehaviour)
                {
                    yield return new WaitForFixedUpdate();
                }
                if (!hasReturned) End(returnVal);
            }
        }

        /// <summary>
        /// Set the return value of the node progress
        /// </summary>
        /// <param name="returnVal"></param>
        public void SetReturnVal(bool returnVal)
        {
            if (!isValid)
            {
                Debug.LogWarning("Setting return value to node progress that is already returned.");
                return;
            }
            this.returnVal = returnVal;
        }

        private void BreakRunAndReturn()
        {
            if (coroutine == null)
            {
                return;
            }
            node.AIComponent.StopCoroutine(coroutine);
            UnityEngine.Object.Destroy(behaviour);
        }

        public void Dispose()
        {
            disposed = true;
        }

        public IAsyncEnumerator<float> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new Node(this, cancellationToken);
        }
    }
}