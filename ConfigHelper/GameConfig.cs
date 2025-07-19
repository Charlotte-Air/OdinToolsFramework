using System.IO;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Framework.Utility.Extensions;

namespace Framework.ConfigHelper
{
    /// <summary>
    /// 快速创建游戏配置单例
    /// 会在编辑器的配置窗口中生成条目（项目->配置）
    /// 通过 CategoryAttribute 可以指定条目的分类
    /// </summary>
    public class GameConfig<T> : SerializedScriptableObject where T : GameConfig<T>
    {
        private static T _defaultInstance;
        public static string DefaultAssetPath => $"Assets/Resources/GameConfigs/{typeof(T).Name}.asset";
        public virtual GameConfigType[] GameConfigTypes { get; } = null;
        private static Dictionary<GameConfigType,T> _instances = new Dictionary<GameConfigType, T>();
        
        public static T Instance
        {
            get
            {
                if (!_defaultInstance)
                {
                    _defaultInstance = GetInstanceByType(GameConfigType.Default);
                }

                var instance = _defaultInstance;

                if (_defaultInstance.GameConfigTypes!=null)
                {
                    var types = GameConfigTypePlatFormConfig.Instance.GetGameConfigTypesNow();
                    if ( types != null )
                    {
                        foreach (var type in types)
                        {
                            if (_defaultInstance.GameConfigTypes.Contains(type))
                            {
                                if ( GetInstanceByType(type) )
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                
                return instance;
            }
        }

        public static string GetAssetPathByPlatform( GameConfigType type )
        {
            return $"Assets/Resources/GameConfigs/{type.ToString()}/{typeof(T).Name}.asset";
        }

        public static T GetInstanceByGameConfigType( GameConfigType type )
        {
            if ( _instances.ContainsKey(type) == false )
            {
                _instances.Add(type,null);
            }
            return _instances[type];
        }
        

        public static T GetInstanceByType( GameConfigType type )
        {
            var instance = type == GameConfigType.Default ? _defaultInstance : GetInstanceByGameConfigType(type);
            if (instance)
            {
                return instance;
            }
            
            var path = DefaultAssetPath;
            if ( type != GameConfigType.Default )
            {
                path = GetAssetPathByPlatform(type);
            }
            // Debug.Log($"GetInstanceByPlatform:{path}");

            if (Application.isPlaying)
            {
                instance ??= Resources.Load<T>(path.ToResourcesPath());
            }
#if UNITY_EDITOR
            else
            {
                UnityEditor.AssetDatabase.Refresh();
                instance = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                if (!instance)
                {
                    if (File.Exists(path)) // 引擎在重导入期间可能会加载失败
                        return null;
                    var asset = CreateInstance<T>();
                    // Utility.ReflexEditorTools.CreateAssetsFolder(path);
                    UnityEditor.AssetDatabase.CreateAsset(asset, path);
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();
                    instance = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                }
            }
#endif
            return instance;
        }

    }
}