﻿using Amlos.AI.Variables;
using System;
using UnityEngine;

namespace Amlos.AI.Nodes
{
    [NodeTip("A Debug-only node that prints message to the console")]
    [Serializable]
    public sealed class DebugPrint : Call
    {
        public VariableField message;
        public VariableReference<UnityEngine.Object> sender;
        public bool returnValue;

        public override State Execute()
        {
            //AddSelfToProgress();
            Debug.Log(message.Value, sender.GameObjectValue);
            return StateOf(returnValue);
        }

        internal static void Log(string str)
        {
#if UNITY_EDITOR
            Debug.Log(str);
#endif
        }
        internal static void Log(string str, UnityEngine.Object sender)
        {
#if UNITY_EDITOR
            Debug.Log(str, sender);
#endif
        }
    }
}