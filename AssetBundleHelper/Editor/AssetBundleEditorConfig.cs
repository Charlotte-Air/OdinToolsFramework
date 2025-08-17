using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Pool;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Framework.ConfigHelper;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using System.Collections.Generic;
using Framework.ConfigHelper.Editor;
using UnityEditor.Build.Pipeline.Interfaces;
using BuildCompression = UnityEngine.BuildCompression;

namespace Framework.AssetBundleHelper.Editor
{
    [Serializable]
    [ConfigMenu(category = "程序/AssetBundle/编辑器")]
    public class AssetBundleEditorConfig : EditorConfig<AssetBundleEditorConfig>
    {
        [LabelText("输出路径")]
        [ShowInInspector]
        [FoldoutGroup("BuildAssetBundle", true)]
        public const string DefaultOutputPath = "Assets/StreamingAssets/AssetBundles";
        
        [LabelText("是否合并极小AB包")]
        [FoldoutGroup("BuildAssetBundle")]
        public bool MergeTinyAssetBundle = true;
        
        [LabelText("资产数量小于多少算极小AB包")]
        [FoldoutGroup("BuildAssetBundle")]
        [ShowIf("MergeTinyAssetBundle")]
        public int TinyAssetBundleCount = 4;
        
        [LabelText("打包参数")]
        [FoldoutGroup("BuildAssetBundle")]
        public AssetBundleBuildParameters BuildAssetBundleParameters = new();
        
        [Button("BuildAssetBundle(当前平台)", ButtonSizes.Medium)]
        [FoldoutGroup("BuildAssetBundle")]
        public void BuildAssetBundle() => BuildAssetBundleByConfig(EditorUserBuildSettings.activeBuildTarget);
        
        [Button("刷新预览BuildAssetBundle", ButtonSizes.Medium)]
        [FoldoutGroup("BuildAssetBundle")]
        public void PreviewBuildManifest()
        {
            BuildAssetBundle(true, "", BuildAssetBundleParameters, EditorUserBuildSettings.activeBuildTarget);
            ManifestRuntime.Init(false);
        }

        [Searchable]
        [NonSerialized, OdinSerialize, HideReferenceObjectPicker]
        [TableList]
        [LabelText("游戏资产包列表？"), ValidateInput(nameof(ValidateGameAssetPackageList))]
        [Tooltip("游戏资产包不是和AssetBundle对应的，而是一次性加载的资源包，比如需要上CDN的话可能会拆成多个AB包，或者和共享AB包。\n一般按照游戏阶段分GameAssetPackage，比如：启动阶段，100级解锁的功能阶段等。")]
        public GameAssetPackage[] GameAssetPackageList = Array.Empty<GameAssetPackage>();
        
        [LabelText("预览Manifest Editor")]
        [NonSerialized]
        public ManifestContext ManifestContext;
        
        [LabelText("预览Manifest Runtime")]
        [NonSerialized]
        public Manifest ManifestRuntime;
        
        private bool ValidateGameAssetPackageList(GameAssetPackage[] gameAssetPackageList, ref string errorMessage)
        {
            // 检查是否有重复的AB包名
            using (HashSetPool<string>.Get(out var repeatNameSet))
            using (HashSetPool<string>.Get(out var nameSet))
            {
                foreach (var gameAssetPackage in gameAssetPackageList)
                {
                    if (nameSet.Contains(gameAssetPackage.Name))
                    {
                        repeatNameSet.Add(gameAssetPackage.Name);
                    }
                    nameSet.Add(gameAssetPackage.Name);
                }

                if (repeatNameSet.Count > 0)
                {
                    errorMessage = $"游戏资产包名重复：{string.Join(", ", repeatNameSet)}";
                    return false;
                }
            }
            return true;
        }

        public void BuildAssetBundleByConfig(BuildTarget targetPlatform)
        {
            BuildAssetBundle(false, DefaultOutputPath, BuildAssetBundleParameters, targetPlatform);
        }
        
        public void BuildAssetBundle(bool isPreview, string outputPath, AssetBundleBuildParameters buildAssetBundleParameters, BuildTarget targetPlatform)
        {
            ManifestContext = new ManifestContext();
            
            // 收集GameAsset，构建AssetFileInfo之间的依赖关系
            foreach (var gameAssetPackage in GameAssetPackageList)
            {
                foreach (var gameAssetCollector in gameAssetPackage.GameAssetCollectorList)
                {
                    gameAssetCollector.Collect(gameAssetPackage, ManifestContext);
                }
            }
            
            // 生成 GameAssetPackageInfo 和 GameAssetShareInfo
            foreach (var pair in ManifestContext.GameAssetInfoMap)
            {
                var gameAssetInfo = pair.Value;
                foreach (var gameAssetPackage in gameAssetInfo.GameAssetPackages)
                {
                    ManifestContext.GetOrAddGameAssetPackageInfo(gameAssetPackage.Name, gameAssetInfo);
                }
            }
            
            // AssetFileInfo 填充 GameAssetPackageInfo 和 GameAssetSharePackageInfo 依赖
            foreach (var gameAssetInfo in ManifestContext.GameAssetInfoMap.Values)
            {
                var isGameAssetPackageInfo = gameAssetInfo.GameAssetPackageInfo != null;
                if(isGameAssetPackageInfo)
                    gameAssetInfo.AssetFile.GameAssetPackageInfoSet.Add(gameAssetInfo.GameAssetPackageInfo);
                else
                    gameAssetInfo.AssetFile.GameAssetSharePackageInfoSet.Add(gameAssetInfo.GameAssetSharePackageInfo);
                foreach (var dependency in gameAssetInfo.AssetFile.Dependencies)
                {
                    if(isGameAssetPackageInfo)
                        dependency.GameAssetPackageInfoSet.Add(gameAssetInfo.GameAssetPackageInfo);
                    else
                        dependency.GameAssetSharePackageInfoSet.Add(gameAssetInfo.GameAssetSharePackageInfo);
                }
            }
            
            // 第一轮 AssetBundleName 标记，根据 GameAssetPackageInfo 和 GameAssetSharePackageInfo 标记 AssetBundleName
            var nameList = new List<string>();
            foreach (var assetFileInfo in ManifestContext.AssetFileMap.Values)
            {
                var assetBundleName = "GameAssetPackage";
                if(assetFileInfo.GameAssetPackageInfoSet.Count > 0)
                {
                    nameList.Clear();
                    foreach (var gameAssetPackageInfo in assetFileInfo.GameAssetPackageInfoSet)
                        nameList.Add(gameAssetPackageInfo.Name);
                    nameList.Sort();
                    foreach (var name in nameList)
                        assetBundleName += $"_{name}";
                    assetFileInfo.AssetBundleName = assetBundleName;
                }
                if(assetFileInfo.GameAssetSharePackageInfoSet.Count > 0)
                {
                    nameList.Clear();
                    foreach (var gameAssetSharePackageInfo in assetFileInfo.GameAssetSharePackageInfoSet)
                        nameList.Add(gameAssetSharePackageInfo.Name);
                    nameList.Sort();
                    foreach (var name in nameList)
                        assetBundleName += $"_{name}";
                    assetFileInfo.AssetBundleName = assetBundleName;
                }
            }
            
            // 第二轮 AssetBundleName 标记，特殊资源
            var atlasGUIDList = AssetDatabase.FindAssets("t:spriteatlas");
            var spritePathToAtlasMap = new Dictionary<string, SpriteAtlas>();
            var assetBundleNameToAtlasMap = new Dictionary<string, SpriteAtlas>();
            foreach (var atlasGUID in atlasGUIDList)
            {
                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(atlasGUID));
                var dependencies = AssetDatabase.GetDependencies(AssetDatabase.GUIDToAssetPath(atlasGUID), false);
                foreach (var dependency in dependencies)
                {
                    spritePathToAtlasMap[dependency] = atlas;
                }
            }
            foreach (var assetFileInfo in ManifestContext.AssetFileMap.Values)
            {
                if (assetFileInfo.IsShaderAsset())
                {
                    assetFileInfo.AssetBundleName = "ShaderAssetBundle";
                    assetFileInfo.ForceAssetBundle = true;
                }
                else if (spritePathToAtlasMap.TryGetValue(assetFileInfo.AssetPath, out var atlas))
                {
                    assetFileInfo.AssetBundleName = $"SpriteAtlas_{atlas.name}";
                    assetFileInfo.ForceAssetBundle = true;
                    assetBundleNameToAtlasMap[assetFileInfo.AssetBundleName] = atlas;
                }
            }
            
            // 第三轮 AssetBundleName 标记，极小AB包合并
            if (MergeTinyAssetBundle)
            {
                var tinyAssetBundleMap = new Dictionary<string, HashSet<AssetFileInfo>>();
                foreach (var assetFileInfo in ManifestContext.AssetFileMap.Values)
                {
                    if (assetFileInfo.ForceAssetBundle || assetFileInfo.IsSceneAsset())
                        continue;
                    if (!tinyAssetBundleMap.TryGetValue(assetFileInfo.AssetBundleName, out var tinyAssetBundleSet))
                    {
                        tinyAssetBundleSet = new HashSet<AssetFileInfo>();
                        tinyAssetBundleMap[assetFileInfo.AssetBundleName] = tinyAssetBundleSet;
                    }
                    tinyAssetBundleSet.Add(assetFileInfo);
                }
                foreach (var pair in tinyAssetBundleMap)
                {
                    if (pair.Value.Count < TinyAssetBundleCount)
                    {
                        foreach (var assetFileInfo in pair.Value)
                        {
                            assetFileInfo.AssetBundleName = "MergeTinyAssetBundle";
                            assetFileInfo.ForceAssetBundle = true;
                        }
                    }
                }
            }
            
            // 创建 AssetBundleInfo
            foreach (var assetFileInfo in ManifestContext.AssetFileMap.Values)
            {
                ManifestContext.GetOrAddAssetBundleInfo(assetFileInfo);
            }
            
            // 创建 AssetBundleInfo 依赖关系
            foreach (var assetFileInfo in ManifestContext.AssetFileMap.Values)
            {
                var assetBundleInfo = ManifestContext.GetOrAddAssetBundleInfo(assetFileInfo);
                foreach (var dependency in assetFileInfo.Dependencies)
                {
                    var dependencyAssetBundleInfo = ManifestContext.GetOrAddAssetBundleInfo(dependency);
                    if (dependencyAssetBundleInfo != assetBundleInfo)
                    {
                        assetBundleInfo.Dependencies.Add(dependencyAssetBundleInfo);
                        assetBundleInfo.Dependencies.UnionWith(dependencyAssetBundleInfo.Dependencies);
                    }
                }
                foreach (var directDependency in assetFileInfo.DirectDependencies)
                {
                    var dependencyAssetBundleInfo = ManifestContext.GetOrAddAssetBundleInfo(directDependency);
                    if (dependencyAssetBundleInfo != assetBundleInfo)
                    {
                        assetBundleInfo.DirectDependencies.Add(dependencyAssetBundleInfo);
                        assetBundleInfo.DirectDependencies.UnionWith(dependencyAssetBundleInfo.Dependencies);
                    }
                
                }
            }
            
            // 将 AssetBundleInfo 关联到 GameAssetInfo 和 GameAssetPackageInfo
            foreach (var gameAssetPackage in ManifestContext.GameAssetPackageInfoMap.Values)
            {
                foreach (var gameAssetInfo in gameAssetPackage.GameAssetInfoSet)
                {
                    var assetBundleInfo = ManifestContext.GetOrAddAssetBundleInfo(gameAssetInfo.AssetFile);
                    gameAssetInfo.AssetBundleInfos.Add(assetBundleInfo);
                    gameAssetInfo.AssetBundleInfos.UnionWith(assetBundleInfo.Dependencies);
                    gameAssetPackage.AssetBundleInfos.Add(assetBundleInfo);
                    gameAssetPackage.AssetBundleInfos.UnionWith(assetBundleInfo.Dependencies);
                    
                    foreach (var dependency in gameAssetInfo.AssetFile.Dependencies)
                    {
                        var dependencyAssetBundleInfo = ManifestContext.GetOrAddAssetBundleInfo(dependency);
                        gameAssetInfo.AssetBundleInfos.Add(dependencyAssetBundleInfo);
                        gameAssetInfo.AssetBundleInfos.UnionWith(dependencyAssetBundleInfo.Dependencies);
                        gameAssetPackage.AssetBundleInfos.Add(dependencyAssetBundleInfo);
                        gameAssetPackage.AssetBundleInfos.UnionWith(dependencyAssetBundleInfo.Dependencies);
                    }
                }
            }
            
            // 将图集填入到AssetBundleInfo
            foreach (var assetBundleInfo in ManifestContext.AssetBundleInfoMap.Values)
            {
                if(assetBundleNameToAtlasMap.TryGetValue(assetBundleInfo.Name, out var atlas)) // 把图集打进去，避免散图冗余
                {
                    var assetFileInfo = ManifestContext.GetOrAddAssetFileInfo(AssetDatabase.GetAssetPath(atlas));
                    assetFileInfo.AssetBundleName = assetBundleInfo.Name;
                    assetFileInfo.ForceAssetBundle = true;
                    ManifestContext.GetOrAddAssetBundleInfo(assetFileInfo);
                }
            }
            
            // 标记不需要明确指定进入AB包的AssetFileInfo（通过unity的自动依赖进入AB）
            foreach (var assetFileInfo in ManifestContext.AssetFileMap.Values)
            {
                if (!assetFileInfo.NeedExplicitAssetBundleName())
                {
                    assetFileInfo.AssetBundleName = null;
                }
            }
            
            // 实际AB包生成
            if (!isPreview)
            {
                // 给包含GameAsset的AssetBundle都生成对应的AssetBundleManifest
                AssetDatabase.StartAssetEditing();
                foreach (var assetBundleInfo in ManifestContext.AssetBundleInfoMap.Values)
                {
                    var assetBundleManifest = ScriptableObject.CreateInstance<AssetBundleManifest>();
                    assetBundleManifest.name = assetBundleInfo.Name;
                    if(!Directory.Exists("Assets/BuildAssetBundleTemp"))
                        AssetDatabase.CreateFolder("Assets", "BuildAssetBundleTemp");
                    AssetDatabase.CreateAsset(assetBundleManifest, assetBundleInfo.AssetBundleManifestPath);
                    assetBundleInfo.AssetBundleManifest = assetBundleManifest;

                    var isContainScene = false;
                    foreach (var assetFileInfo in assetBundleInfo.AssetFileInfoSet)
                    {
                        foreach (var gameAssetInfo in assetFileInfo.GameAssetInfoSet)
                        {
                            assetBundleManifest.AddGameAsset(gameAssetInfo.Asset);
                            if(assetFileInfo.IsSceneAsset())
                                isContainScene = true;
                        }
                    }

                    if (assetBundleManifest.GameAssetMap.Count == 0 || isContainScene) //没有GameAsset就不填入，场景AB包不能混合其他资产
                    {
                        assetBundleInfo.AssetBundleManifest = null;
                    }
                }
                AssetDatabase.StopAssetEditing();
                
                // 生成Unity的AssetBundle信息
                List<AssetBundleBuild> builds = new();
                List<string> assetNames = new();
                foreach (var assetBundleInfo in ManifestContext.AssetBundleInfoMap.Values)
                {
                    assetNames.Clear();
                    var assetBundleBuild = new AssetBundleBuild
                    {
                        assetBundleName = assetBundleInfo.Name,
                    };
                    if(assetBundleInfo.AssetBundleManifest != null)
                        assetNames.Add(assetBundleInfo.AssetBundleManifestPath);
                    foreach (var assetFileInfo in assetBundleInfo.AssetFileInfoSet)
                    {
                        if(!string.IsNullOrEmpty(assetFileInfo.AssetBundleName))
                            assetNames.Add(assetFileInfo.AssetPath);
                    }

                    assetBundleBuild.assetNames = assetNames.ToArray();
                    builds.Add(assetBundleBuild);
                }

                // 开始打包
                var content = new BundleBuildContent(builds);
                var group = BuildPipeline.GetBuildTargetGroup(targetPlatform);
                var parameters = buildAssetBundleParameters.ToBundleBuildParameters(targetPlatform, group, outputPath);
                
                // 清空输出目录
                if (Directory.Exists(outputPath))
                {
                    Directory.Delete(outputPath, true);
                }
                
                IBundleBuildResults results;
                ReturnCode exitCode = ContentPipeline.BuildAssetBundles(parameters, content, out results);
                
                // 清理临时文件
                AssetDatabase.DeleteAsset("Assets/BuildAssetBundleTemp");
                
                if (exitCode != ReturnCode.Success)
                {
                    throw new Exception($"BuildAssetBundles failed: {exitCode.ToString()}");
                }

                // 将AB包文件名改成Hash名
                if (buildAssetBundleParameters.UseHashFileName)
                {
                    // 项目路径
                    var rootPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
                    foreach (var pair in results.BundleInfos)
                    {
                        var filePath = Path.GetFullPath(pair.Value.FileName, rootPath);
                        var hashFilePath = Path.GetFullPath(Path.Combine(outputPath, pair.Value.Hash.ToString()), rootPath);
                        FileUtil.MoveFileOrDirectory(filePath, hashFilePath);
                        FileUtil.MoveFileOrDirectory(filePath+".meta", hashFilePath+".meta");
                    }
                }
                
                foreach (var pair in results.BundleInfos)
                {
                    var assetBundleInfo = ManifestContext.AssetBundleInfoMap[pair.Key];
                    assetBundleInfo.BuildFileName = buildAssetBundleParameters.UseHashFileName ? pair.Value.Hash.ToString() : Path.GetFileName(pair.Value.FileName);
                    assetBundleInfo.Crc = pair.Value.Crc;
                }
            }
            
            // 生成 Manifest
            ManifestRuntime = new Manifest();
            var manifestRuntime_GameAssetPackages = new List<GameAssetPackageRuntime>();
            var assetBundleRuntimeMap = new Dictionary<string, AssetBundleRuntime>();
            var gameAssetRuntimeMap = new Dictionary<(Guid, long), GameAssetRuntime>();
            foreach (var gameAssetPackageInfo in ManifestContext.GameAssetPackageInfoMap.Values)
            {
                var gameAssetPackageRuntime = new GameAssetPackageRuntime
                {
                    Name = gameAssetPackageInfo.Name,
                };
                manifestRuntime_GameAssetPackages.Add(gameAssetPackageRuntime);
                
                foreach (var assetBundleInfo in gameAssetPackageInfo.AssetBundleInfos)
                {
                    if(!assetBundleRuntimeMap.TryGetValue(assetBundleInfo.Name, out var assetBundleRuntime))
                    {
                        assetBundleRuntime = new AssetBundleRuntime
                        {
                            Name = assetBundleInfo.Name,
                            FileName = assetBundleInfo.BuildFileName,
                            Crc = assetBundleInfo.Crc,
                        };
                        assetBundleRuntimeMap[assetBundleInfo.Name] = assetBundleRuntime;
                    }
                    foreach (var assetFileInfo in assetBundleInfo.AssetFileInfoSet)
                    {
                        if(assetFileInfo.IsSceneAsset())
                            assetBundleRuntime.IsContainScene = true;
                        foreach (var gameAssetInfo in assetFileInfo.GameAssetInfoSet)
                        {
                            var key = (gameAssetInfo.AssetGuid, gameAssetInfo.LocalId);
                            if(!gameAssetRuntimeMap.TryGetValue(key, out var gameAssetRuntime))
                            {
                                gameAssetRuntime = new GameAssetRuntime
                                {
                                    AssetGuid = gameAssetInfo.AssetGuid,
                                    LocalId = gameAssetInfo.LocalId,
                                    Asset = gameAssetInfo.Asset,
                                };
                                gameAssetRuntime.Aliases.Add(gameAssetInfo.AssetPath);
                                gameAssetRuntime.Aliases.AddRange(gameAssetInfo.Aliases);
                                gameAssetRuntimeMap[key] = gameAssetRuntime;
                            }
                            assetBundleRuntime.GameAssetRuntimesTemp.Add(gameAssetRuntime);
                        }
                        assetBundleRuntime.MainAssetList.Add(assetFileInfo.MainAsset);
                    }
                    gameAssetPackageRuntime.AssetBundlesTemp.Add(assetBundleRuntime);
                }
                gameAssetPackageRuntime.AssetBundles = gameAssetPackageRuntime.AssetBundlesTemp.ToArray();
            }
            foreach (var assetBundleRuntime in assetBundleRuntimeMap.Values)
            {
                assetBundleRuntime.GameAssetRuntimes = assetBundleRuntime.GameAssetRuntimesTemp.ToArray();
            }
            
            var manifestRuntime_AssetBundleList = new List<AssetBundleRuntime>();
            var manifestRuntime_AssetBundleRuntime_Dependencies = new List<AssetBundleRuntime>();
            foreach (var assetBundleInfo in ManifestContext.AssetBundleInfoMap.Values)
            {
                var assetBundleRuntime = assetBundleRuntimeMap[assetBundleInfo.Name];
                manifestRuntime_AssetBundleList.Add(assetBundleRuntime);
                
                // 填充assetBundleRuntime之间的依赖
                manifestRuntime_AssetBundleRuntime_Dependencies.Clear();
                foreach (var dependency in assetBundleInfo.Dependencies)
                {
                    var dependencyAssetBundleRuntime = assetBundleRuntimeMap[dependency.Name];
                    manifestRuntime_AssetBundleRuntime_Dependencies.Add(dependencyAssetBundleRuntime);
                }
                assetBundleRuntime.Dependencies = manifestRuntime_AssetBundleRuntime_Dependencies.ToArray();
            }
            ManifestRuntime.GameAssetPackages = manifestRuntime_GameAssetPackages.ToArray();
            ManifestRuntime.AssetBundleList = manifestRuntime_AssetBundleList.ToArray();

            if (!isPreview)
            {
                AssetBundleRuntimeConfig.Instance.Manifest = ManifestRuntime;
                EditorUtility.SetDirty(AssetBundleRuntimeConfig.Instance);
                AssetDatabase.SaveAssetIfDirty(AssetBundleRuntimeConfig.Instance);
            }
            
            GameAssetPackagePreviewWindow.OnDataUpdate();
        }

        // 方便反射获取实例
        public static AssetBundleEditorConfig GetInstance()
        {
            return Instance;
        }
    }

    [Serializable, InlineProperty, HideReferenceObjectPicker]
    public class AssetBundleBuildParameters
    {
        [Tooltip("AB文件名使用内容的Hash")]
        public bool UseHashFileName;

        [Tooltip("按照MainAssetType来排序一个AB中的资源顺序，LoadAllAssets会快一点点")]
        public bool ContiguousBundles;

        [Tooltip("禁用SubAsset的可见性，加快打包速度，会导致无法直接使用LoadAsset等接口从AB包中加载SubAsset")]
        public bool DisableVisibleSubAssetRepresentations;

        [Tooltip("压缩类型")]
        public CompressionType Compression;

        public ContentBuildFlags ContentBuildFlags;

        public bool UseCache;

        public bool WriteLinkXML;

        public bool NonRecursiveDependencies;

        public AssetBundleBuildParameters()
        {
            UseHashFileName = true;
            ContiguousBundles = true;
            DisableVisibleSubAssetRepresentations = true;
            Compression = CompressionType.LZMA;
            ContentBuildFlags = ContentBuildFlags.DisableWriteTypeTree;
            UseCache = false;
            WriteLinkXML = true;
            NonRecursiveDependencies = true;
        }

        public BundleBuildParameters ToBundleBuildParameters(BuildTarget target, BuildTargetGroup group, string outputFolder)
        {
            var parameters = new BundleBuildParameters(target, group, outputFolder)
            {
                AppendHash = UseHashFileName,
                ContiguousBundles = ContiguousBundles,
                DisableVisibleSubAssetRepresentations = DisableVisibleSubAssetRepresentations,
                BundleCompression = Compression switch
                {
                    CompressionType.LZMA => BuildCompression.LZMA,
                    CompressionType.LZ4 => BuildCompression.LZ4,
                    CompressionType.Uncompressed => BuildCompression.Uncompressed,
                    _ => BuildCompression.LZMA,
                },
                ContentBuildFlags = ContentBuildFlags,
                UseCache = UseCache,
                WriteLinkXML = WriteLinkXML,
                NonRecursiveDependencies = NonRecursiveDependencies,
            };
            return parameters;
        }
        
        public enum CompressionType
        {
            LZMA,
            LZ4,
            Uncompressed,
        }
    }
}