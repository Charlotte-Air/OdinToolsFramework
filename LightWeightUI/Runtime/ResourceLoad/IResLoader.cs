using System;
using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;

namespace LightWeightUI
{
    /// <summary>资源加载器接口，用于加载UI资源预制，继承这个接口来实现新的资源加载方法</summary>
    public interface IResLoader
    {
        /// <summary>根据ID加载UI物体资源,异步</summary>
        /// <param name="ID">UI枚举ID</param>
        /// <returns>返回加载出的预制体，用之前还需要Instantite创建实例</returns>
        UniTask<GameObject> LoadUIObject(UIPageId ID);

        /// <summary>根据ID加载UI物体资源,异步,使用协程的方式</summary>
        /// <param name="ID">UI枚举ID</param>
        /// <param name="onLoadDone">接受物体的回调</param>
        IEnumerator LoadUIObjectCoroutine(UIPageId ID, Action<GameObject> onLoadDone);
        
        /// <summary>加载UI资源,同步</summary>
        /// <param name="ID">UI枚举ID</param>
        /// <returns>返回加载出的预制体，用之前还需要Instantite创建实例</returns>
        GameObject LoadUIObjectSync(UIPageId ID);
        
        /// <summary>加载UI资源,同步</summary>
        /// <param name="prefabName">prefabName</param>
        /// <returns>返回加载出的预制体，用之前还需要Instantite创建实例</returns>
        GameObject LoadUIObjectSync(string prefabName);

        /// <summary>根据ID加载UI物体资源,异步</summary>
        /// <param name="ID">UI枚举ID</param>
        /// <returns>返回加载出的预制体，用之前还需要Instantite创建实例</returns>
        UniTask<GameObject> LoadUIObject(WorldUIId ID);

        /// <summary>根据ID加载UI物体资源,异步,使用协程的方式</summary>
        /// <param name="ID">UI枚举ID</param>
        /// <param name="onLoadDone">接受物体的回调</param>
        IEnumerator LoadUIObjectCoroutine(WorldUIId ID, Action<GameObject> onLoadDone);

        /// <summary>加载UI资源,同步</summary>
        /// <param name="ID">UI枚举ID</param>
        /// <returns>返回加载出的预制体，用之前还需要Instantite创建实例</returns>
        GameObject LoadUIObjectSync(WorldUIId ID);
    }
}