#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;

namespace Framework.ConfigHelper.Editor
{
    /// <summary>
    /// 快速创建编辑器配置单例
    /// 会在编辑器的配置窗口中生成条目（Custom->配置）
    /// 通过 CategoryAttribute 可以指定条目的分类
    /// </summary>
    public class EditorConfig<T> : SerializedScriptableObject where T : EditorConfig<T>
    {
        private static T _instance;
        
        public static string AssetPath => $"Assets/Settings/Configs/{typeof(T).Name}.asset";
        
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    AssetDatabase.Refresh();
                    _instance = AssetDatabase.LoadAssetAtPath<T>(AssetPath);
                    if (_instance == null)
                    {
                        if (File.Exists(AssetPath)) // 引擎在重导入期间可能会加载失败
                            return null;
                        var asset = CreateInstance<T>();
                        
                        var assetPath = AssetPath;
                        assetPath = assetPath.Replace("\\", "/");
                        // 去掉文件名
                        if (assetPath.Contains("."))
                            assetPath = assetPath.Substring(0, assetPath.LastIndexOf('/'));
                        string[] folders = assetPath.Split('/');
                        string path = Application.dataPath;
                        string relativePath = "Assets";
                        for (int i = 1; i < folders.Length; i++)
                        {
                            if (!Directory.Exists(path + "/" + folders[i]))
                            {
                                var guid = AssetDatabase.CreateFolder(relativePath, folders[i]);
                            }
                            path += "/" + folders[i];
                            relativePath += "/" + folders[i];
                        }
                        
                        AssetDatabase.CreateAsset(asset, AssetPath);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                    _instance = AssetDatabase.LoadAssetAtPath<T>(AssetPath);
                }
                return _instance;
            }
        }
    }
}

#endif