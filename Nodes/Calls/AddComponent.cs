﻿using Amlos.AI.References;
using System;
using UnityEngine;

namespace Amlos.AI.Nodes
{
    [Serializable]
    [Tooltip("Add a component to the game object")]
    public sealed class AddComponent : Call
    {
        public enum ParentMode
        {
            underSelf,
            underParent,
        }

        public TypeReference<Component> component;
        public ParentMode targetGameObject;

        public override State Execute()
        {
            switch (targetGameObject)
            {
                case ParentMode.underSelf:
                    gameObject.AddComponent(component);
                    break;
                case ParentMode.underParent:
                    if (transform.parent) transform.parent.gameObject.AddComponent(component);
                    else
                    {
                        return State.Failed;
                    }
                    break;
                default:
                    return State.Failed;
            }
            return State.Success;
        }
    }
}