using System;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Framework.AssetBundleHelper.Editor
{
    /// <summary> 一次构建过程中收集的各种资源的依赖关系 </summary>
    public class ManifestContext
    {
        public Dictionary<string /*AssetPath*/, AssetFileInfo> AssetFileMap = new();
        public Dictionary<string /*AssetGuid_FileId*/, GameAssetInfo> GameAssetInfoMap = new();
        public Dictionary<string /*PackageName*/, GameAssetPackageInfo> GameAssetPackageInfoMap = new();
        public Dictionary<GameAssetInfo, GameAssetPackageInfo> GameAssetInfoToPackageInfoMap = new();
        public Dictionary<GameAssetInfo, GameAssetSharePackageInfo> GameAssetInfoToSharePackageInfoMap = new();
        public Dictionary<string, AssetBundleInfo> AssetBundleInfoMap = new();
        public HashSet<string> Aliases = new();
        
        public AssetFileInfo GetOrAddAssetFileInfo(string assetPath)
        {
            if (!AssetFileMap.TryGetValue(assetPath, out var assetFile))
            {
                assetFile = new AssetFileInfo
                {
                    AssetPath = assetPath,
                    AssetGuid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath),
                    FileExtension = System.IO.Path.GetExtension(assetPath),
                    MainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPath),
                    MainAssetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(assetPath)
                };
                if(assetFile.MainAsset == null)
                    Debug.LogError($"无效资产: {assetPath}");
                AssetFileMap[assetPath] = assetFile;
                assetFile.BuildDependencies(this);
            }
            return assetFile;
        }
        
        public GameAssetInfo GetOrAddGameAssetInfo(Object asset, GameAssetPackage gameAssetPackage, HashSet<string> Aliases)
        {
            UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long fileId);
            var assetGuidFileId = $"{guid}_{fileId}";
            if (!GameAssetInfoMap.TryGetValue(assetGuidFileId, out var gameAssetInfo))
            {
                var assetPath = UnityEditor.AssetDatabase.GetAssetPath(asset);
                var assetFile = GetOrAddAssetFileInfo(assetPath);
                var isSubAsset = UnityEditor.AssetDatabase.IsSubAsset(asset);
                gameAssetInfo = new GameAssetInfo
                {
                    Asset = asset,
                    AssetFile = assetFile,
                    AssetPath = isSubAsset ? $"{assetPath}_{asset.name}" : assetPath,
                    AssetGuid = new Guid(guid),
                    LocalId = fileId,
                    GameAssetPackages = new HashSet<GameAssetPackage> {gameAssetPackage}
                };
                GameAssetInfoMap[assetGuidFileId] = gameAssetInfo;
                assetFile.GameAssetInfoSet.Add(gameAssetInfo);
            }
            gameAssetInfo.GameAssetPackages.Add(gameAssetPackage);
            gameAssetInfo.Aliases.UnionWith(Aliases);
            return gameAssetInfo;
        }
        
        public (GameAssetPackageInfo, GameAssetSharePackageInfo) GetOrAddGameAssetPackageInfo(string packageName, GameAssetInfo gameAssetInfo)
        {
            if (!GameAssetPackageInfoMap.TryGetValue(packageName, out var gameAssetPackageInfo))
            {
                gameAssetPackageInfo = new GameAssetPackageInfo
                {
                    Name = packageName
                };
                GameAssetPackageInfoMap[packageName] = gameAssetPackageInfo;
                GameAssetInfoToPackageInfoMap[gameAssetInfo] = gameAssetPackageInfo;
            }
            gameAssetPackageInfo.GameAssetInfoSet.Add(gameAssetInfo);
            gameAssetInfo.GameAssetPackageInfo = gameAssetPackageInfo;

            if (!gameAssetInfo.IsShared)
                return (gameAssetPackageInfo, null);
            
            if (!GameAssetInfoToSharePackageInfoMap.TryGetValue(gameAssetInfo, out var gameAssetSharePackageInfo))
            {
                gameAssetSharePackageInfo = new GameAssetSharePackageInfo();
                GameAssetInfoToSharePackageInfoMap[gameAssetInfo] = gameAssetSharePackageInfo;
            }
            gameAssetSharePackageInfo.GameAssetInfoSet.Add(gameAssetInfo);
            gameAssetSharePackageInfo.GameAssetPackageInfoSet.Add(gameAssetPackageInfo);
            gameAssetPackageInfo.GameAssetSharePackageInfoSet.Add(gameAssetSharePackageInfo);
            gameAssetInfo.GameAssetSharePackageInfo = gameAssetSharePackageInfo;
            gameAssetInfo.GameAssetPackageInfo = null;
            
            return (gameAssetPackageInfo, gameAssetSharePackageInfo);
        }
        
        public AssetBundleInfo GetOrAddAssetBundleInfo(AssetFileInfo assetFileInfo)
        {
            if (!AssetBundleInfoMap.TryGetValue(assetFileInfo.AssetBundleName, out var assetBundleInfo))
            {
                assetBundleInfo = new AssetBundleInfo
                {
                    Name = assetFileInfo.AssetBundleName
                };
                AssetBundleInfoMap[assetFileInfo.AssetBundleName] = assetBundleInfo;
            }
            assetBundleInfo.AssetFileInfoSet.Add(assetFileInfo);
            assetFileInfo.AssetBundleInfo = assetBundleInfo;
            return assetBundleInfo;
        }
        
        public void AddAlias(string gameAssetPackageName, string assetPath, string alias)
        {
            if(!Aliases.Add(alias))
                throw new Exception($"游戏资产别名重复! 别名[{alias}] 游戏资产包名[{gameAssetPackageName}] 资产路径[{assetPath}]");
        }
    }
}