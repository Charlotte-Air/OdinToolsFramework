using System;
using System.IO;
using UnityEngine;
using Sirenix.OdinInspector;
using Object = UnityEngine.Object;

namespace Framework.AssetBundleHelper.Editor
{
    [Serializable, InlineProperty]
    public class FilterRule
    {
        /// <summary> 在没加载资源之前就过滤，比如直接通过文件后缀 </summary>
        public virtual bool IsCollectAsset(ManifestContext context, GameAssetPackage gameAssetPackage, string assetPath)
        {
            return true;
        }

        /// <summary> 加载资源之后过滤，主要用于过滤SubAsset类型，也可以在这检查资源有没有配置错误然后报错 </summary>
        public virtual bool IsCollectAsset(ManifestContext contextPlayerLoopInterface, GameAssetPackage gameAssetPackage, string assetPath, Object asset)
        {
            return true;
        }
    }
    
    [Serializable]
    public class CollectScene : FilterRule
    {
        public override bool IsCollectAsset(ManifestContext context, GameAssetPackage gameAssetPackage, string assetPath)
        {
            return Path.GetExtension(assetPath) == ".unity";
        }

        public override bool IsCollectAsset(ManifestContext context, GameAssetPackage gameAssetPackage, string assetPath, Object asset)
        {
            return Path.GetExtension(assetPath) == ".unity";
        }
    }

    [Serializable]
    public class CollectPrefab : FilterRule
    {
        public override bool IsCollectAsset(ManifestContext context, GameAssetPackage gameAssetPackage, string assetPath)
        {
            return Path.GetExtension(assetPath) == ".prefab";
        }

        public override bool IsCollectAsset(ManifestContext context, GameAssetPackage gameAssetPackage, string assetPath, Object asset)
        {
            return Path.GetExtension(assetPath) == ".prefab";
        }
    }

    [Serializable]
    public class CollectSprite : FilterRule
    {
        public override bool IsCollectAsset(ManifestContext context, GameAssetPackage gameAssetPackage, string assetPath)
        {
            return true;
        }

        public override bool IsCollectAsset(ManifestContext context, GameAssetPackage gameAssetPackage, string assetPath, Object asset)
        {
            return asset is Sprite;
        }
    }
    
    [Serializable]
    public class CollectShaderVariants : FilterRule
    {
        public override bool IsCollectAsset(ManifestContext context, GameAssetPackage gameAssetPackage, string assetPath)
        {
            return Path.GetExtension(assetPath) == ".shadervariants";
        }

        public override bool IsCollectAsset(ManifestContext context, GameAssetPackage gameAssetPackage, string assetPath, Object asset)
        {
            return Path.GetExtension(assetPath) == ".shadervariants";
        }
    }
    
    [Serializable]
    public class CollectCustomType : FilterRule
    {
        [HideLabel]
        [TypeDrawerSettings(BaseType = typeof(Object))]
        public Type AssetType;
        public override bool IsCollectAsset(ManifestContext context, GameAssetPackage gameAssetPackage, string assetPath)
        {
            return true;
        }

        public override bool IsCollectAsset(ManifestContext context, GameAssetPackage gameAssetPackage, string assetPath, Object asset)
        {
            var isMonoBehaviourType = AssetType.IsSubclassOf(typeof(MonoBehaviour));
            if (isMonoBehaviourType)
            {
                var gameObject = asset as GameObject;
                if (gameObject == null)
                    return false;
                return gameObject.GetComponent(AssetType) != null;
            }

            return asset.GetType() == AssetType;
        }
    }
    
    [Serializable]
    public class CollectCustomSuffix : FilterRule
    {
        [LabelText("后缀"), Tooltip("不是文件后缀，比如‘Assets/Farm/Art/Meshes/Common/Axe_Lod.fbx’，这里的后缀是‘_Lod’")]
        public string Suffix;
        public override bool IsCollectAsset(ManifestContext context, GameAssetPackage gameAssetPackage, string assetPath)
        {
            return Path.GetFileNameWithoutExtension(assetPath).EndsWith(Suffix);
        }
        
        public override bool IsCollectAsset(ManifestContext context, GameAssetPackage gameAssetPackage, string assetPath, Object asset)
        {
            return Path.GetFileNameWithoutExtension(assetPath).EndsWith(Suffix);
        }
    }

    [Serializable]
    public class CollectMainAsset : FilterRule
    {
        public override bool IsCollectAsset(ManifestContext context, GameAssetPackage gameAssetPackage, string assetPath)
        {
            return true;
        }

        public override bool IsCollectAsset(ManifestContext context, GameAssetPackage gameAssetPackage, string assetPath, Object asset)
        {
            return UnityEditor.AssetDatabase.IsMainAsset(asset);
        }
    }
}