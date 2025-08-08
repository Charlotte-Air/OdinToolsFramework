using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;

namespace Framework.AssetBundleHelper
{
    /// <summary> 在打包过程给每个AB包生成的清单，用于引用这个AB中的GameAsset </summary>
    public class AssetBundleManifest : SerializedScriptableObject
    {
        [NonSerialized, OdinSerialize]
        public Dictionary<(Guid AssetGuid, long LocalId), UnityEngine.Object> GameAssetMap = new();

        /// <summary> 第一个一定是MainAsset，后面的SubAsset的顺序不确定 </summary>
        [NonSerialized, OdinSerialize]
        public Dictionary<Guid /*AssetGuid*/, List<UnityEngine.Object>> GameAssetFileMap = new();

#if UNITY_EDITOR
        public void AddGameAsset(UnityEngine.Object asset)
        {
            UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId);
            var assetGuid = new Guid(guid);
            GameAssetMap[(assetGuid, localId)] = asset;
            
            if (!GameAssetFileMap.TryGetValue(assetGuid, out var assetList))
            {
                AssetBundleUtility.GetAssetList(asset, assetList = new List<UnityEngine.Object>());
                GameAssetFileMap[assetGuid] = assetList;
            }
        }
#endif
    }
}