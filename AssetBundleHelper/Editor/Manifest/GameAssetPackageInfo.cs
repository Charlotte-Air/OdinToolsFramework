using System.Collections.Generic;
using UnityEngine.Pool;

namespace Framework.AssetBundleHelper.Editor
{
    public class GameAssetPackageInfo
    {
        public string Name;

        /// <summary> 引用的游戏资产，这些游戏资产可能是在共享游戏资产包内的 </summary>
        public HashSet<GameAssetInfo> GameAssetInfoSet = new();

        /// <summary> 依赖的共享游戏资产包 </summary>
        public HashSet<GameAssetSharePackageInfo> GameAssetSharePackageInfoSet = new();

        /// <summary> 引用的AssetBundleInfo，包括间接引用的 </summary>
        public HashSet<AssetBundleInfo> AssetBundleInfos = new();
    }

    public class GameAssetSharePackageInfo
    {
        /// <summary> 依赖当前共享游戏资产包的游戏资产包 </summary>
        public HashSet<GameAssetPackageInfo> GameAssetPackageInfoSet = new();

        /// <summary> 包含的游戏资产 </summary>
        public HashSet<GameAssetInfo> GameAssetInfoSet = new();

        private string _name;
        public string Name
        {
            get
            {
                _name = "ShareGameAssetPackage";
                var nameList = ListPool<string>.Get();
                foreach (var gameAssetPackageInfo in GameAssetPackageInfoSet)
                    nameList.Add(gameAssetPackageInfo.Name);
                nameList.Sort();
                foreach (var name in nameList)
                    _name += $"_{name}";
                ListPool<string>.Release(nameList);
                return _name;
            }
        }
    }
}