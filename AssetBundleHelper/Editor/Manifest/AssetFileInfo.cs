using System;
using UnityEditor;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Framework.AssetBundleHelper.Editor
{
    /// <summary> 对应一个Unity资产文件，包括其中的子资产 </summary>
    public class AssetFileInfo
    {
        /// <summary> Unity的资产相对路径，包括文件后缀，比如’Assets/MyPrefab.prefab‘ </summary>
        public string AssetPath;

        /// <summary> Unity的AssetGuid </summary>
        public string AssetGuid;

        /// <summary> 文件后缀，比如‘prefab’ </summary>
        public string FileExtension;
        
        public Object MainAsset;
        
        public Type MainAssetType;

        /// <summary> 直接引用当前AssetFile的游戏资产 </summary>
        public HashSet<GameAssetInfo> GameAssetInfoSet = new();

        /// <summary> 引用当前AssetFile的游戏资产包（包括间接引用） </summary>
        public HashSet<GameAssetPackageInfo> GameAssetPackageInfoSet = new();

        /// <summary> 引用当前AssetFile的共享游戏资产包（包括间接引用） </summary>
        public HashSet<GameAssetSharePackageInfo> GameAssetSharePackageInfoSet = new();
        
        /// <summary> 依赖的其他AssetFile，包括间接的 </summary>
        public HashSet<AssetFileInfo> Dependencies = new();

        /// <summary> 直接依赖的 </summary>
        public HashSet<AssetFileInfo> DirectDependencies = new();

        public string AssetBundleName;

        /// <summary> 需要强制打入AB包的资产 </summary>
        public bool ForceAssetBundle;
        
        public AssetBundleInfo AssetBundleInfo;

        /// <summary> 是否需要显示指定AB，不需要的就通过AB自动引用打入(这样还能减少AB大小) </summary>
        public bool NeedExplicitAssetBundleName()
        {
            if(ForceAssetBundle)
                return true;
            if (GameAssetInfoSet.Count > 0)
                return true;
            if(GameAssetPackageInfoSet.Count > 1)
                return true;
            if(GameAssetSharePackageInfoSet.Count > 0)
                return true;
            return false;
        }
        
        public void BuildDependencies(ManifestContext context)
        {
            var dependencies = AssetBundleUtility.GetDependencies(AssetPath);
            foreach (var dependency in dependencies)
            {
                if (dependency == AssetPath)
                    continue;
                if(!GameAssetCollector.IsValidateAsset(dependency, null))
                    continue;
                var dependencyAssetFile = context.GetOrAddAssetFileInfo(dependency);
                Dependencies.Add(dependencyAssetFile);
            }
            
            var dependenciesSet = new HashSet<string>(dependencies);
            var directDependencies = UnityEditor.AssetDatabase.GetDependencies(AssetPath, false);
            foreach (var dependency in directDependencies)
            {
                if(!dependenciesSet.Contains(dependency))
                    throw new Exception($"!!!!!: {dependency}");
                if (dependency == AssetPath)
                    continue;
                if(!GameAssetCollector.IsValidateAsset(dependency, null))
                    continue;
                var dependencyAssetFile = context.GetOrAddAssetFileInfo(dependency);
                DirectDependencies.Add(dependencyAssetFile);
            }
        }

        /// <summary> 是否为着色器资源 </summary>
        public bool IsShaderAsset()
        {
            if (MainAssetType == typeof(UnityEngine.Shader) || MainAssetType == typeof(UnityEngine.ShaderVariantCollection))
                return true;
            else
                return false;
        }

        /// <summary> 是否为unity场景 </summary>
        public bool IsSceneAsset()
        {
            return FileExtension == ".unity";
        }
    }
}