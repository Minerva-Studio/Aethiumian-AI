﻿using Amlos.AI.Variables;
using Minerva.Module;
using System;

namespace Amlos.AI.Nodes
{
    [Serializable]
    public sealed class WaitForAnimationEnd : Action
    {
        public enum AnimationState
        {
            current,
            stageName,
        }

        UnityEngine.Animator Animator => behaviourTree.Script.GetComponent<UnityEngine.Animator>();


        public AnimationState animation;
        [DisplayIf(nameof(animation), AnimationState.stageName)] public VariableField<string> stageName;
        int nameHash;


        public override void Awake()
        {
            if (animation == AnimationState.current)
            {
                nameHash = Animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            }
        }


        public override void FixedUpdate()
        {
            if (animation == AnimationState.stageName)
            {
                nameHash = UnityEngine.Animator.StringToHash(stageName);
            }

            if (Animator.GetCurrentAnimatorStateInfo(0).shortNameHash != nameHash)
            {
                End(true);
            }
        }
    }
}