﻿using Amlos.AI.Nodes;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Amlos.AI.Editor
{
    [CustomNodeDrawer(typeof(ScriptCall))]
    [Obsolete]
    public class ScriptCallDrawer : MethodCallerDrawerBase
    {
        public override void Draw()
        {
            var call = (ScriptCall)node;

            if (tree.targetScript == null || !tree.targetScript.GetClass().IsSubclassOf(typeof(Component)))
            {
                EditorGUILayout.LabelField("No target script assigned, please assign a target script");
                return;
            }

            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            methods = GetMethods();
            call.methodName = SelectMethod(call.methodName);
            var method = methods.FirstOrDefault(m => m.Name == call.methodName);
            if (method is null)
            {
                EditorGUILayout.LabelField("Cannot load method info");
                return;
            }

            DrawParameters(call, method);
            DrawResultField(call.result, method);
        }
    }
}