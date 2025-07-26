using System;
using UnityEngine;
using System.Globalization;
using Framework.ConfigHelper;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace Framework.LocalizationHelper
{
    [Serializable]
    [ConfigMenu(category = "程序/本地化配置")]
    public partial class LocalizationConfig : GameConfig<LocalizationConfig>
    {
        [LabelText("缺省语言"), Tooltip("匹配语言时找不到合适语言的话就使用缺省语言")]
        [Required, ValueDropdown("EnableLanguages"), ValidateInput("ValidateFallbackLanguage", "缺省语言必须是启用语言之一")]
        public string FallbackLanguage;
        
        [LabelText("启用语言"), Tooltip("LanguageCode")]
        [OnValueChanged("UpdateEnableLanguageInfos", true), CustomValueDrawer("DrawCultureInfoListElement")]
        [ListDrawerSettings(ShowFoldout = true, CustomAddFunction = "AddNewLanguageConfig", DraggableItems = false)]
        public List<string> EnableLanguages = new ();

        /// <summary> 所有启用语言的CultureInfo列表，下标和EnableLanguages对应 </summary>
        public List<CultureInfo> EnableLanguageInfos
        {
            get
            {
                if(_languageInfos == null)
                    UpdateEnableLanguageInfos();
                return _languageInfos;
            }
        }

        public CultureInfo FallbackLanguageInfo
        {
            get
            {
                _fallbackLanguageInfo ??= new CultureInfo(FallbackLanguage);
                return _fallbackLanguageInfo;
            }
        }

        [NonSerialized]
        private List<CultureInfo> _languageInfos;
        [NonSerialized]
        private CultureInfo _fallbackLanguageInfo;
        
        private void UpdateEnableLanguageInfos()
        {
            if(_languageInfos == null)
                _languageInfos = new List<CultureInfo>(EnableLanguages.Count);
            else
                _languageInfos.Clear();
            foreach (var language in EnableLanguages)
            {
                _languageInfos.Add(new CultureInfo(language));
            }
#if UNITY_EDITOR
            EditorLanguageSwitch.UpdateLanguagesList(true);
#endif
        }
    }
}