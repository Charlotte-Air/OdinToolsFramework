using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;

namespace Framework.AssetBundleHelper.Editor
{
    /// <summary>
    /// 用户指定的游戏资产数据包。
    /// 游戏资产包不是和AssetBundle对应的，而是一次性加载的资源包，比如需要上CDN的话可能会拆成多个AB包，或者和共享AB包。
    /// 一般按照游戏阶段分GameAssetPackage，比如：启动阶段，100级解锁的功能阶段等。
    /// </summary>
    [Serializable]
    public class GameAssetPackage
    {
        [VerticalGroup("包名"), HideLabel]
        [TableColumnWidth(160, false)]
        public string Name;
        
        [VerticalGroup("包名")]
        [Button("预览")]
        public void Preview()
        {
            if(AssetBundleEditorConfig.Instance.ManifestContext == null)
                AssetBundleEditorConfig.Instance.PreviewBuildManifest();
            GameAssetPackagePreviewWindow.ShowPreviewWindow(Name);
        }
        
        [VerticalGroup("描述"), HideLabel]
        [TableColumnWidth(160, false), TextArea]
        public string Description;
        
        [VerticalGroup("资产过滤规则"), HideLabel]
        [TableColumnWidth(220, false)]
        [ListDrawerSettings(DraggableItems = false, ShowPaging = false, ShowFoldout = false)]
        public List<FilterRule> FilterRules = new();
        
        [VerticalGroup("资产别名规则"), HideLabel]
        [TableColumnWidth(300, false)]
        [ListDrawerSettings(DraggableItems = false, ShowPaging = false, ShowFoldout = false)]
        public List<AliaseRule> AliaseRules = new();
        
        [VerticalGroup("游戏资产收集器"), HideLabel]
        [ListDrawerSettings(DraggableItems = false, ShowPaging = false, ShowFoldout = false, CustomAddFunction = nameof(AddGameAssetCollector))]
        public List<GameAssetCollector> GameAssetCollectorList = new();
        
        private GameAssetCollector AddGameAssetCollector()
        {
            return new GameAssetCollector();
        }
    }
}