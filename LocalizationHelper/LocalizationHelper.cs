using UnityEngine;
using Framework.Utility;
using System.Globalization;
using Application = UnityEngine.Application;

namespace Framework.LocalizationHelper
{
    public struct CurrentLanguageChangedEvent
    {
        
    }
    
    public static partial class LocalizationHelper
    {
        public static void Init()
        {
            // 主要是为了初始化 LocalizationTmpConfig.Instance.UpdateLanguageCache(_currentLanguageCode);
            var l = CurrentLanguage;
        }
        
        private static CultureInfo _currentLanguageInfo;
        private static string _currentLanguageCode;

        public static string CurrentLanguageCode
        {
            get
            {
                if (string.IsNullOrEmpty(_currentLanguageCode))
                    _currentLanguageCode = CurrentLanguage.GetLanguageCode();
                return _currentLanguageCode;
            }
        }
        public static CultureInfo CurrentLanguage
        {
            get
            {
                if (_currentLanguageInfo != null)
                    return _currentLanguageInfo;
                var currentLanguage = GameSettingsPrefs.GetString("LocalizationHelper.CurrentLanguage", null);
                _currentLanguageInfo = string.IsNullOrEmpty(currentLanguage)
                    ? GetCultureInfoBySystemLanguage()
                    : GetBestLanguage(new CultureInfo(currentLanguage));
                _currentLanguageCode = _currentLanguageInfo.GetLanguageCode();
                LocalizationTmpConfig.Instance.UpdateLanguageCache(_currentLanguageCode);
                return _currentLanguageInfo;
            }
            set
            {
                if(!LocalizationConfig.Instance.EnableLanguageInfos.Contains(value))
                    return;
                if (value.Equals(_currentLanguageInfo))
                    return;
                
                _currentLanguageInfo = value;
                _currentLanguageCode = value.GetLanguageCode();
                GameSettingsPrefs.SetString("LocalizationHelper.CurrentLanguage", value.GetLanguageCode());
                LocalizationTmpConfig.Instance.UpdateLanguageCache(_currentLanguageCode);
                MessageSystem.Send(new CurrentLanguageChangedEvent());
            }
        }

        /// <summary> 根据目标语言从启用语言中找到最合适的 </summary>
        public static CultureInfo GetBestLanguage(CultureInfo targetLanguage)
        {
            CultureInfo bestLanguage = null;
            foreach (var language in LocalizationConfig.Instance.EnableLanguageInfos)
            {
                if (language.Equals(targetLanguage))
                    return language;
                if (language.TwoLetterISOLanguageName == targetLanguage.TwoLetterISOLanguageName)
                    bestLanguage = language;
            }

            if (bestLanguage == null)
                bestLanguage = LocalizationConfig.Instance.FallbackLanguageInfo;
            return bestLanguage;
        }

        public static CultureInfo GetCultureInfoBySystemLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.ChineseTraditional
                    or SystemLanguage.ChineseSimplified
                    or SystemLanguage.Chinese:
                    return CultureInfo.GetCultureInfo("zh-Hant");
                case SystemLanguage.English:
                    return CultureInfo.GetCultureInfo("en");
                case SystemLanguage.French:
                    return CultureInfo.GetCultureInfo("fr");
                case SystemLanguage.German:
                    return CultureInfo.GetCultureInfo("de");
                case SystemLanguage.Indonesian:
                    return CultureInfo.GetCultureInfo("id");
                case SystemLanguage.Italian:
                    return CultureInfo.GetCultureInfo("it");
                case SystemLanguage.Japanese:
                    return CultureInfo.GetCultureInfo("ja");
                case SystemLanguage.Korean:
                    return CultureInfo.GetCultureInfo("ko");
                case SystemLanguage.Portuguese:
                    return CultureInfo.GetCultureInfo("pt");
                case SystemLanguage.Russian:
                    return CultureInfo.GetCultureInfo("ru");
                case SystemLanguage.Spanish:
                    return CultureInfo.GetCultureInfo("es");
                default:
                    return LocalizationConfig.Instance.FallbackLanguageInfo;
            }
        }
        
        public static string GetLanguageCode(this CultureInfo cultureInfo)
        {
            return cultureInfo.Name;
        }
        
        public static string GetLanguageDisplayName(this CultureInfo cultureInfo)
        {
            return cultureInfo.EnglishName;
        }

        public static string GetLanguageLongDisplayName(this CultureInfo cultureInfo)
        {
            return $"{cultureInfo.GetLanguageDisplayName()}({cultureInfo.GetLanguageCode()})";
        }
    }
}