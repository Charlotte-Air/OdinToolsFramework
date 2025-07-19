using System;
using System.Diagnostics;

namespace Framework.ConfigHelper
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [Conditional("UNITY_EDITOR")]
    public sealed class ConfigMenuAttribute : Attribute
    {
        /// <summary> 格式：XXX/XXX </summary>
        public string category { get; set; }

        /// <summary> 越小越靠前,没设置ConfigMenu的默认是0 </summary>
        public int order { get; set; }
    }
}