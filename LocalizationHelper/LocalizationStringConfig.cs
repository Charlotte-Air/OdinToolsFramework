using System;
using UnityEngine;
using Framework.ConfigHelper;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;

namespace Framework.LocalizationHelper
{
    [Serializable]
    [PreferBinarySerialization]
    [ConfigMenu(category = "程序/本地化配置/字符串")]
    public partial class LocalizationStringConfig : GameConfig<LocalizationStringConfig>
    {
        [LabelText("本地化表路径"), Tooltip("相对于Asset目录，但是文件在Asset目录外面"), FilePath(Extensions = "csv", IncludeFileExtension = true)]
        public string DataTablePath;
        
        [LabelText("本地化表每个语言的下标"), Tooltip("主要是用于查询加速"), ReadOnly]
        public Dictionary<string, int> LanguageCodeIndexMap = new();
        
        [LabelText("本地化表数据"), ReadOnly, OdinSerialize]
        [Searchable]
        public Dictionary<string, List<string>> StringMap = new ();

        public string GetCurrentText(string key, string defaultValue = null)
        {
            var currentLanguageCode = LocalizationHelper.CurrentLanguageCode;
            return GetText(key, currentLanguageCode, defaultValue);
        }
        
        public string GetText(string key, string languageCode, string defaultValue = null)
        {
            if(string.IsNullOrEmpty(key) || string.IsNullOrEmpty(languageCode))
                return defaultValue;
            if (!StringMap.TryGetValue(key, out var localizationStringList))
                return defaultValue;
            if (!LanguageCodeIndexMap.TryGetValue(languageCode, out var index))
                return defaultValue;
            if (index >= localizationStringList.Count)
                return defaultValue;
            var text = localizationStringList[index];
            return string.IsNullOrEmpty(text) ? defaultValue : text;
        }

        /// <summary> 获取指定语言的字符集 </summary>
        public string GetCharacterSet(string languageCode)
        {
            if (!LanguageCodeIndexMap.TryGetValue(languageCode, out var index))
                return "";

            var outCharacterSet = "";
            foreach (var stringList in StringMap.Values)
            {
                var text = stringList[index];
                foreach (var character in text)
                {
                    if (!outCharacterSet.Contains(character))
                        outCharacterSet += character;
                }
            }
            return outCharacterSet;
        }
    }
}