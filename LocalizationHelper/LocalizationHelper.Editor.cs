#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;
using System.Collections.Generic;

namespace Framework.LocalizationHelper
{
    [InitializeOnLoad]
    public static class EditorLanguageSwitch
    {
        static EditorLanguageSwitch()
        {
            ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
        }

        private static bool _isInitialized = false;
        private static string[] _languagesDisplayList;
        private static List<string> _languagesCodeList;
        
        static public void UpdateLanguagesList(bool force = false)
        {
            if (_isInitialized && !force)
                return;
            _isInitialized = true;
            _languagesDisplayList = new string[LocalizationConfig.Instance.EnableLanguages.Count];
            for (int i = 0; i < _languagesDisplayList.Length; i++)
            {
                _languagesDisplayList[i] = LocalizationConfig.Instance.EnableLanguageInfos[i].GetLanguageDisplayName();
            }
            _languagesCodeList = LocalizationConfig.Instance.EnableLanguages;
        }
        
        static void OnToolbarGUI()
        {
            if (Application.isPlaying)
                return;
            
            UpdateLanguagesList();
            var currentLanguageIndex = _languagesCodeList.IndexOf(LocalizationHelper.CurrentLanguageCode);
            EditorGUI.BeginChangeCheck();
            var newLanguageIndex = EditorGUILayout.Popup(currentLanguageIndex, _languagesDisplayList, EditorStyles.toolbarPopup, GUILayout.Width(145));
            if (EditorGUI.EndChangeCheck())
            {
                if (newLanguageIndex != currentLanguageIndex)
                {
                    LocalizationHelper.CurrentLanguage = new System.Globalization.CultureInfo(_languagesCodeList[newLanguageIndex]);
                }
            }
        }
    }
}
#endif