using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Framework.AssetBundleHelper.Editor
{
    public class GameAssetInfo
    {
        public AssetFileInfo AssetFile;

        public Object Asset;

        /// <summary> AssetPath_AssetObjectName(子资产才有AssetObjectName) </summary>
        public string AssetPath;
        
        public Guid AssetGuid;

        public long LocalId;
        
        public HashSet<string> Aliases = new();

        /// <summary> 被哪些游戏资产包引用了 </summary>
        public HashSet<GameAssetPackage> GameAssetPackages = new();

        /// <summary> 直接引用的游戏资产包 </summary>
        public GameAssetPackageInfo GameAssetPackageInfo;

        /// <summary> 直接引用的共享游戏资产包 </summary>
        public GameAssetSharePackageInfo GameAssetSharePackageInfo;

        /// <summary> 是否是共享的游戏资产 </summary>
        public bool IsShared => GameAssetPackages.Count > 1;

        /// <summary> 包括间接引用的 </summary>
        public HashSet<AssetBundleInfo> AssetBundleInfos = new();
    }
}