﻿using System;

namespace BenchmarkDotNet
{
    /// <summary>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ParamsAttribute : Attribute
    {
        public int[] Args { get; private set; }

        public ParamsAttribute(params int[] args)
        {
            Args = args;
        }
    }
}
