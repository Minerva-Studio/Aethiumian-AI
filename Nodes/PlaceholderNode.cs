﻿using System;

namespace Amlos.AI.Nodes
{
    /// <summary>
    /// a placeholder node used for represent invalid generic node when they are trying to convert to a normal node
    /// </summary>
    [Obsolete("Obsolete due to change of serilization of the nodes")]
    public class PlaceholderNode : TreeNode
    {
        public string originalType;
        public string values;


        /// <summary>
        /// No implementation because it is a placeholder only
        /// </summary>
        /// <exception cref="System.NotImplementedException">always</exception>
        public override State Execute()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// No implementation because it is a placeholder only
        /// </summary>
        /// <exception cref="System.NotImplementedException">always</exception>
        public override void Initialize()
        {
            throw new System.NotImplementedException();
        }
    }

}