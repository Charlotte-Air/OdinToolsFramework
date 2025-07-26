using TMPro;
using UnityEngine;
using Framework.Utility;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Framework.Utility.WeakReference;

namespace Framework.LocalizationHelper
{
    /// <summary> 控制TextMeshProUGUI字体组件的本地化 </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    [AddComponentMenu("本地化/LocalizationTMP")]
    public partial class LocalizationTMP : MonoBehaviour
    {
        [LabelText("忽略本地化的材质参数预设")]
        public bool IgnoreMaterialPreset;
        
        [LabelText("使用IDT")]
        public bool UseIDT = true;
        
        [LabelText("IDT"), ValueDropdown("GetIDTList"), ValidateInput("ValidateIDT", "IDT无效"), OnValueChanged("UpdateLocalization"), ShowIf("UseIDT")]
        public string IDT;
        
        [LabelText("字体类型"), ValueDropdown("GetFontTypeList"), ValidateInput("ValidateFontType", "字体类型无效")]
        public string FontTypeName;
        
        [LabelText("字体样式"), ValueDropdown("GetFontStyleList"), ValidateInput("ValidateFontStyle", "字体样式无效"), OnValueChanged("UpdateLocalization")]
        public string FontStyleName;
        
        [LabelText("强制使用单语言"), Tooltip("有些数字可能不需要多语言，只用只用英文效果")]
        public bool IsForceUseLanguage;
        
        [LabelText("指定单语言"), ShowIf("IsForceUseLanguage")]
        [ValueDropdown("GetLanguages"), ValidateInput("ValidateLanguage", "语言无效"), OnValueChanged("UpdateLocalization")]
        public string ForceUseLanguage;
        
        private TextMeshProUGUI _textMeshProUGUI;
        private LocalizationStringConfig _localizationStringConfig;
        private LocalizationTmpConfig _localizationTmpConfig;
        private TMP_TextCustomMaterialPreset _tmpTextCustomMaterialPreset;
        private bool _needUpdateText;
        private TMP_FontAsset _fontAsset;
        private Material _fontMaterial;
        private bool _waitTmpFont;
        private bool _waitTmpFontMaterial;
        private bool _updateLocalization;
        
        private void Awake()
        {
            _textMeshProUGUI = GetComponent<TextMeshProUGUI>();
            _localizationStringConfig = LocalizationStringConfig.Instance;
            _localizationTmpConfig = LocalizationTmpConfig.Instance;
        }
        
        private void OnEnable()
        {
            UpdateLocalization();
            MessageSystem.Register<CurrentLanguageChangedEvent>(OnCurrentLanguageChangedEvent);
        }
        
        private void OnDisable()
        {
            MessageSystem.UnRegister<CurrentLanguageChangedEvent>(OnCurrentLanguageChangedEvent);
        }

        private void OnCurrentLanguageChangedEvent(CurrentLanguageChangedEvent obj)
        {
            UpdateLocalization();
        }

        public string GetText()
        {
            return IsForceUseLanguage ? _localizationStringConfig.GetText(IDT, ForceUseLanguage) : _localizationStringConfig.GetCurrentText(IDT);
        }

        public AssetPathRef? GetMaterialPath(FontType fontType, FontStyle fontStyle)
        {
            if (IsForceUseLanguage)
            {
                var materialType = fontStyle.GetMaterialType(ForceUseLanguage);
                return fontType.GetMaterial(ForceUseLanguage, materialType);
            }
            return fontStyle.CurrentMaterial;
        }
        
        public TMP_TextCustomMaterialPreset GetMaterialSettingPreset(FontStyle fontStyle)
        {
            if (IsForceUseLanguage)
            {
                return fontStyle.GetMaterialSettingPreset(ForceUseLanguage);
            }
            return fontStyle.CurrentMaterialSettingPreset;
        }
        
        public AssetPathRef? GetFontAsset(FontType fontType, FontStyle fontStyle)
        {
            if (IsForceUseLanguage)
            {
                return fontType.GetTmpFontAsset(ForceUseLanguage);
            }
            return fontStyle.CurrentTmpFontAsset;
        }
        
        public void UpdateLocalization()
        {
            if(!isActiveAndEnabled)
                return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (!string.IsNullOrEmpty(FontTypeName) && _localizationTmpConfig.FontTypes.TryGetValue(FontTypeName, out var fontType))
                {
                    if(!string.IsNullOrEmpty(FontStyleName) && fontType.FontStyles.TryGetValue(FontStyleName, out var fontStyle))
                    {
                        if(UseIDT)
                            _textMeshProUGUI.text = GetText();
                        
                        var tmpMaterialPath = GetMaterialPath(fontType, fontStyle);
                        if(tmpMaterialPath.HasValue)
                            _textMeshProUGUI.fontMaterial = tmpMaterialPath.Value.EditorLoad<Material>();
                        
                        if (!IgnoreMaterialPreset)
                        {
                            var tmpMaterialSettingPreset = GetMaterialSettingPreset(fontStyle);
                            _textMeshProUGUI.CustomMaterialPreset = tmpMaterialSettingPreset;
                        }
                        
                        var tmpFontAsset = GetFontAsset(fontType, fontStyle);
                        if(tmpFontAsset.HasValue)
                            _textMeshProUGUI.font = tmpFontAsset.Value.EditorLoad<TMP_FontAsset>();
                        
                        _textMeshProUGUI.UpdateFontAsset();
                    }
                }
            }
            else
#endif
            {
                _needUpdateText = true;
                if (!string.IsNullOrEmpty(FontTypeName) && _localizationTmpConfig.FontTypes.TryGetValue(FontTypeName, out var fontType))
                {
                    if (!string.IsNullOrEmpty(FontStyleName) && fontType.FontStyles.TryGetValue(FontStyleName, out var fontStyle))
                    {
                        _updateLocalization = true;
                        var tmpFontAsset = GetFontAsset(fontType, fontStyle);
                        if (tmpFontAsset.HasValue)
                        {
                            _waitTmpFont = true;
                            var fontAsset = tmpFontAsset.Value.FindGameAsset<TMP_FontAsset>();
                            _fontAsset = fontAsset;
                            _waitTmpFont = false;
                            if(!_updateLocalization)
                                UpdateText(fontStyle);
                        }
                        var tmpMaterialPath = GetMaterialPath(fontType, fontStyle);
                        if (tmpMaterialPath.HasValue)
                        {
                            _waitTmpFontMaterial = true;
                            var material = tmpMaterialPath.Value.FindGameAsset<Material>();
                            _fontMaterial = material;
                            _waitTmpFontMaterial = false;
                            if(!_updateLocalization)
                                UpdateText(fontStyle);
                        }
                        UpdateText(fontStyle);
                        _textMeshProUGUI.UpdateFontAsset();
                        _updateLocalization = false;
                    }
                }
            }
        }

        private void UpdateText(FontStyle fontStyle)
        {
            if(_waitTmpFont || _waitTmpFontMaterial || !_needUpdateText)
                return;
            if (!IgnoreMaterialPreset)
            {
                var tmpMaterialSettingPreset = GetMaterialSettingPreset(fontStyle);
                _textMeshProUGUI.CustomMaterialPreset = tmpMaterialSettingPreset;
            }
            _textMeshProUGUI.font = _fontAsset;
            _textMeshProUGUI.fontMaterial = _fontMaterial;
            if(UseIDT)
                _textMeshProUGUI.text = GetText();
            _needUpdateText = false;
        }

#if UNITY_EDITOR
        private IEnumerable<string> GetIDTList()
        {
            return LocalizationStringConfig.Instance.StringMap.Keys;
        }
        
        private IEnumerable<string> GetFontTypeList()
        {
            return LocalizationTmpConfig.Instance.FontTypes.Keys;
        }
        
        private IEnumerable<string> GetFontStyleList()
        {
            if(string.IsNullOrEmpty(FontTypeName))
                return null;
            if(!LocalizationTmpConfig.Instance.FontTypes.TryGetValue(FontTypeName, out var fontType))
                return null;
            return fontType.FontStyles.Keys;
        }
        
        private bool ValidateIDT(string idt)
        {
            if(string.IsNullOrEmpty(idt))
                return false;
            return LocalizationStringConfig.Instance.StringMap.ContainsKey(idt);
        }
        
        private bool ValidateFontType(string fontType)
        {
            if(string.IsNullOrEmpty(fontType))
                return false;
            return LocalizationTmpConfig.Instance.FontTypes.ContainsKey(fontType);
        }
        
        private bool ValidateFontStyle(string fontStyle)
        {
            if(string.IsNullOrEmpty(fontStyle))
                return false;
            if(string.IsNullOrEmpty(FontTypeName))
                return false;
            if(!LocalizationTmpConfig.Instance.FontTypes.TryGetValue(FontTypeName, out var fontType))
                return false;
            return fontType.FontStyles.ContainsKey(fontStyle);
        }
        
        private IEnumerable<string> GetLanguages()
        {
            return LocalizationConfig.Instance.EnableLanguages;
        }
        
        private bool ValidateLanguage(string language)
        {
            if(string.IsNullOrEmpty(language))
                return false;
            return LocalizationConfig.Instance.EnableLanguages.Contains(language);
        }
#endif
    }
}