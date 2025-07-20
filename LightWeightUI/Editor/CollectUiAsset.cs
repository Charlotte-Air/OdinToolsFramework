using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;

namespace LightWeightUI
{
    [DisplayName("收集UI相关预制体")]
    public class CollectUiAsset : IFilterRule
    {
        public bool IsCollectAsset(FilterRuleData data)
        {
            if (Path.GetExtension(data.AssetPath) != ".prefab")
                return false;
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(data.AssetPath) as GameObject;
            if (mainAsset == null)
                return false;
            var page = mainAsset.GetComponent<UIPage>();
            var worldUIElement = mainAsset.GetComponent<WorldUIElement>();
            if (page == null && worldUIElement == null)
                return false;
            return true;
        }
    }
}