using System;

namespace Framework.Utility.Extensions
{
    public static class StringExtensions
    {
        /// <summary> 将 Assets/Resources/GameConfigs/{typeof(T).Name}.** 路径转换成 ResourcesPath </summary>
        public static string ToResourcesPath(this string assetPath)
        {
            const int resourcesStrLen = 10;
            var indexOf = assetPath.IndexOf("Resources/", StringComparison.OrdinalIgnoreCase);
            var lastIndexOf = assetPath.LastIndexOf(".", StringComparison.OrdinalIgnoreCase);

            if (indexOf != -1 && lastIndexOf != -1)
                return assetPath.Substring(indexOf + resourcesStrLen, lastIndexOf - indexOf - resourcesStrLen);

            return null;
        }
    }
}