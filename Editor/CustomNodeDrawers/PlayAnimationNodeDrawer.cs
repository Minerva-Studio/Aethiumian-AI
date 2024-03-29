﻿using System;
using System.Collections.Generic;
using UnityEditor;

namespace Amlos.AI.Editor
{
    [CustomNodeDrawer(typeof(Nodes.PlayAnimation))]
    public class PlayAnimationNodeDrawer : NodeDrawerBase
    {
        public override void Draw()
        {
            if (!tree.animatorController)
            {
                EditorGUILayout.HelpBox($"Animator of the AI {tree.name} has not yet been assigned", MessageType.Warning);
                return;
            }

            List<string> states = new();
            List<int> stateLayers = new();
            foreach (var layer in tree.animatorController.layers)
            {
                foreach (var item in layer.stateMachine.states)
                {
                    states.Add(layer.name + "." + item.state.name);
                    stateLayers.Add(layer.syncedLayerIndex);
                }
            }

            // no parameter
            if (states.Count == 0)
            {
                EditorGUILayout.HelpBox($"Animator {tree.animatorController.name} has no state", MessageType.Warning);
                return;
            }

            Nodes.PlayAnimation ac = node as Nodes.PlayAnimation;

            var selections = states.ToArray();
            var index = Array.IndexOf(selections, ac.stateName.StringValue);
            index = EditorGUILayout.Popup("State", index, selections);
            if (index != -1)
            {
                ac.stateName.ForceSetConstantValue(selections[index]);
                ac.layer.ForceSetConstantValue(stateLayers[index]);
            }
        }
    }
}
