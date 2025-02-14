﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BenchmarkDotNet.Extensions
{
    internal static class CommonExtensions
    {
        internal static string WithoutSuffix(this string str, string suffix, StringComparison stringComparison = StringComparison.CurrentCulture)
        {
            return str.EndsWith(suffix, stringComparison) ? str.Substring(0, str.Length - suffix.Length) : str;
        }

        public static IEnumerable<T> TakeLast<T>(this IList<T> source, int count)
        {
            return source.Skip(Math.Max(0, source.Count - count));
        }

        public static int GetStrLength(this long value)
        {
            return value.ToInvariantString().Length;
        }

        public static string ToInvariantString(this long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static int GetStrLength(this int value)
        {
            return value.ToInvariantString().Length;
        }

        public static string ToInvariantString(this int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}