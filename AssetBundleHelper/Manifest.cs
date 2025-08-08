using System;
using System.IO;
using UnityEngine;
using UnityEngine.Pool;
using Sirenix.OdinInspector;
using UnityEngine.Profiling;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace Framework.AssetBundleHelper
{
    [Serializable]
    public class Manifest
    {
        [SerializeReference]
        public GameAssetPackageRuntime[] GameAssetPackages;
        
        [SerializeReference]
        public AssetBundleRuntime[] AssetBundleList;
        
        [NonSerialized, ShowInInspector]
        public Dictionary<string /*GameAssetPackageName*/, GameAssetPackageRuntime> GameAssetPackageMap;
        
        [NonSerialized, ShowInInspector]
        public Dictionary<string /*AssetBundleName*/, AssetBundleRuntime> AssetBundleMap;
        
        [NonSerialized, ShowInInspector]
        public Dictionary<(Guid, long), GameAssetRuntime> GameAssetMap;

        /// <summary> 包含所有地址：AssetPath或AssetPath_SubAssetName，AssetGuid_FileId，Alias </summary>
        [NonSerialized, ShowInInspector]
        public Dictionary<string, GameAssetRuntime> GameAssetMap2;
        
        [NonSerialized, ShowInInspector]
        private Dictionary<Guid, GameAssetFileRuntime> _gameAssetFileMap;

        private bool _forceLoadAssetBundle;
        public bool IsForceLoadAssetBundle => _forceLoadAssetBundle;

        public void Init(bool forceLoadAssetBundle)
        {
            if(GameAssetPackageMap != null)
                return;
            _forceLoadAssetBundle = forceLoadAssetBundle;
            
            GameAssetPackageMap = new Dictionary<string, GameAssetPackageRuntime>();
            AssetBundleMap = new Dictionary<string, AssetBundleRuntime>();
            GameAssetMap = new Dictionary<(Guid, long), GameAssetRuntime>();
            GameAssetMap2 = new Dictionary<string, GameAssetRuntime>();
            _gameAssetFileMap = new Dictionary<Guid, GameAssetFileRuntime>();
            
            // 初始化 GameAssetPackageMap
            foreach (var gameAssetPackage in GameAssetPackages)
            {
#if UNITY_EDITOR
                Debug.Assert(!GameAssetPackageMap.ContainsKey(gameAssetPackage.Name), gameAssetPackage.Name);
#endif
                GameAssetPackageMap[gameAssetPackage.Name] = gameAssetPackage;
                
                // 初始化 AssetBundleMap
                foreach (var assetBundle in gameAssetPackage.AssetBundles)
                {
                    AssetBundleMap[assetBundle.Name] = assetBundle;
                }
            }
            
            // 初始化 GameAssetMap
            var stringBuilder = new System.Text.StringBuilder(100);
            foreach (var assetBundleRuntime in AssetBundleMap.Values)
            {
                foreach (var gameAssetRuntime in assetBundleRuntime.GameAssetRuntimes)
                {
                    var key = (gameAssetRuntime.AssetGuid, gameAssetRuntime.LocalId);
#if UNITY_EDITOR
                    Debug.Assert(!GameAssetMap.ContainsKey(key) || GameAssetMap[key] == gameAssetRuntime, key.ToString());
                    Debug.Assert(gameAssetRuntime.AssetBundle == null);
#endif
                    if (!GameAssetMap.ContainsKey(key))
                    {
                        GameAssetMap[key] = gameAssetRuntime;
                        gameAssetRuntime.AssetFile = GetOrAddGameAssetFile(gameAssetRuntime.AssetGuid);
                        gameAssetRuntime.AssetBundle = assetBundleRuntime;
                    }

                    // 初始化 GameAssetMap2
                    {
                        stringBuilder.Clear();
                        stringBuilder.Append(gameAssetRuntime.AssetFile.AssetGuid.ToString());
                        stringBuilder.Append('_');
                        stringBuilder.Append(gameAssetRuntime.LocalId.ToString());
                        var guidFileId = stringBuilder.ToString();
#if UNITY_EDITOR
                        Debug.Assert(!GameAssetMap2.ContainsKey(guidFileId) || GameAssetMap2[guidFileId] == gameAssetRuntime, guidFileId);    
#endif
                        if (!GameAssetMap2.ContainsKey(guidFileId))
                        {
                            GameAssetMap2[guidFileId] = gameAssetRuntime;
                            foreach (var alias in gameAssetRuntime.Aliases)
                            {
#if UNITY_EDITOR
                                Debug.Assert(!GameAssetMap2.ContainsKey(alias), alias);            
#endif
                                GameAssetMap2[alias] = gameAssetRuntime;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 异步加载游戏资产包
        /// 注意：如果在等待加载过程中调用了卸载会提前返回
        /// </summary>
        public async UniTask<GameAssetPackageRuntime> LoadGameAssetPackage(string packageName)
        {
            if (!GameAssetPackageMap.TryGetValue(packageName, out var gameAssetPackage))
            {
                throw new Exception($"游戏资产包[{packageName}]，不存在！");
            }
            if (!gameAssetPackage.IsNeedLoad)
            {
                gameAssetPackage.IsNeedLoad = true;
                _isNeedUpdateLoadState = true;
            }

            var startTime = Time.realtimeSinceStartupAsDouble;
            while (gameAssetPackage.IsNeedLoad && !gameAssetPackage.IsLoad)
            {
                await UniTask.Yield(PlayerLoopTiming.PreUpdate);
            }
            Debug.Log($"游戏资产包[{packageName}]加载耗时：{Time.realtimeSinceStartupAsDouble - startTime}秒");
            
            return gameAssetPackage;
        }
        
        public void UnloadGameAssetPackage(string packageName)
        {
            if (!GameAssetPackageMap.TryGetValue(packageName, out var gameAssetPackage))
                return;
            if (gameAssetPackage.IsNeedLoad)
            {
                gameAssetPackage.IsNeedLoad = false;
                _isNeedUpdateLoadState = true;
            }
        }

        /// <summary>
        /// 通过address查找已加载的游戏资源对象
        /// address：AssetPath或AssetPath_SubAssetName，AssetGuid_FileId，Alias
        /// 如果指定的类型不是address直接加载的类型，会遍历查找同一个GameAssetFile中的所有资产对象（Unity一个资产文件里面可能包含多个资产对象）
        /// </summary>
        public T FindGameAsset<T>(string address) where T : UnityEngine.Object
        {
            if (GameAssetMap2.TryGetValue(address, out var gameAssetRuntime))
            {
                if(gameAssetRuntime.LoadState == LoadState.LoadedManifest)
                {
#if UNITY_EDITOR
                    if (!_forceLoadAssetBundle)
                    {
                        var outEditorAsset = gameAssetRuntime.GetEditorAsset<T>();
                        if (outEditorAsset == null)
                            return gameAssetRuntime.GetEditorAsset<T>(true);
                        return outEditorAsset;
                    }
#endif
                    var outAsset = gameAssetRuntime.Asset as T;
                    if (outAsset == null)
                    {
                        foreach (var asset in gameAssetRuntime.AssetFile.Assets)
                        {
                            outAsset = asset as T;
                            if (outAsset != null)
                                break;
                        }
                    }
                    return outAsset;
                }
            }
            return null;
        }

        public T FindGameAsset<T>(Guid assetGuid, long fileId) where T : UnityEngine.Object
        {
            if (GameAssetMap.TryGetValue((assetGuid, fileId), out var gameAssetRuntime))
            {
                if(gameAssetRuntime.LoadState == LoadState.LoadedManifest)
                {
#if UNITY_EDITOR
                    if(!_forceLoadAssetBundle)
                        return FindGameAssetEditor<T>(assetGuid, fileId);
#endif
                    var outAsset = gameAssetRuntime.Asset as T;
                    return outAsset;
                }
            }
            return null;
        }

#if UNITY_EDITOR
        public T FindGameAssetEditor<T>(Guid AssetGuid, long FileId) where T : UnityEngine.Object
        {
            if (GameAssetMap.TryGetValue((AssetGuid, FileId), out var gameAssetRuntime))
            {
                return gameAssetRuntime.GetEditorAsset<T>();
            }
            return null;
        }
#endif

        private bool _isNeedUpdateLoadState;
        public void UpdateLoadState()
        {
            if (_isNeedUpdateLoadState)
            {
                _isNeedUpdateLoadState = false;

#if UNITY_EDITOR
                if (!_forceLoadAssetBundle)
                {
                    foreach (var assetBundleRuntime in AssetBundleList)
                    {
                        assetBundleRuntime.LoadState = assetBundleRuntime.RefCount > 0 ? LoadState.LoadedManifest : LoadState.None;
                    }
                    foreach (var gameAssetPackage in GameAssetPackages)
                    {
                        gameAssetPackage.IsLoad = gameAssetPackage.IsNeedLoad;
                    }
                    return;
                }
#endif
                // 更新 AssetBundle 的 LoadState
                {
                    // 加载AB包
                    foreach (var assetBundleRuntime in AssetBundleList)
                    {
                        if (assetBundleRuntime.RefCount > 0)
                        {
                            if (assetBundleRuntime.LoadState is LoadState.None or LoadState.Unloading)
                            {
                                assetBundleRuntime.UnloadOperation = null;
                                assetBundleRuntime.LoadState = LoadState.LoadingAB;
                                assetBundleRuntime.CreateRequest = AssetBundle.LoadFromFileAsync(assetBundleRuntime.LocalAssetBundlePath); // 加载本地AB不需要Crc验证
                            }
                            if (assetBundleRuntime.LoadState == LoadState.LoadingAB)
                            {
                                if (assetBundleRuntime.CreateRequest.isDone)
                                {
                                    assetBundleRuntime.AssetBundle = assetBundleRuntime.CreateRequest.assetBundle;
                                    assetBundleRuntime.CreateRequest = null;
                                    
                                    if (assetBundleRuntime.IsContainScene || assetBundleRuntime.GameAssetRuntimes.Length == 0)
                                    {
                                        // 场景AB特殊处理，不从AB加载资产，后面直接用场景加载
                                        // 没有资产清单的AB包也不需要加载，其他AB加载资源的时候会自动一起加载
                                        assetBundleRuntime.LoadState = LoadState.LoadedManifest;
                                    }
                                    else
                                    {
                                        assetBundleRuntime.LoadState = LoadState.LoadedAB;
                                    }
                                }
                                else
                                    _isNeedUpdateLoadState = true;
                            }
                        }
                    }
                    // 加载AB包中的资产
                    foreach (var assetBundleRuntime in AssetBundleList)
                    {
                        if (assetBundleRuntime.RefCount > 0)
                        {
                            if (assetBundleRuntime.LoadState == LoadState.LoadedAB)
                            {
                                // 判断所有依赖项都加载完毕
                                var isAllDependenciesLoaded = true;
                                foreach (var dependencie in assetBundleRuntime.Dependencies)
                                {
                                    if(dependencie.LoadState is LoadState.None or LoadState.LoadingAB or LoadState.Unloading)
                                    {
                                        isAllDependenciesLoaded = false;
                                        break;
                                    }
                                }
                                if (isAllDependenciesLoaded)
                                {
                                    assetBundleRuntime.LoadManifestRequest = assetBundleRuntime.AssetBundle.LoadAssetAsync<AssetBundleManifest>(assetBundleRuntime.AssetBundleManifestPath);
                                    assetBundleRuntime.LoadState = LoadState.LoadingManifest;
                                }
                                else
                                    _isNeedUpdateLoadState = true;
                            }
                            if (assetBundleRuntime.LoadState == LoadState.LoadingManifest)
                            {
                                if (assetBundleRuntime.LoadManifestRequest.isDone)
                                {
                                    // 加载游戏资产清单
                                    if (assetBundleRuntime.GameAssetRuntimes.Length > 0)
                                    {
                                        assetBundleRuntime.AssetBundleManifest = assetBundleRuntime.LoadManifestRequest.asset as AssetBundleManifest;
#if UNITY_EDITOR
                                        Debug.Assert(assetBundleRuntime.AssetBundleManifest != null);                    
#endif
                                        Profiler.BeginSample("LoadManifest Init");
                                        LoadManifest(assetBundleRuntime.AssetBundleManifest);
                                        Profiler.EndSample();
                                    }
                                
                                    assetBundleRuntime.LoadManifestRequest = null;
                                    assetBundleRuntime.LoadState = LoadState.LoadedManifest;
                                }
                                else
                                    _isNeedUpdateLoadState = true;
                            }
                        }
                    }
                    // 卸载AB包
                    foreach(var assetBundleRuntime in AssetBundleList)
                    {
                        if (assetBundleRuntime.RefCount <= 0)
                        {
                            if (assetBundleRuntime.LoadState is LoadState.LoadedAB or LoadState.LoadedManifest)
                            {
                                if(assetBundleRuntime.AssetBundleManifest != null)
                                    UnloadManifest(assetBundleRuntime.AssetBundleManifest);
                                assetBundleRuntime.AssetBundleManifest = null;
                                assetBundleRuntime.UnloadOperation = assetBundleRuntime.AssetBundle.UnloadAsync(true);
                                assetBundleRuntime.AssetBundle = null;
                                assetBundleRuntime.LoadState = LoadState.Unloading;
                            }
                            if (assetBundleRuntime.LoadState == LoadState.Unloading)
                            {
                                if (assetBundleRuntime.UnloadOperation.isDone)
                                {
                                    assetBundleRuntime.UnloadOperation = null;
                                    assetBundleRuntime.LoadState = LoadState.None;
                                }
                                else
                                    _isNeedUpdateLoadState = true;
                            }
                        }
                    }
                }
                
                // 更新 GameAssetPackage 的 IsLoad
                foreach (var gameAssetPackage in GameAssetPackages)
                {
                    if (gameAssetPackage.IsNeedLoad)
                    {
                        if(gameAssetPackage.IsLoad)
                            continue;
                        gameAssetPackage.IsLoad = true;
                        foreach (var assetBundleRuntime in gameAssetPackage.AssetBundles)
                        {
                            if (assetBundleRuntime.LoadState != LoadState.LoadedManifest)
                            {
                                gameAssetPackage.IsLoad = false;
                                break;
                            }
                        }
                        if(!gameAssetPackage.IsLoad)
                            _isNeedUpdateLoadState = true;
                    }
                }
            }
        }

        private void LoadManifest(AssetBundleManifest assetBundleManifest)
        {
            using (HashSetPool<GameAssetFileRuntime>.Get(out var assetFileSet))
            {
                foreach (var pair in assetBundleManifest.GameAssetMap)
                {
                    var gameAsset = GameAssetMap[pair.Key];
                    gameAsset.Asset = pair.Value;
                    if (assetFileSet.Add(gameAsset.AssetFile))
                    {
                        gameAsset.AssetFile.Assets = assetBundleManifest.GameAssetFileMap[pair.Key.AssetGuid];
                    }
                }
            }
        }
        
        private void UnloadManifest(AssetBundleManifest assetBundleManifest)
        {
            using (HashSetPool<GameAssetFileRuntime>.Get(out var assetFileSet))
            {
                foreach (var pair in assetBundleManifest.GameAssetMap)
                {
                    var gameAsset = GameAssetMap[pair.Key];
                    gameAsset.Asset = null;
                    if (assetFileSet.Add(gameAsset.AssetFile))
                    {
                        gameAsset.AssetFile.Assets = null;
                    }
                }
            }
        }
        
        private GameAssetFileRuntime GetOrAddGameAssetFile(Guid assetGuid)
        {
            if (!_gameAssetFileMap.TryGetValue(assetGuid, out var gameAssetFile))
            {
                gameAssetFile = new GameAssetFileRuntime();
                gameAssetFile.AssetGuid = assetGuid;
                _gameAssetFileMap[assetGuid] = gameAssetFile;
            }

            return gameAssetFile;
        }
    }

    [Serializable]
    public class GameAssetPackageRuntime
    {
        public string Name;

        /// <summary> 包括间接引用的 </summary>
        [SerializeReference]
        public AssetBundleRuntime[] AssetBundles;
        
        [NonSerialized]
        public HashSet<AssetBundleRuntime> AssetBundlesTemp = new();

        [NonSerialized, ShowInInspector]
        public bool IsLoad;

        /// <summary> 是否需要加载 </summary>
        [ShowInInspector]
        public bool IsNeedLoad
        {
            get => _isNeedLoad;
            set
            {
                if (value != _isNeedLoad)
                {
                    foreach (var assetBundleRuntime in AssetBundles)
                    {
                        assetBundleRuntime.RefCount += value ? 1 : -1;
                    }
                    _isNeedLoad = value;
                    if(!_isNeedLoad)
                        IsLoad = false;
                }
            }
        }
        
        private bool _isNeedLoad;
    }

    [Serializable]
    public class AssetBundleRuntime
    {
        public string Name;
        
        /// <summary> 实际打包出来的文件名，可能是哈希码的名字 </summary>
        public string FileName;

        /// <summary> AssetBundle的CRC校验码 </summary>
        public uint Crc;

        public bool IsContainScene;

        [SerializeReference]
        public GameAssetRuntime[] GameAssetRuntimes;
        
        [NonSerialized]
        public HashSet<GameAssetRuntime> GameAssetRuntimesTemp = new();

        /// <summary> 包括间接引用的 </summary>
        [SerializeReference]
        public AssetBundleRuntime[] Dependencies;

#if UNITY_EDITOR
        public List<UnityEngine.Object> MainAssetList = new();
#endif
        
        public string AssetBundleManifestPath => $"Assets/BuildAssetBundleTemp/{Name}.asset";

        /// <summary> 和AssetBundleEditorConfig.DefaultOutputPath保持一致 </summary>
        public string LocalAssetBundlePath => Path.Combine(Application.streamingAssetsPath, "AssetBundles", FileName);
        
        [NonSerialized, ShowInInspector]
        public LoadState LoadState;
        
        [NonSerialized, ShowInInspector]
        public int RefCount;
        
        [NonSerialized]
        public AssetBundle AssetBundle;
        
        [NonSerialized]
        public AssetBundleCreateRequest CreateRequest;
        
        [NonSerialized]
        public AsyncOperation UnloadOperation;
        
        [NonSerialized]
        public AssetBundleRequest LoadManifestRequest;
        
        [NonSerialized]
        public AssetBundleManifest AssetBundleManifest;
    }
    
    [Serializable]
    public class GameAssetRuntime
    {
        public Guid AssetGuid;

        public long LocalId;
        
        /// <summary> 别名，AssetPath_AssetObjectName(子资产才有AssetObjectName) </summary>
        public List<string> Aliases = new();
        
        public string AssetPath => Aliases[0];
        
        [NonSerialized]
        public AssetBundleRuntime AssetBundle;
        
        [ShowInInspector]
        public LoadState LoadState => AssetBundle?.LoadState ?? LoadState.None;

        /// <summary> 从AB包加载出来的 </summary>
        [NonSerialized, ShowInInspector]
        public UnityEngine.Object Asset;
        
        [NonSerialized, ShowInInspector]
        public GameAssetFileRuntime AssetFile;
        
#if UNITY_EDITOR
        public T GetEditorAsset<T>(bool IgnoreLocalId = false) where T : UnityEngine.Object
        {
            var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(AssetGuid.ToString("N"));
            using (ListPool<UnityEngine.Object>.Get(out var assetList))
            {
                AssetBundleUtility.GetAssetList(assetPath, assetList);
                foreach (var asset in assetList)
                {
                    var localId = UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long fileId) ? fileId : 0;
                    if (IgnoreLocalId)
                    {
                        if (asset is T t)
                            return t;
                    }
                    else if (localId == LocalId)
                        return asset as T;
                }
            }
            return null;
        }
#endif
    }

    public class GameAssetFileRuntime
    {
        [NonSerialized, ShowInInspector]
        public Guid AssetGuid;
        
        /// <summary> 第一个一定是MainAsset，后面的SubAsset的顺序不确定 </summary>
        [NonSerialized, ShowInInspector]
        public List<UnityEngine.Object> Assets;
        
#if UNITY_EDITOR
        public List<UnityEngine.Object> GetEditorAssets()
        {
            var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(AssetGuid.ToString("N"));
            var assetList = new List<UnityEngine.Object>();
            AssetBundleUtility.GetAssetList(assetPath, assetList);
            return assetList;
        }
#endif
    }
    
    public enum LoadState
    {
        [InspectorName("未加载")]
        None,
        [InspectorName("正在加载AB包")]
        LoadingAB,
        [InspectorName("已加载AB包")]
        LoadedAB,
        [InspectorName("正在加载AB包中的清单资产")]
        LoadingManifest,
        [InspectorName("已加载AB包中的清单资产")]
        LoadedManifest,
        [InspectorName("卸载中")]
        Unloading,
    }
}