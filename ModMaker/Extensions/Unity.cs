using System;
using System.Collections.Generic;
using System.Text;

namespace ModMaker.Extensions
{
    public static class Unity
    {
        public static bool IsNullOrDestroyed<T>(this T value)
        {
            return value == null || ((value is UnityEngine.Object UnityObj) && UnityObj == null);
        }
    }
}
