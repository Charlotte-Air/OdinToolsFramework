#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

namespace Framework.AssetBundleHelper
{
    public static class AssetBundleUtility
    {
        /// <summary> 获取目标资产的MainAsset和SubAssets的列表，第一个一定是MainAsset，后面的顺序随机 </summary>
        public static void GetAssetList(string assetPath, List<Object> outAssets)
        {
            outAssets.Clear();
            var isScene = assetPath.EndsWith(".unity");
            var isPrefab = assetPath.EndsWith(".prefab");
            
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            outAssets.Add(mainAsset);
            
            if(isScene || isPrefab) //场景不能用LoadAllAssetsAtPath（会报错），预制体也不能用（因为会递归获取里面的所有对象组件）
                return;
            var allAsset = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var asset in allAsset)
            {
                if(asset == mainAsset)
                    continue;
                outAssets.Add(asset);
            }
        }
        
        /// <summary> 获取目标资产的MainAsset和SubAssets的列表，第一个一定是MainAsset，后面的顺序随机 </summary>
        public static void GetAssetList(Object asset, List<Object> outAssets)
        {
            GetAssetList(AssetDatabase.GetAssetPath(asset), outAssets);
        }

        private static Stack<string> _stack = new();
        /// <summary> 查询资产依赖，包括间接依赖 </summary>
        public static List<string> GetDependencies(string assetPath)
        {
            _stack.Clear();
            using (HashSetPool<string>.Get(out var dependencies))
            {
                _stack.Push(assetPath);
                while (_stack.Count > 0)
                {
                    var currentAssetPath = _stack.Pop();
                    if (!dependencies.Add(currentAssetPath))
                        continue;
                    var currentDependencies = AssetDatabase.GetDependencies(currentAssetPath, false); // 这个接口有bug，只有获取直接依赖的时候才能获取到正确的依赖
                    foreach (var dependency in currentDependencies)
                    {
                        _stack.Push(dependency);
                    }
                }
                return new List<string>(dependencies);
            }
        }
    }
}
#endif