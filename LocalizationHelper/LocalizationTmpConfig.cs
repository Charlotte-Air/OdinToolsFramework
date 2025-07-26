using TMPro;
using System;
using System.IO;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;
using Framework.ConfigHelper;
using System.Collections.Generic;
using Framework.Utility.WeakReference;

namespace Framework.LocalizationHelper
{
    [Serializable]
    [ConfigMenu(category = "程序/本地化配置/TMP字体")]
    public partial class LocalizationTmpConfig : GameConfig<LocalizationTmpConfig>
    {
        [LabelText("Tmp字体输出路径"), FolderPath]
        public string TmpFontOutputPath;
        
        [LabelText("快速生成模式"), Tooltip("影响图集排布，效率模式空间利用率会更高")]
        public bool FastMode;
        
        [Space(10)]
        [LabelText("字体类型")]
        [DictionaryDrawerSettings(KeyLabel = "字体类型名称", ValueLabel = "字体类型信息", KeyColumnWidth = 70)]
        public SortedDictionary<string, FontType> FontTypes = new();
        
#if UNITY_EDITOR
        [Button("更新[Tmp字体图集生成设置]"), PropertyOrder(-1)]
        private void UpdateTmpFontSettingsButton() => UpdateTmpFontGenerateSetting();
#endif
        
        [LabelText("Tmp字体图集生成设置")]
        [ListDrawerSettings(DraggableItems = false, HideAddButton = true, HideRemoveButton = true, DefaultExpandedState = false, ShowPaging = true, NumberOfItemsPerPage = 2)]
        public List<TmpFontGenerateSetting> TmpFontSettings = new();
 
#if UNITY_EDITOR
        public TmpFontGenerateSetting GetOrAddTmpFontInfo(string languageCode, AssetPathRef sourceFont)
        {
            if(string.IsNullOrEmpty(sourceFont.AssetGuidStr))
                return null;
            foreach (var tmpFontSetting in TmpFontSettings)
            {
                if (tmpFontSetting.SourceFont.AssetGuidStr == sourceFont.AssetGuidStr && tmpFontSetting.LanguageCode == languageCode)
                    return tmpFontSetting;
            }
            var newTmpFontSetting = new TmpFontGenerateSetting
            {
                LanguageCode = languageCode,
                SourceFont = sourceFont
            };
            TmpFontSettings.Add(newTmpFontSetting);
            UnityEditor.EditorUtility.SetDirty(this);
            return newTmpFontSetting;
        }
#endif
        
        public void UpdateLanguageCache(string languageCode)
        {
            foreach (var fontType in FontTypes.Values)
            {
                if (fontType.IsLanguageFont)
                {
                    var tmpFontSetting = fontType.LanguageTmpFontGenerateSettings.GetValueOrDefault(languageCode);
                    if (tmpFontSetting == null)
                        continue;
                    foreach (var fontStyle in fontType.FontStyles.Values)
                    {
                        fontStyle.UpdateCurrentLanguageCache(languageCode, tmpFontSetting);
                    }
                }
                else
                {
                    var tmpFontSetting = fontType.DefaultTmpFontGenerateSetting;
                    foreach (var fontStyle in fontType.FontStyles.Values)
                    {
                        fontStyle.UpdateCurrentLanguageCache(languageCode, tmpFontSetting);
                    }
                }
            }
        }
        
#if UNITY_EDITOR
        private void UpdateTmpFontGenerateSetting()
        {
            var enableLanguageCodeList = LocalizationConfig.Instance.EnableLanguages;
            var useTmpFontSettings = new List<TmpFontGenerateSetting>();
            foreach (var fontType in FontTypes.Values)
            {
                fontType.DefaultTmpFontGenerateSetting = null;
                fontType.LanguageTmpFontGenerateSettings.Clear();
                if (fontType.IsLanguageFont)
                {
                    foreach (var pair in fontType.LanguageFontAssets)
                    {
                        var languageCode = pair.Key;
                        var fontAssetPath = pair.Value;
                        if(!enableLanguageCodeList.Contains(languageCode))
                            continue;
                        var tmpFontSetting = GetOrAddTmpFontInfo(languageCode, fontAssetPath);
                        fontType.LanguageTmpFontGenerateSettings[languageCode] = tmpFontSetting;
                        useTmpFontSettings.Add(tmpFontSetting);
                    }
                }
                else
                {
                    var tmpFontSetting = GetOrAddTmpFontInfo("", fontType.DefaultFontAsset);
                    fontType.DefaultTmpFontGenerateSetting = tmpFontSetting;
                    useTmpFontSettings.Add(tmpFontSetting);
                }
            }
            
            // 删除TmpFontSettings中不使用的设置
            for (int i = TmpFontSettings.Count - 1; i >= 0; i--)
            {
                var tmpFontSetting = TmpFontSettings[i];
                if (!useTmpFontSettings.Contains(tmpFontSetting))
                {
                    TmpFontSettings.RemoveAt(i);
                }
            }
            
            TmpFontSettings.Sort((a, b) => string.Compare(a.LanguageCode, b.LanguageCode, StringComparison.Ordinal));

            UpdateLanguageCache(LocalizationHelper.CurrentLanguageCode);
        }
#endif
    }
    
    [Serializable, HideReferenceObjectPicker]
    public class FontType
    {
        [LabelText("多语言字体")]
        public bool IsLanguageFont;
        
#if UNITY_EDITOR
        [LabelText("默认字体资产(TTF)"), HideIf("IsLanguageFont")]
        public AssetPathRef DefaultFontAsset;
        
        [LabelText("多语言字体资产(TTF)"), ShowIf("IsLanguageFont")]
        [DictionaryDrawerSettings(KeyLabel = "语言", ValueLabel = "字体资产", KeyColumnWidth = 50)]
        public SortedDictionary<string/*LanguageCode*/, AssetPathRef> LanguageFontAssets = new();
        
        [Button("添加启用语言字体资产设置"), ShowIf("IsLanguageFont")]
        private void AddLanguageFontAsset()
        {
            foreach (var languageCode in LocalizationConfig.Instance.EnableLanguages)
            {
                if (LanguageFontAssets.ContainsKey(languageCode))
                    continue;
                LanguageFontAssets.Add(languageCode, new AssetPathRef());
            }
        }
#endif
        
        [Space(10)]
        [LabelText("字体样式")]
        [DictionaryDrawerSettings(KeyLabel = "样式名称", ValueLabel = "样式信息")]
        public SortedDictionary<string, FontStyle> FontStyles = new();
        
        [HideInInspector, SerializeField]
        public TmpFontGenerateSetting DefaultTmpFontGenerateSetting;
        
        [HideInInspector, SerializeField]
        public Dictionary<string/*LanguageCode*/, TmpFontGenerateSetting> LanguageTmpFontGenerateSettings = new();
        
        public AssetPathRef? GetMaterial(string languageCode, UseMaterialType useMaterialType)
        {
            var tmpFontSetting = IsLanguageFont ? LanguageTmpFontGenerateSettings.GetValueOrDefault(languageCode) : DefaultTmpFontGenerateSetting;
            if (tmpFontSetting == null)
                return null;
            var material = useMaterialType == UseMaterialType.Outline ? tmpFontSetting.TmpFontOutlineMaterial : tmpFontSetting.TmpFontShadowMaterial;
#if UNITY_EDITOR
            if(!material.IsValid())
                return null;
#endif
            return material;
        }
        
        public AssetPathRef? GetTmpFontAsset(string languageCode)
        {
            var tmpFontSetting = IsLanguageFont ? LanguageTmpFontGenerateSettings.GetValueOrDefault(languageCode) : DefaultTmpFontGenerateSetting;
            if (tmpFontSetting == null)
                return null;
#if UNITY_EDITOR
            if(!tmpFontSetting.TmpFontAsset.IsValid())
                return null;
#endif
            return tmpFontSetting.TmpFontAsset;
        }
    }

    public enum UseMaterialType
    {
        [InspectorName("描边")]
        Outline,
            
        [InspectorName("阴影")]
        Shadow
    }
    
    [Serializable, HideReferenceObjectPicker]
    public class FontStyle
    {
        [LabelText("默认材质类型")]
        public UseMaterialType DefaultMaterialType;
        
        [LabelText("多语言材质类型")]
        [DictionaryDrawerSettings(KeyLabel = "语言", ValueLabel = "材质类型")]
        public SortedDictionary<string/*LanguageCode*/, UseMaterialType> LanguageMaterialTypes = new();
        
        [Space(10)]
        [LabelText("默认材质设置预设")]
        public TMP_TextCustomMaterialPreset DefaultMaterialSettingPreset;
        
        [LabelText("多语言材质设置预设")]
        [DictionaryDrawerSettings(KeyLabel = "语言", ValueLabel = "材质设置预设")]
        public SortedDictionary<string/*LanguageCode*/, TMP_TextCustomMaterialPreset> LanguageMaterialSettingPresets = new();
        
        [Space(10)]
        [FoldoutGroup("运行时缓存", false), LabelText("当前语言材质设置预设"), NonSerialized, ShowInInspector]
        public TMP_TextCustomMaterialPreset CurrentMaterialSettingPreset;
        
        [FoldoutGroup("运行时缓存"), LabelText("当前语言材质"), NonSerialized, ShowInInspector]
        public AssetPathRef? CurrentMaterial;
        
        [FoldoutGroup("运行时缓存"), LabelText("当前语言Tmp字体"), NonSerialized, ShowInInspector]
        public AssetPathRef? CurrentTmpFontAsset;
        
        public UseMaterialType GetMaterialType(string languageCode)
        {
            return string.IsNullOrEmpty(languageCode) ? DefaultMaterialType : LanguageMaterialTypes.GetValueOrDefault(languageCode, DefaultMaterialType);
        }
        
        public TMP_TextCustomMaterialPreset GetMaterialSettingPreset(string languageCode)
        {
            return string.IsNullOrEmpty(languageCode) ? DefaultMaterialSettingPreset : LanguageMaterialSettingPresets.GetValueOrDefault(languageCode, DefaultMaterialSettingPreset);
        }
        
        public void UpdateCurrentLanguageCache(string languageCode, TmpFontGenerateSetting tmpFontGenerateSetting)
        {
            var currentMaterialType = GetMaterialType(languageCode);
            CurrentMaterial = currentMaterialType == UseMaterialType.Outline ? tmpFontGenerateSetting.TmpFontOutlineMaterial : tmpFontGenerateSetting.TmpFontShadowMaterial;
            CurrentMaterialSettingPreset = GetMaterialSettingPreset(languageCode);
            CurrentTmpFontAsset = tmpFontGenerateSetting.TmpFontAsset;
            
            
            if(!CurrentMaterial.Value.IsValid())
                CurrentMaterial = null;
            if(!CurrentTmpFontAsset.Value.IsValid())
                CurrentTmpFontAsset = null;
        }
    }

    [Serializable, HideReferenceObjectPicker]
    public class TmpFontGenerateSetting
    {
        [LabelText("所属语言"), Tooltip("LanguageCode"), ReadOnly]
        public string LanguageCode = "";
        
#if UNITY_EDITOR
        [LabelText("字体资产"), ReadOnly]
        public AssetPathRef SourceFont;
        
        public string FontAssetName => string.IsNullOrEmpty(LanguageCode) ? $"{Path.GetFileNameWithoutExtension(SourceFont.MainAssetPath)}" : $"{LanguageCode}_{Path.GetFileNameWithoutExtension(SourceFont.MainAssetPath)}";

        [LabelText("Padding")]
        public int Padding = 3;
        
        public int PointSize = 32;
        
        [LabelText("额外字符集"), Multiline]
        public string CharacterSet = @" !""'()*+,-./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";
        
        [LabelText("自动收集的字符集"), Multiline, ReadOnly]
        public string AutoCharacterSet;

        [LabelText("自动收集的字符集数量"), ShowInInspector]
        public int AutoCharacterSetNum => AutoCharacterSet?.Length ?? 0;
        
        public string FinalCharacterSet 
        {
            get
            {
                var f = CharacterSet.ToCharArray().Union(AutoCharacterSet.ToCharArray()).ToList();
                f.Sort();
                return new string(f.ToArray());
            }
        }
#endif
        
        [LabelText("Tmp字体描边材质"), ReadOnly]
        public AssetPathRef TmpFontOutlineMaterial;
        
        [LabelText("Tmp字体阴影材质"), ReadOnly]
        public AssetPathRef TmpFontShadowMaterial;
        
        [LabelText("TMP字体资产"), ReadOnly]
        public AssetPathRef TmpFontAsset;
#if UNITY_EDITOR
        static string[] fontResolutionLabels_str = { "8", "16", "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" };
        static int[] fontAtlasResolutions = { 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
        private Vector2Int DrawAtlasSize(Vector2Int value)
        {
            value.x = UnityEditor.EditorGUILayout.IntPopup("字体设置_图集宽度:", value.x, fontResolutionLabels_str, fontAtlasResolutions);
            value.y = UnityEditor.EditorGUILayout.IntPopup("字体设置_图集高度:", value.y, fontResolutionLabels_str, fontAtlasResolutions);
            
            return value;
        }
#endif
    }
}