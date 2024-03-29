﻿using System;
using System.Collections.Generic;
using UnityEditor;

namespace Amlos.AI
{
    /// <summary>
    /// An attribute that allow ai editor to display alternative name for the type of node
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AliasAttribute : Attribute
    {
        private static Dictionary<Type, string> aliases = new();
        readonly string alias;

        static AliasAttribute()
        {
#if UNITY_EDITOR 
            foreach (var type in TypeCache.GetTypesWithAttribute<AliasAttribute>())
            {
                var alias = (Attribute.GetCustomAttribute(type, typeof(AliasAttribute)) as AliasAttribute).Alias;
                AliasAttribute.AddEntry(type, alias);
            }
#endif
        }

        /// <summary>
        /// the tip
        /// </summary>
        /// <param name="tip"></param>
        public AliasAttribute(string tip)
        {
            this.alias = tip;
        }

        public string Alias
        {
            get { return alias; }
        }

        public static void AddEntry(Type type, string tip)
        {
            aliases[type] = tip;
        }

        public static string GetEntry(Type type)
        {
            if (aliases.TryGetValue(type, out string tip))
            {
                return tip;
            }
            return string.Empty;
        }
    }
}