using System;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;

namespace Framework.AssetBundleHelper.Editor
{
    public class GameAssetPackagePreviewWindow : OdinEditorWindow
    {
        public static void ShowPreviewWindow(string packageName)
        {
            var window = GetWindow<GameAssetPackagePreviewWindow>();
            window.titleContent = new UnityEngine.GUIContent($"预览 {packageName}");
            window.Init(packageName);
            window.Show();
        }
        
        public static void OnDataUpdate()
        {
            if (HasOpenInstances<GameAssetPackagePreviewWindow>())
            {
                var window = GetWindow<GameAssetPackagePreviewWindow>();
                window.Init(window._packageName);
            }
        }

        [HideLabel, ShowInInspector, PropertyOrder(-1)]
        private string _mainAssetCountInfo => $"引用资产文件数量：{_currentPackageMainAssetCount}/{_totalMainAssetCount}";
        
        [HideLabel, ShowInInspector, PropertyOrder(-1)]
        private string _assetBundleCountInfo => $"引用AssetBundle数量：{_currentPackageAssetBundleCount}/{_totalAssetBundleCount}";
        
        [TableList(AlwaysExpanded = true, IsReadOnly = true), HideLabel, ShowInInspector]
        [Searchable]
        List<PreviewInfo> _previewInfos = new();
        
        Dictionary<string, PreviewInfo> _previewInfoMap = new Dictionary<string, PreviewInfo>();
        
        private string _packageName;

        private int _totalMainAssetCount;
        private int _currentPackageMainAssetCount;
        
        private int _totalAssetBundleCount;
        private int _currentPackageAssetBundleCount;
        
        private void Init(string packageName)
        {
            var context = AssetBundleEditorConfig.Instance.ManifestContext;
            if(context == null)
                return;
            if(!context.GameAssetPackageInfoMap.ContainsKey(packageName))
                return;
            _packageName = packageName;
            _previewInfos.Clear();
            _previewInfoMap.Clear();
            var gameAssetPackage = context.GameAssetPackageInfoMap[packageName];
            foreach (var gameAssetInfo in gameAssetPackage.GameAssetInfoSet)
            {
                var guidFileIdAddress = $"{gameAssetInfo.AssetGuid}_{gameAssetInfo.LocalId}";
                var assetPathAddress = gameAssetInfo.AssetPath;
                AddPreviewInfo(guidFileIdAddress, gameAssetInfo.Asset);
                AddPreviewInfo(assetPathAddress, gameAssetInfo.Asset);
                foreach (var alias in gameAssetInfo.Aliases)
                {
                    AddPreviewInfo(alias, gameAssetInfo.Asset);
                }
            }
            _previewInfos.Sort((a, b) => string.Compare(a.AssetPath, b.AssetPath, StringComparison.Ordinal));

            _totalMainAssetCount = context.AssetFileMap.Values.Count;
            _currentPackageMainAssetCount = 0;
            foreach (var assetBundleInfo in gameAssetPackage.AssetBundleInfos)
            {
                _currentPackageMainAssetCount += assetBundleInfo.AssetFileInfoSet.Count;
            }
            
            _totalAssetBundleCount = context.AssetBundleInfoMap.Count;
            _currentPackageAssetBundleCount = gameAssetPackage.AssetBundleInfos.Count;
        }
        
        private void AddPreviewInfo(string address, UnityEngine.Object asset)
        {
            if (_previewInfoMap.TryGetValue(address, out var previewInfo))
                return;
            previewInfo = new PreviewInfo
            {
                Address = address,
                Asset = asset,
                AssetPath = UnityEditor.AssetDatabase.GetAssetPath(asset)
            };
            _previewInfoMap[address] = previewInfo;
            _previewInfos.Add(previewInfo);
        }

        [Serializable]
        class PreviewInfo
        {
            [VerticalGroup("加载地址"), HideLabel, ReadOnly] 
            public string Address;
            
            [VerticalGroup("资源对象"), HideLabel, ReadOnly]
            public UnityEngine.Object Asset;

            [HideInInspector]
            public string AssetPath;
        }
    }
}