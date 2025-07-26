#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Framework.LocalizationHelper
{
    public partial class LocalizationTMP : MonoBehaviour
    {
        [MenuItem("CONTEXT/Component/本地化TMP")]
        private static void LocalizeTMP(MenuCommand command)
        {
            var component = command.context as TextMeshProUGUI;
            if (component == null)
                return;
            var localizationTMP = component.gameObject.GetComponent<LocalizationTMP>();
            if (localizationTMP == null)
                component.gameObject.AddComponent<LocalizationTMP>();
        }
        
        [MenuItem("CONTEXT/Component/本地化TMP", true)]
        private static bool ValidateLocalizeTMP(MenuCommand command)
        {
            var component = command.context as TextMeshProUGUI;
            return component != null && component.gameObject.GetComponent<LocalizationTMP>() == null;
        }
        
        [MenuItem("CONTEXT/Component/本地化TMP(不使用IDT)")]
        private static void LocalizeTMPNotUseIDT(MenuCommand command)
        {
            var component = command.context as TextMeshProUGUI;
            if (component == null)
                return;
            var localizationTMP = component.gameObject.GetComponent<LocalizationTMP>();
            if (localizationTMP == null)
                localizationTMP = component.gameObject.AddComponent<LocalizationTMP>();
            localizationTMP.UseIDT = false;
        }
        
        [MenuItem("CONTEXT/Component/本地化TMP(不使用IDT)", true)]
        private static bool ValidateLocalizeTMPNotUseIDT(MenuCommand command)
        {
            var component = command.context as TextMeshProUGUI;
            return component != null && component.gameObject.GetComponent<LocalizationTMP>() == null;
        }
        
        [MenuItem("CONTEXT/Component/本地化TMP(强制单语言)")]
        private static void LocalizeTMPForceSingleLanguage(MenuCommand command)
        {
            var component = command.context as TextMeshProUGUI;
            if (component == null)
                return;
            var localizationTMP = component.gameObject.GetComponent<LocalizationTMP>();
            if (localizationTMP == null)
                localizationTMP = component.gameObject.AddComponent<LocalizationTMP>();
            localizationTMP.IsForceUseLanguage = true;
            localizationTMP.ForceUseLanguage = LocalizationConfig.Instance.FallbackLanguage;
        }
        
        [MenuItem("CONTEXT/Component/本地化TMP(强制单语言)", true)]
        private static bool ValidateLocalizeTMPForceSingleLanguage(MenuCommand command)
        {
            var component = command.context as TextMeshProUGUI;
            return component != null && component.gameObject.GetComponent<LocalizationTMP>() == null;
        }
        
        [MenuItem("CONTEXT/Component/本地化TMP(强制单语言)(不使用IDT)")]
        private static void LocalizeTMPForceSingleLanguageNotUseIDT(MenuCommand command)
        {
            var component = command.context as TextMeshProUGUI;
            if (component == null)
                return;
            var localizationTMP = component.gameObject.GetComponent<LocalizationTMP>();
            if (localizationTMP == null)
                localizationTMP = component.gameObject.AddComponent<LocalizationTMP>();
            localizationTMP.UseIDT = false;
            localizationTMP.IsForceUseLanguage = true;
            localizationTMP.ForceUseLanguage = LocalizationConfig.Instance.FallbackLanguage;
        }
        
        [MenuItem("CONTEXT/Component/本地化TMP(强制单语言)(不使用IDT)", true)]
        private static bool ValidateLocalizeTMPForceSingleLanguageNotUseIDT(MenuCommand command)
        {
            var component = command.context as TextMeshProUGUI;
            return component != null && component.gameObject.GetComponent<LocalizationTMP>() == null;
        }
    }
}
#endif