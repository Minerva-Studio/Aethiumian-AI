﻿using Minerva.Module.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 
/// </summary>
namespace Amlos.AI.Editor
{
    /// <summary>
    /// Drawer of variables
    /// 
    /// Author : Wendell Cai
    /// </summary>
    public static class VariableDrawers
    {
        public static void DrawVariable(string labelName, BehaviourTreeData tree, VariableBase variable, VariableType[] possibleTypes = null)
        {
            possibleTypes ??= (VariableType[])Enum.GetValues(typeof(VariableType));

            if (variable.GetType().IsGenericType && variable.GetType().GetGenericTypeDefinition() == typeof(VariableReference<>))
            {
                DrawVariableReference(labelName, tree, variable, possibleTypes);
            }
            else if (variable.GetType() == typeof(VariableReference))
            {
                DrawVariableReference(labelName, tree, variable, possibleTypes);
            }
            else DrawVariableField(labelName, tree, variable, possibleTypes);
        }

        private static void DrawVariableReference(string labelName, BehaviourTreeData tree, VariableBase variable, VariableType[] possibleTypes) => DrawVariableSelection(labelName, tree, variable, possibleTypes);

        private static void DrawVariableField(string labelName, BehaviourTreeData tree, VariableBase variable, VariableType[] possibleTypes)
        {
            if (variable.IsConstant) DrawVariableConstant(labelName, tree, variable, possibleTypes);
            else DrawVariableSelection(labelName, tree, variable, possibleTypes, true);
        }

        private static void DrawVariableConstant(string labelName, BehaviourTreeData tree, VariableBase variable, VariableType[] possibleTypes)
        {
            VariableField f;
            FieldInfo newField;
            switch (variable.Type)
            {
                case VariableType.String:
                    newField = variable.GetType().GetField(nameof(f.stringValue));
                    break;
                case VariableType.Int:
                    newField = variable.GetType().GetField(nameof(f.intValue));
                    break;
                case VariableType.Float:
                    newField = variable.GetType().GetField(nameof(f.floatValue));
                    break;
                case VariableType.Bool:
                    newField = variable.GetType().GetField(nameof(f.boolValue));
                    break;
                case VariableType.Vector2:
                    newField = variable.GetType().GetField(nameof(f.vector2Value));
                    break;
                case VariableType.Vector3:
                    newField = variable.GetType().GetField(nameof(f.vector3Value));
                    break;
                default:
                    newField = null;
                    break;
            }
            GUILayout.BeginHorizontal();
            EditorFieldDrawers.DrawField(labelName, newField, variable);
            if (variable is VariableField vf && variable is not Parameter && vf.IsGeneric && vf.IsConstant)
            {
                variable.Type = (VariableType)EditorGUILayout.EnumPopup(GUIContent.none, variable.Type, CanDisplay, false, EditorStyles.popup, GUILayout.MaxWidth(80));
            }
            if (tree.variables.Count > 0 && GUILayout.Button("Use Variable", GUILayout.MaxWidth(100)))
            {
                variable.SetReference(tree.variables[0]);
            }
            GUILayout.EndHorizontal();
            bool CanDisplay(Enum val)
            {
                return Array.IndexOf(possibleTypes, val) != -1;
            }
        }

        private static void DrawVariableSelection(string labelName, BehaviourTreeData tree, VariableBase variable, VariableType[] possibleTypes, bool allowConvertToConstant = false)
        {
            GUILayout.BeginHorizontal();

            string[] list;
            IEnumerable<VariableData> vars =
            variable.IsGeneric
                ? tree.variables.Where(v => Array.IndexOf(possibleTypes, v.type) != -1)
                : tree.variables.Where(v => v.type == variable.Type && Array.IndexOf(possibleTypes, v.type) != -1);
            ;
            list = vars.Select(v => v.name).Append("Create New...").ToArray();

            if (list.Length < 2)
            {
                EditorGUILayout.LabelField(labelName, "No valid variable found");
                if (GUILayout.Button("Create New", GUILayout.MaxWidth(80))) variable.SetReference(tree.CreateNewVariable(variable.Type));
            }
            else
            {
                string variableName = tree.GetVariable(variable.UUID)?.name ?? "";
                if (Array.IndexOf(list, variableName) == -1)
                {
                    variableName = list[0];
                }
                int selectedIndex = Array.IndexOf(list, variableName);
                if (selectedIndex < 0)
                {
                    if (!variable.HasReference)
                    {
                        EditorGUILayout.LabelField(labelName, $"No Variable");
                        if (GUILayout.Button("Create", GUILayout.MaxWidth(80))) variable.SetReference(tree.CreateNewVariable(variable.Type));
                    }
                    else
                    {
                        EditorGUILayout.LabelField(labelName, $"Variable {variableName} not found");
                        if (GUILayout.Button("Recreate", GUILayout.MaxWidth(80))) variable.SetReference(tree.CreateNewVariable(variable.Type, variableName));
                        if (GUILayout.Button("Clear", GUILayout.MaxWidth(80))) variable.SetReference(null);
                    }
                }
                else
                {
                    int currentIndex = EditorGUILayout.Popup(labelName, selectedIndex, list, GUILayout.MinWidth(400));
                    if (currentIndex < 0) { currentIndex = 0; }
                    //using existing var
                    if (currentIndex != list.Length - 1)
                    {
                        string varName = list[currentIndex];
                        var a = tree.GetVariable(varName);
                        variable.SetReference(a);
                    }
                    //Create new var
                    else
                    {
                        tree.CreateNewVariable(variable.Constant);
                    }
                }
            }


            if (variable.IsGeneric && variable.IsConstant)
            {
                variable.Type = (VariableType)EditorGUILayout.EnumPopup(GUIContent.none, variable.Type, CanDisplay, false, EditorStyles.popup, GUILayout.MaxWidth(80));
            }

            if (allowConvertToConstant)
            {
                if (GUILayout.Button("Set Constant", GUILayout.MaxWidth(100)))
                {
                    variable.SetReference(null);
                }
            }
            else
            {
                EditorGUILayout.LabelField("         ", GUILayout.MaxWidth(100));
            }
            GUILayout.EndHorizontal();


            bool CanDisplay(Enum val)
            {
                return Array.IndexOf(possibleTypes, val) != -1;
            }
        }
    }
}
