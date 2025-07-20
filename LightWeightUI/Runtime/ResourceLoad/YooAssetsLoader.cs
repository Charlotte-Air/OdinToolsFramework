using System;
using YooAsset;
using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;

namespace LightWeightUI
{
    /// <summary>YooAssets的资源加载器,使用YooAssets插件加载</summary>
    public class YooAssetsLoader : IResLoader
    {
        /// <summary>枚举转寻址符,UIPage</summary>
        private string GetUIPageResAddress(UIPageId ID)
        {
            //YooAsset中使用的寻址规则为 “组名”_"文件名"
            return $"UIPages_{ID.ToString()}";
        }

        /// <summary>枚举转寻址符,WorldUI</summary>
        private string GetWorldUIResAddress(WorldUIId ID)
        {
            //YooAsset中使用的寻址规则为 “组名”_"文件名"
            return $"WorldUI_{ID.ToString()}";
        }
        
        /// <summary>异步加载预制体</summary>
        /// <param name="address">在YooAsset中配置好的资源定位字段</param>
        /// <returns>成功返回预制体，加载失败返回Null</returns>
        private async UniTask<GameObject> LoadPrefabAsync(string address)
        {
            AssetHandle opHandle = YooAssets.LoadAssetAsync<GameObject>(address);
            await UniTaskExtensions.AsUniTask(opHandle.Task);
            if (EOperationStatus.Succeed == opHandle.Status)
            {
                return opHandle.AssetObject as GameObject;
            }

            return null;
        }

        private IEnumerator LoadPrefabAsyncCoroutine(string address, Action<GameObject> OnLoadDone)
        {
            var opHandle = YooAssets.LoadAssetAsync<GameObject>(address);
            opHandle.Completed += (assetOpHandle) => { OnLoadDone?.Invoke(assetOpHandle.AssetObject as GameObject); };

            return opHandle;
        }
        
        /// <summary>同步加载预制体</summary>
        /// <param name="address">在YooAsset中配置好的资源定位字段</param>
        /// <returns>成功返回预制体，加载失败返回Null</returns>
        private GameObject LoadPrefabSync(string address)
        {
            AssetHandle opHandle = YooAssets.LoadAssetSync<GameObject>(address);
            return opHandle.AssetObject as GameObject;
        }
        
        /// <summary>根据ID加载UI资源,异步</summary>
        /// <param name="ID">UI物体的ID</param>
        /// <returns>返回对应的UI预制体</returns>
        public async UniTask<GameObject> LoadUIObject(UIPageId ID)
        {
            try
            {
                return await LoadPrefabAsync(GetUIPageResAddress(ID));
            }
            catch (Exception e)
            {
                Debug.LogError($"LightWeight UIManager 加载UI资源失败: 失败资源ID {ID}，错误信息 {e.ToString()}");
                return null;
            }
        }

        /// <summary>根据ID加载UI物体资源,异步,使用协程的方式</summary>
        /// <param name="ID">UI枚举ID</param>
        /// <param name="onLoadDone">接受物体的回调</param>
        public IEnumerator LoadUIObjectCoroutine(UIPageId ID, Action<GameObject> onLoadDone)
        {
            try
            {
                return LoadPrefabAsyncCoroutine(GetUIPageResAddress(ID), onLoadDone);
            }
            catch (Exception e)
            {
                Debug.LogError($"LightWeight UIManager 加载UI资源失败: 失败资源ID {ID}，错误信息 {e.ToString()}");
                return null;
            }
        }

        /// <summary>根据ID加载UI资源,同步</summary>
        /// <param name="ID">UI物体的ID</param>
        /// <returns>返回对应的UI预制体</returns>
        public GameObject LoadUIObjectSync(UIPageId ID)
        {
            try
            {
                return LoadPrefabSync(GetUIPageResAddress(ID));
            }
            catch (Exception e)
            {
                Debug.LogError($"LightWeight UIManager 加载UI资源失败: 失败资源ID {ID}，错误信息 {e.ToString()}");
                return null;
            }
        }
        
        /// <summary>根据ID加载UI资源,同步</summary>
        /// <param name="prefabName">UI物体的prefabName</param>
        /// <returns>返回对应的UI预制体</returns>
        public GameObject LoadUIObjectSync( string prefabName )
        {
            try
            {
                return LoadPrefabSync($"UIPages_{prefabName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"LightWeight UIManager 加载UI资源失败: 失败资源prefabName {prefabName}，错误信息 {e.ToString()}");
                return null;
            }
        }

        /// <summary>根据ID加载UI资源,异步</summary>
        /// <param name="ID">UI物体的ID</param>
        /// <returns>返回对应的UI预制体</returns>
        public async UniTask<GameObject> LoadUIObject(WorldUIId ID)
        {
            try
            {
                return await LoadPrefabAsync(GetWorldUIResAddress(ID));
            }
            catch (Exception e)
            {
                Debug.LogError($"LightWeight UIManager 加载UI资源失败: 失败资源ID {ID}，错误信息 {e.ToString()}");
                return null;
            }
        }

        /// <summary>根据ID加载UI物体资源,异步,使用协程的方式</summary>
        /// <param name="ID">UI枚举ID</param>
        /// <param name="onLoadDone">接受物体的回调</param>
        public IEnumerator LoadUIObjectCoroutine(WorldUIId ID, Action<GameObject> onLoadDone)
        {
            try
            {
                return LoadPrefabAsyncCoroutine(GetWorldUIResAddress(ID), onLoadDone);
            }
            catch (Exception e)
            {
                Debug.LogError($"LightWeight UIManager 加载UI资源失败: 失败资源ID {ID}，错误信息 {e.ToString()}");
                return null;
            }
        }

        /// <summary>根据ID加载UI资源,同步</summary>
        /// <param name="ID">UI物体的ID</param>
        /// <returns>返回对应的UI预制体</returns>
        public GameObject LoadUIObjectSync(WorldUIId ID)
        {
            try
            {
                return LoadPrefabSync(GetWorldUIResAddress(ID));
            }
            catch (Exception e)
            {
                Debug.LogError($"LightWeight UIManager 加载UI资源失败: 失败资源ID {ID}，错误信息 {e.ToString()}");
                return null;
            }
        }
    }
}