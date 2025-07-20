using System;
using System.Linq;
using Sirenix.OdinInspector;

namespace LightWeightUI
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class UIPageTagAttribute : Attribute
    {
        public static readonly UIPageTag[] Tags = Enum.GetValues(typeof(UIPageTag)).Cast<UIPageTag>().ToArray();
    }
}