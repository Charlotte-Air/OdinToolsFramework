using System;
using UnityEngine.Pool;
using System.Collections.Generic;

namespace Framework.AssetBundleHelper.Editor
{
    public class AssetBundleInfo
    {
        public string Name;
        public HashSet<AssetFileInfo> AssetFileInfoSet = new();

        /// <summary> 包括间接依赖的 </summary>
        public HashSet<AssetBundleInfo> Dependencies = new();
        
        /// <summary> 直接依赖的 </summary>
        public HashSet<AssetBundleInfo> DirectDependencies = new();

        /// <summary> 实际打包出来后的文件名 </summary>
        public string BuildFileName;

        public uint Crc;
        
        public AssetBundleManifest AssetBundleManifest;
        
        public string AssetBundleManifestPath => $"Assets/BuildAssetBundleTemp/{Name}.asset";
    }
}