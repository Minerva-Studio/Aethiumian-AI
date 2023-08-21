﻿using Amlos.AI.Nodes;
using Minerva.Module;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Amlos.AI
{
    public partial class BehaviourTree
    {
        /// <summary>
        /// The call stack of inside the behaviour tree
        /// </summary>
        [Serializable]
        public class NodeCallStack
        {
            public enum StackState
            {
                Invalid = -1,
                /// <summary>
                /// stack is ready to continue after a wait, and will continue executing in this frame or next frame of the game
                /// </summary>
                Ready,
                /// <summary>
                /// stack is calling nodes
                /// </summary>
                Calling,
                /// <summary>
                /// stack is receiving return value true
                /// </summary>
                Receiving,
                /// <summary>
                /// stack is waiting for next update
                /// </summary>
                WaitUntilNextUpdate,
                /// <summary>
                /// stack is waiting for some time
                /// </summary>
                Waiting,
                /// <summary>
                /// stack is NOT executing
                /// </summary>
                End,
            }

            public event Action<TreeNode> OnNodePopStack;
            public event System.Action OnStackEnd;
            protected Stack<TreeNode> callStack;

            public int Count => callStack.Count;
            public bool IsPaused { get; set; }
            public bool PauseAfterSingleExecution { get; set; }
            public bool? Result { get; protected set; }

            /// <summary> Check whether stack is in receiving state</summary>
            public bool IsInReceivingState => State == StackState.Receiving;
            /// <summary> Check whether stack is in waiting state</summary>
            public bool IsInWaitingState => State == StackState.Waiting || State == StackState.WaitUntilNextUpdate;
            /// <summary> Check whether stack is in error state</summary>
            public bool IsInInvalidState => State == StackState.Invalid;

            /// <summary> State of the stack </summary>
            public StackState State { get; set; }
            /// <summary> Ongoing executing node </summary>
            public TreeNode Current { get; protected set; }
            /// <summary> Last executing node </summary>
            public TreeNode Last { get; protected set; }

            public bool Receive => State == StackState.Receiving;

            public List<TreeNode> Nodes => callStack.ShallowCloneToList();

            public NodeCallStack()
            {
                callStack = new Stack<TreeNode>();
            }

            public virtual void Initialize()
            {
                callStack ??= new Stack<TreeNode>();
                callStack.Clear();
                Current = null;
                State = StackState.Ready;
                IsPaused = false;
            }

            /// <summary>
            /// start the stack
            /// </summary>
            public void Start(TreeNode head)
            {
                if (Current != null) throw new InvalidOperationException($"The behaviour tree stack is not initialized properly: Current node not null ({head.name})");
                if (State != StackState.Ready) throw new InvalidOperationException($"The behaviour tree is not in Ready state when start. Execution abort. ({State}),({Last?.name}),({Current?.name})");

                Push(head);
                Continue();
            }

            /// <summary>
            /// Receive return from an action node
            /// </summary>
            public void ReceiveReturn(bool ret)
            {
                Pop();
                var prevState = State;
                State = StackState.Receiving;
                Result = ret;
                // was waiting, set current to null, reactivate stack
                if (prevState == StackState.Waiting) MoveState();
                Continue();
                //UnityEngine.Debug.LogError(prevState);
            }

            /// <summary>
            /// Receive return from an action node
            /// </summary>
            public void ReceiveReturn(State returnValue)
            {
                HandleResult(returnValue);
                switch (returnValue)
                {
                    case Amlos.AI.Nodes.State.Success:
                        ReceiveReturn(true);
                        break;
                    case Amlos.AI.Nodes.State.Failed:
                        ReceiveReturn(false);
                        break;
                    case Amlos.AI.Nodes.State.Error:
                        HandleErrorState(returnValue);
                        break;
                    // nothing should be done yet
                    case Amlos.AI.Nodes.State.Wait:
                    default:
                        break;
                    // action should not calling other nodes
                    case Amlos.AI.Nodes.State.NONE_RETURN:
                    // doesn't make sense for an action to WaitUntilNextUpdate because is already in waiting
                    case Amlos.AI.Nodes.State.WaitUntilNextUpdate:
                        break;
                }
            }

            /// <summary>
            /// continue executing the stack
            /// </summary>
            public void Continue()
            {
                Last = null;
                RunStack();

                // check calling end stack
                if (callStack.Count == 0 && State != StackState.End)
                {
                    End_Internal();
                }
            }

            /// <summary>
            /// force this call stack end, will call break first
            /// </summary>
            public void End()
            {
                BreakAll();
                End_Internal();
            }

            /// <summary>
            /// end this call stack
            /// </summary>
            protected void End_Internal()
            {
                if (State == StackState.End)
                {
                    return;
                }

                callStack.Clear();
                Current = null;
                Last = null;
                State = StackState.End;
                OnStackEnd?.Invoke();
                //Debug.Log("Stack is ended");
            }

            private void RunStack()
            {
                while (callStack.Count != 0 && (!IsPaused || IsInReceivingState) && Current == null)
                {
                    Current = callStack.Peek();
                    if (!IsInWaitingState && Last == Current && Last != null)
                    {
                        ThrowRecuriveExecution();
                        return;
                    }

                    switch (State)
                    {
                        case StackState.Ready:
                        case StackState.Calling:
                            State = StackState.Calling;
                            // make sure no Action.End called during execution
                            var current = Current;
                            State result;
                            try
                            {
                                result = current.Execute();
                            }
                            catch (NodeReturnException ret)
                            {
                                result = ret.ReturnValue;
                            }
                            catch (Exception) { throw; }
                            if (current == Current) HandleResult(result);
                            else throw new InvalidOperationException();
                            break;
                        case StackState.Receiving:
                            if (!Result.HasValue) throw new InvalidOperationException($"The behaviour tree cannot find return value from last node. Execution abort. ({StackState.Receiving}),({Last?.name}),({Current?.name})");
                            HandleResult(Current.ReceiveReturnFromChild(Result.Value));
                            break;
                        case StackState.WaitUntilNextUpdate:
                            break;
                        case StackState.Invalid:
                            throw new InvalidOperationException($"The behaviour tree is in invalid state. Execution abort. ({StackState.Invalid}),({Last?.name}),({Current?.name})");
                        case StackState.Waiting:
                        case StackState.End:
                        default:
                            return;
                    }

                    // break the loop
                    switch (State)
                    {
                        case StackState.Invalid:
                            throw new InvalidOperationException($"The behaviour tree is in invalid state. Execution abort. ({StackState.Invalid}),({Last?.name}),({Current?.name})");
                        // stack start waiting a short time, stop loop
                        case StackState.WaitUntilNextUpdate:
                        // stack start waiting, stop loop
                        case StackState.Waiting:
                        // stack ended early, stopped
                        case StackState.End:
                            return;
                    }

                    MoveState();


#if UNITY_EDITOR 
                    // debug section
                    if (PauseAfterSingleExecution && IsInReceivingState)
                    {
                        IsPaused = true;
                    }
#endif 
                }
            }

            private void HandleResult(State result)
            {
                // stack ended early, stopped
                if (State == StackState.End)
                {
                    return;
                }

                switch (result)
                {
                    // case where node does not have a return value (usually indicate that it is an flow node)
                    case Amlos.AI.Nodes.State.NONE_RETURN:
                        Result = null;
                        // Looping execution
                        if (callStack.Peek() == Current)
                        {
                            ThrowRecuriveExecution();
                        }
                        //Debug.Log($"{Current.name} add {callStack.Peek().name} to stack");
                        break;
                    case Amlos.AI.Nodes.State.Success:
                        Current.Stop();
                        Pop();
                        //Debug.Log($"{Current.name} return true to {Peek()?.name ?? "STACKBASE"}");
                        State = StackState.Receiving;
                        Result = true;
                        break;
                    case Amlos.AI.Nodes.State.Failed:
                        Current.Stop();
                        Pop();
                        //Debug.Log($"{Current.name} return false to {Peek()?.name ?? "STACKBASE"}");
                        State = StackState.Receiving;
                        Result = false;
                        break;
                    case Amlos.AI.Nodes.State.WaitUntilNextUpdate:
                        Result = null;
                        State = StackState.WaitUntilNextUpdate;
                        break;
                    case Amlos.AI.Nodes.State.Wait:
                        Result = null;
                        State = StackState.Waiting;
                        break;
                    case Amlos.AI.Nodes.State.Error:
                    default:
                        HandleErrorState(result);
                        break;
                }
            }

            private void HandleErrorState(State result)
            {
                Result = null;
                Debug.LogException(new InvalidOperationException($"The node return invalid state. Execution Paused. ({result})({Current?.name})"));
                IsPaused = true;
            }

            private void ThrowRecuriveExecution()
            {
                throw new InvalidOperationException($"The behaviour tree started repeating execution, execution abort. (Did you forget to call TreeNode.End() when node finish execution?) ({State}),({Last.name})");
            }

            /// <summary>
            /// Roll back the entire stack
            /// </summary> 
            private void BreakAll()
            {
                Last = null;
                Current = null;
                while (callStack.Count > 0)
                {
                    RollBack();
                }
                State = StackState.Ready;
            }

            /// <summary>
            /// Roll back the stack to certain point
            /// </summary>
            /// <param name="stopAt"></param>
            public void Break(TreeNode stopAt)
            {
                // stop at is not null and it is not a valid stop point, then it is invalid
                if (stopAt != null && !callStack.Contains(stopAt))
                {
                    throw new InvalidOperationException("Given break point is not on the stack");
                }

                Last = null;
                Current = null;
                while (callStack.Count > 0)
                {
                    if (callStack.Peek() == stopAt) break;
                    RollBack();
                }
                State = StackState.Ready;
            }

            public TreeNode Peek()
            {
                callStack.TryPeek(out var node);
                return node;
            }

            /// <summary>
            /// push a node in the stack
            /// </summary>
            /// <param name="node"></param>
            public void Push(TreeNode node)
            {
                State = StackState.Calling;
                callStack.Push(node);
                //Debug.Log($"Node {node.name} were pushed into stack");
            }

            /// <summary>
            /// remove the first node from the stack
            /// </summary>
            /// <returns></returns>
            public TreeNode Pop()
            {
                if (callStack.TryPop(out var node))
                    OnNodePopStack?.Invoke(node);
                return node;
            }

            /// <summary>
            /// roll back the stack to last node
            /// </summary>
            /// <returns></returns>
            public TreeNode RollBack()
            {
                TreeNode treeNode = Pop();
                treeNode.Stop();
                State = StackState.Calling;
                Current = null;
                Last = null;
                return treeNode;
            }

            /// <summary>
            /// clear the stack
            /// </summary>
            public void Clear()
            {
                callStack.Clear();
            }

            /// <summary>
            /// Update last state to current state and set current state to null
            /// </summary>
            public void MoveState()
            {
                Last = Current;
                Current = null;
            }

            public override string ToString()
            {
                return callStack.Count.ToString();
            }






            internal void Update()
            {
                if (Current is not Nodes.Action action)
                {
                    return;
                }
                try
                {
                    action.Update();
                }
                catch (NodeReturnException ret)
                {
                    ReceiveReturn(ret.ReturnValue);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            internal void FixedUpdate()
            {
                if (Current is not Nodes.Action action)
                {
                    return;
                }
                try
                {
                    action.FixedUpdate();
                }
                catch (NodeReturnException ret)
                {
                    ReceiveReturn(ret.ReturnValue);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            internal void LateUpdate()
            {
                if (Current is not Nodes.Action action)
                {
                    return;
                }
                try
                {
                    action.LateUpdate();
                }
                catch (NodeReturnException ret)
                {
                    ReceiveReturn(ret.ReturnValue);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }



        [Serializable]
        public class ServiceStack : NodeCallStack
        {
            public readonly Service service;

            public ServiceStack(Service service)
            {
                this.service = service;
                //currentFrame = 0;
                callStack = new Stack<TreeNode>();
            }
        }

    }
}
