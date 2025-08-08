using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Framework.ConfigHelper;

namespace Framework.AssetBundleHelper
{
    [Serializable]
    [ConfigMenu(category = "程序/AssetBundle/运行时")]
    public class AssetBundleRuntimeConfig : GameConfig<AssetBundleRuntimeConfig>
    {
#if UNITY_EDITOR
        [LabelText("编辑器下强制AB加载"), Tooltip("启用后在编辑器中播放游戏时会强制加载AB包，方便调试"), ShowInInspector, PropertyOrder(-1)]
        public bool ForceLoadAssetBundleInEditor
        {
            get => UnityEditor.EditorPrefs.GetBool($"ForceLoadAssetBundleInEditor_{Application.productName}", false);
            set => UnityEditor.EditorPrefs.SetBool($"ForceLoadAssetBundleInEditor_{Application.productName}", value);
        }
#endif
        [NonSerialized, OdinSerialize, ReadOnly]
        public Manifest Manifest;
    }
}