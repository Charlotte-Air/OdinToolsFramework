#if UNITY_EDITOR
using System;
using UnityEngine;
using System.Globalization;
using Sirenix.Utilities.Editor;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;

namespace Framework.LocalizationHelper
{
    public partial class LocalizationConfig
    {
        private void AddNewLanguageConfig()
        {
            var newLanguages = new List<CultureInfo>();
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            foreach (var cultureInfo in cultures)
            {
                if(EnableLanguageInfos.Contains(cultureInfo))
                    continue;
                newLanguages.Add(cultureInfo);
            }
            newLanguages.Sort((a, b) => String.CompareOrdinal(a.GetLanguageDisplayName(), b.GetLanguageDisplayName()));
            GenericSelector<CultureInfo> selector = new GenericSelector<CultureInfo>("添加新语言", true, LocalizationHelper.GetLanguageLongDisplayName, newLanguages);
            selector.CheckboxToggle = true;
            selector.SelectionCancelled += () =>
            {
                foreach (var cultureInfo in selector.GetCurrentSelection())
                {
                    EnableLanguages.Add(cultureInfo.GetLanguageCode());
                }
                EnableLanguages.Sort((a, b) => String.CompareOrdinal(new CultureInfo(a).GetLanguageDisplayName(), new CultureInfo(b).GetLanguageDisplayName()));
                UpdateEnableLanguageInfos();
            };
            selector.ShowInPopup();
        }

        private void DrawCultureInfoListElement(string code)
        {
            foreach (var cultureInfo in EnableLanguageInfos)
            {
                if (cultureInfo.GetLanguageCode() == code)
                {
                    GUILayout.BeginHorizontal();
                    SirenixEditorGUI.BeginBox(GUILayout.MaxWidth(10000));
                    GUILayout.Label(cultureInfo.GetLanguageDisplayName());
                    SirenixEditorGUI.EndBox();
                    SirenixEditorGUI.BeginBox(GUILayout.Width(80));
                    GUILayout.Label(cultureInfo.GetLanguageCode());
                    SirenixEditorGUI.EndBox();
                    GUILayout.EndHorizontal();
                    break;
                }
            }
        }
        
        private bool ValidateFallbackLanguage(string language)
        {
            return EnableLanguages.Contains(language);
        }
    }
}
#endif