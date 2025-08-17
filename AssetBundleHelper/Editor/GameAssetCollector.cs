using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Framework.AssetBundleHelper.Editor
{
    /// <summary>
    /// 游戏资产收集器
    /// </summary>
    [Serializable, HideReferenceObjectPicker, InlineProperty]
    public class GameAssetCollector
    {
        [HorizontalGroup, HideLabel, Required]
        public Object AssetOrFolder;
        
        [HorizontalGroup(220)]
        [LabelText("过滤规则")]
        [ListDrawerSettings(DraggableItems = false, ShowPaging = false, ShowFoldout = false)]
        public List<FilterRule> FilterRules = new();
        
        [HorizontalGroup(300)]
        [LabelText("别名规则")]
        [ListDrawerSettings(DraggableItems = false, ShowPaging = false, ShowFoldout = false)]
        public List<AliaseRule> AliaseRules = new();
        
        public bool IsValid => AssetOrFolder != null;
        public bool IsFolder => IsValid && UnityEditor.AssetDatabase.IsValidFolder(AssetPath);
        public bool IsAsset => IsValid && !IsFolder;
        public string AssetPath => IsValid ? UnityEditor.AssetDatabase.GetAssetPath(AssetOrFolder) : null;
        
        public void Collect(GameAssetPackage gameAssetPackage, ManifestContext context)
        {
            if (!IsValid)
            {
                Debug.LogWarning("Invalid GameAssetCollector");
                return;
            }
            
            if (IsFolder)
            {
                CollectFolder(gameAssetPackage, AssetPath, context);
            }
            else if (IsAsset)
            {
                CollectAsset(gameAssetPackage, AssetOrFolder, context);
            }
        }
        
        private void CollectFolder(GameAssetPackage gameAssetPackage, string folderPath, ManifestContext context)
        {
            var assetGuidList = UnityEditor.AssetDatabase.FindAssets("", new[] {folderPath});
            var assetPaths = new string[assetGuidList.Length];
            for (var i = 0; i < assetGuidList.Length; i++)
            {
                assetPaths[i] = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuidList[i]);
            }
            foreach (var assetPath in assetPaths)
            {
                if(UnityEditor.AssetDatabase.IsValidFolder(assetPath)) // 排除文件夹
                    continue;
                
                var isNeedCollect = true;
                foreach (var filterRule in gameAssetPackage.FilterRules)
                {
                    if (!filterRule.IsCollectAsset(context, gameAssetPackage, assetPath))
                    {
                        isNeedCollect = false;
                        break;
                    }
                }
                if (!isNeedCollect)
                    continue;
                foreach (var filterRule in FilterRules)
                {
                    if (!filterRule.IsCollectAsset(context, gameAssetPackage, assetPath))
                    {
                        isNeedCollect = false;
                        break;
                    }
                }
                if (!isNeedCollect)
                    continue;

                using (ListPool<Object>.Get(out var assetList))
                {
                    AssetBundleUtility.GetAssetList(assetPath, assetList);
                    foreach (var asset in assetList)
                    {
                        CollectAsset(gameAssetPackage, asset, context);
                    }
                }
            }
        }
        
        private void CollectAsset(GameAssetPackage gameAssetPackage, Object asset, ManifestContext context)
        {
            CollectAsset(gameAssetPackage, UnityEditor.AssetDatabase.GetAssetPath(asset), asset, context);
        }
        
        private void CollectAsset(GameAssetPackage gameAssetPackage, string assetPath, Object asset, ManifestContext context)
        {
            if (asset == null)
            {
                Debug.LogError($"资产无效: {assetPath}");
                return;    
            }
            
            if(!IsValidateAsset(assetPath, asset))
                return;
            
            foreach (var filterRule in gameAssetPackage.FilterRules)
            {
                if (!filterRule.IsCollectAsset(context, gameAssetPackage, assetPath, asset))
                    return;
            }
            foreach (var filterRule in FilterRules)
            {
                if (!filterRule.IsCollectAsset(context, gameAssetPackage, assetPath, asset))
                    return;
            }

            var aliases = HashSetPool<string>.Get();
            foreach (var filterRule in gameAssetPackage.AliaseRules)
            {
                var alias = filterRule.GetAliase(context, gameAssetPackage, asset);
                if (!string.IsNullOrEmpty(alias))
                {
                    context.AddAlias(gameAssetPackage.Name, assetPath, alias);
                    aliases.Add(alias);
                }
            }
            foreach (var filterRule in AliaseRules)
            {
                var alias = filterRule.GetAliase(context, gameAssetPackage, asset);
                if (!string.IsNullOrEmpty(alias))
                {
                    context.AddAlias(gameAssetPackage.Name, assetPath, alias);
                    aliases.Add(alias);
                }
            }
            
            var assetCollectorInfo = context.GetOrAddGameAssetInfo(asset, gameAssetPackage, aliases);
            HashSetPool<string>.Release(aliases);
        }

        /// <summary> 忽略的文件类型 </summary>
        private readonly static HashSet<string> _ignoreFileExtensions = new HashSet<string>() { "", ".so", ".dll", ".cs", ".js", ".boo", ".meta", ".cginc", ".hlsl" };
        
        /// <summary> 是否是有效资源，可以进AB打包的 </summary>
        public static bool IsValidateAsset(string assetPath, Object asset)
        {
            // 忽略文件夹
            if (AssetDatabase.IsValidFolder(assetPath))
                return false;
            
            // 忽略Editor文件夹下的资产
            if (assetPath.Contains("/Editor/") || assetPath.Contains("/Editor Resources/"))
                return false;
            
            // 忽略指定的文件类型
            var fileExtension = System.IO.Path.GetExtension(assetPath);
            if (_ignoreFileExtensions.Contains(fileExtension))
                return false;
            
            // 忽略编辑器下的类型资源
            if (asset != null && asset.GetType() == typeof(LightingDataAsset))
                return false;
            
            return true;
        }
    }
}