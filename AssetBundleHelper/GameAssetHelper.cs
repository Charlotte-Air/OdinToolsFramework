using System;
using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Framework.AssetBundleHelper
{
    public static class GameAssetHelper
    {
        private static Manifest _currentManifest;

        /// <summary> 是否需要在编辑器下从AB包中加载资源 </summary>
        private static bool _forceLoadAssetBundle
        {
#if UNITY_EDITOR
            get => AssetBundleRuntimeConfig.Instance.ForceLoadAssetBundleInEditor;
#else
            get => true;
#endif
        }
        
        public static void Init()
        {
#if UNITY_EDITOR
            if (_forceLoadAssetBundle)
                _currentManifest = AssetBundleRuntimeConfig.Instance.Manifest;
            else
            {
                PreviewBuildManifest();
                _currentManifest = GetManifest();
            }
#else
            _currentManifest = AssetBundleRuntimeConfig.Instance.Manifest;
#endif
            
            if(_currentManifest == null)
                throw new System.Exception("Manifest is null");
            _currentManifest.Init(_forceLoadAssetBundle);

            PlayerLoopInterface.InsertSystemAfter(typeof(GameAssetHelper), Update, typeof(UnityEngine.PlayerLoop.PostLateUpdate));
        }

        /// <summary>
        /// 异步加载游戏资产包
        /// 注意：如果在等待加载过程中调用了卸载会提前返回
        /// </summary>
        public static async UniTask<GameAssetPackageRuntime> LoadPackage(string packageName)
        {
            return await _currentManifest.LoadGameAssetPackage(packageName);
        }

        /// <summary> 卸载游戏资产包 </summary>
        public static void UnloadGameAssetPackage(string packageName)
        {
            _currentManifest.UnloadGameAssetPackage(packageName);
        }

        /// <summary>
        /// 异步加载场景游戏资产包，并打开关卡
        /// 目标packageName中必须包含sceneName1和sceneName2，不然会找不到关卡，除非这个关卡在其他已经加载过的游戏资产包中（这样的话建议不要使用这个接口，直接用SceneManager操作）
        /// 不提供allowSceneActivation参数，因为Unity的场景异步加载如果挂起的话，会阻塞后面的其他异步任务（异步资源加载，异步打开场景）
        /// </summary>
        public static async UniTask LoadScenePackage(string packageName, LoadSceneMode loadScene1Mode, string sceneName1, string sceneName2 = null)
        {
            var gameAssetPackageRuntime = await LoadPackage(packageName);
            if (gameAssetPackageRuntime == null)
                throw new System.Exception($"LoadScenePackage packageName:{packageName} gameAssetPackageRuntime is null");

            await LoadSceneAsyncUniTask(sceneName1, loadScene1Mode);
            
            if (sceneName2 != null)
            {
                await LoadSceneAsyncUniTask(sceneName2, LoadSceneMode.Additive);
            }
        }

        /// <summary> 加载场景，异步，请使用此方法加载场景，因为Editor下加载方式略有不同 </summary>
        public static UniTask LoadSceneAsyncUniTask(string sceneAlias, LoadSceneMode loadSceneMode)
        {
            var asyOp = LoadSceneAsync(sceneAlias, loadSceneMode);
            return asyOp.ToUniTask();
        }

        /// <summary> 加载场景，异步，请使用此方法加载场景，因为Editor下加载方式略有不同 </summary>
        public static AsyncOperation LoadSceneAsync(string sceneAlias, LoadSceneMode loadSceneMode)
        {
            if (null == _currentManifest)
            {
                throw new Exception($"加载场景失败, 没有清单文件, 请先尝试调用GameAssetHelper.Init()");
            }

            if (!_currentManifest.GameAssetMap2.TryGetValue(sceneAlias, out var asset))
            {
                throw new Exception($"加载场景失败，资产别名[{sceneAlias}]不存在,请检查配置");
            }
#if UNITY_EDITOR
            if (!_currentManifest.IsForceLoadAssetBundle)
            {
                //Editor下需要特殊方式加载场景
                LoadSceneParameters loadSceneParameters = new LoadSceneParameters();
                loadSceneParameters.loadSceneMode = loadSceneMode;
                var asyOp = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(asset.AssetPath, loadSceneParameters);
                if (asyOp != null)
                {
                    asyOp.allowSceneActivation = true;
                    asyOp.priority = 100;
                }
                else
                {
                    throw new Exception($"加载场景失败, 指定路径下没有找到场景文件 {asset.AssetPath}");
                }

                return asyOp;
            }
            else
#endif
            {
                
                string sceneName = asset.AssetPath;
                var asyOp = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
                if(asyOp == null)
                    throw new System.Exception($"加载场景失败, 没有找到名为[{sceneName}]的场景");
                return asyOp;
            }
        }

        /// <summary>卸载场景,异步</summary>
        /// <param name="sceneAlias">传入场景的别名寻址符</param>
        public static UniTask UnloadSceneAsyncUniTask(string sceneAlias)
        {
            var asyOp = UnloadSceneAsync(sceneAlias);
            return asyOp.ToUniTask();
        }

        /// <summary>卸载场景,异步</summary>
        /// <param name="sceneAlias">传入场景的别名寻址符</param>
        public static AsyncOperation UnloadSceneAsync(string sceneAlias)
        {
            if (null == _currentManifest)
            {
                throw new Exception($"卸载场景失败, 没有清单文件, 请先尝试调用GameAssetHelper.Init()");
            }

            if (!_currentManifest.GameAssetMap2.TryGetValue(sceneAlias, out var asset))
            {
                throw new Exception($"卸载场景失败，资产别名[{sceneAlias}]不存在,请检查配置");
            }
#if UNITY_EDITOR
            if (!_currentManifest.IsForceLoadAssetBundle)
            {
                //Editor下需要特殊方式卸载场景
                var asyOp = UnityEditor.SceneManagement.EditorSceneManager.UnloadSceneAsync(asset.AssetPath);
                if (asyOp != null)
                {
                    asyOp.allowSceneActivation = true;
                }
                else
                {
                    throw new Exception($"卸载场景失败, 此场景没有被加载过 {asset.AssetPath}");
                }

                return asyOp;
            }
            else
#endif
            {
                string sceneName = asset.AssetPath;
                var asyOp = SceneManager.UnloadSceneAsync(sceneName);
                if (asyOp == null)
                    throw new System.Exception($"卸载场景失败, 此场景没有被加载过[{sceneName}]");
                return asyOp;
            }
        }

        /// <summary> 卸载场景游戏资产包 </summary>
        public static async UniTask UnloadScenePackage(string packageName, string sceneName1, string sceneName2 = null)
        {
            var unloadSceneAsync1 = SceneManager.UnloadSceneAsync(sceneName1);
            if(unloadSceneAsync1 == null)
                throw new System.Exception($"UnloadScenePackage packageName:{packageName} sceneName1:{sceneName1} unloadSceneAsync1 is null");
            await unloadSceneAsync1.ToUniTask();
            
            if (sceneName2 != null)
            {
                var unloadSceneAsync2 = SceneManager.UnloadSceneAsync(sceneName2);
                if(unloadSceneAsync2 == null)
                    throw new System.Exception($"UnloadScenePackage packageName:{packageName} sceneName2:{sceneName2} unloadSceneAsync2 is null");
                await unloadSceneAsync2.ToUniTask();
            }
            UnloadGameAssetPackage(packageName);
        }
        
        /// <summary>
        /// 通过address查找已加载的游戏资源对象
        /// address：AssetPath或AssetPath_SubAssetName，AssetGuid_FileId，Alias
        /// 如果指定的类型不是address直接加载的类型，会遍历查找同一个GameAssetFile中的所有资产对象（Unity一个资产文件里面可能包含多个资产对象）
        /// </summary>
        public static T FindAsset<T>(string address) where T : UnityEngine.Object
        {
            return _currentManifest.FindGameAsset<T>(address);
        }

        /// <summary> 查找游戏资产，对应的游戏资产包加载后才能找到 </summary>
        public static T FindAsset<T>(Guid assetGuid, long fileId) where T : UnityEngine.Object
        {
            return _currentManifest.FindGameAsset<T>(assetGuid, fileId);
        }

        /// <summary> 资产是否存在清单中 </summary>
        public static bool IsAssetExistInManifest(Guid assetGuid, long fileId)
        {
            return _currentManifest.GameAssetMap.ContainsKey((assetGuid, fileId));
        }
        
        /// <summary> 资产是否存在清单中 </summary>
        public static bool IsAssetExistInManifest(string address)
        {
            return _currentManifest.GameAssetMap2.ContainsKey(address);
        }

        private static void Update()
        {
            _currentManifest.UpdateLoadState();
        }
        
#if UNITY_EDITOR
        /// <summary> 通过反射调用AssetBundleEditorConfig.PreviewBuildManifest() </summary>
        private static void PreviewBuildManifest()
        {
            // 从Editor中找 Framework.AssetBundleHelper.Editor.AssetBundleEditorConfig 类型
            var type = System.Reflection.Assembly.Load("Assembly-CSharp-Editor").GetType("Framework.AssetBundleHelper.Editor.AssetBundleEditorConfig");
            if (type == null)
            {
                Debug.LogError("找不到 Framework.AssetBundleHelper.Editor.AssetBundleEditorConfig 类型");
                return;
            }
            // 获取实例 Framework.AssetBundleHelper.Editor.AssetBundleEditorConfig.GetInstance
            var instance = type.GetMethod("GetInstance").Invoke(null, null);
            if (instance == null)
            {
                Debug.LogError("找不到 Framework.AssetBundleHelper.Editor.AssetBundleEditorConfig.Instance 实例");
                return;
            }
            // 调用方法
            var method = type.GetMethod("PreviewBuildManifest");
            if (method == null)
            {
                Debug.LogError("找不到 Framework.AssetBundleHelper.Editor.AssetBundleEditorConfig.PreviewBuildManifest 方法");
                return;
            }
            method.Invoke(instance, null);
        }

        /// <summary> 通过反射获取 Framework.AssetBundleHelper.Editor.AssetBundleEditorConfig.ManifestRuntime 变量 </summary>
        private static Manifest GetManifest()
        {
            // 从Editor中找 Framework.AssetBundleHelper.Editor.AssetBundleEditorConfig 类型
            var type = System.Reflection.Assembly.Load("Assembly-CSharp-Editor").GetType("Framework.AssetBundleHelper.Editor.AssetBundleEditorConfig");
            if (type == null)
            {
                Debug.LogError("找不到 Framework.AssetBundleHelper.Editor.AssetBundleEditorConfig 类型");
                return null;
            }
            // 获取实例 Framework.AssetBundleHelper.Editor.AssetBundleEditorConfig.GetInstance
            var instance = type.GetMethod("GetInstance").Invoke(null, null);
            if (instance == null)
            {
                Debug.LogError("找不到 Framework.AssetBundleHelper.Editor.AssetBundleEditorConfig.Instance 实例");
                return null;
            }
            // 获取变量
            var field = type.GetField("ManifestRuntime");
            if (field == null)
            {
                Debug.LogError("找不到 Framework.AssetBundleHelper.Editor.AssetBundleEditorConfig.ManifestRuntime 变量");
                return null;
            }
            return field.GetValue(instance) as Manifest;
        }
#endif
    }
}