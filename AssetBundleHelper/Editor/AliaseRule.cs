using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector;
using Object = UnityEngine.Object;

namespace Framework.AssetBundleHelper.Editor
{
    [Serializable, InlineProperty]
    public class AliaseRule
    {
        public virtual string GetAliase(ManifestContext context, GameAssetPackage gameAssetPackage, Object asset)
        {
            return null;
        }
    }

    /// <summary> 包名_文件夹名_资产对象名 </summary>
    [Serializable]
    public class PackageName_FolderName_AssetName : AliaseRule
    {
        public override string GetAliase(ManifestContext context, GameAssetPackage gameAssetPackage, Object asset)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var fileInfo = new FileInfo(assetPath);
            var folderName = fileInfo.Directory.Name;
            return $"{gameAssetPackage.Name}_{folderName}_{asset.name}";
        }
    }
    
    /// <summary> 包名_文件夹名_资产文件名 </summary>
    [Serializable]
    public class PackageName_FolderName_AssetFileName : AliaseRule
    {
        public override string GetAliase(ManifestContext context, GameAssetPackage gameAssetPackage, Object asset)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var fileInfo = new FileInfo(assetPath);
            var folderName = fileInfo.Directory.Name;
            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            return $"{gameAssetPackage.Name}_{folderName}_{fileName}";
        }
    }
    
    /// <summary> 包名_资产文件名 </summary>
    [Serializable]
    public class PackageName_AssetFileName : AliaseRule
    {
        public override string GetAliase(ManifestContext context, GameAssetPackage gameAssetPackage, Object asset)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            return $"{gameAssetPackage.Name}_{fileName}";
        }
    }
    
    /// <summary> 文件夹名_资产文件名 </summary>
    [Serializable]
    public class FolderName_AssetFileName : AliaseRule
    {
        public override string GetAliase(ManifestContext context, GameAssetPackage gameAssetPackage, Object asset)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var fileInfo = new FileInfo(assetPath);
            var folderName = fileInfo.Directory.Name;
            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            return $"{folderName}_{fileName}";
        }
    }
}