using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Framework.Utility.Extensions;

namespace Framework.ConfigHelper
{
    [Serializable]
    public enum GameConfigPlatForm
    {
        Default,
        Android,
        IOS_GL,
        IOS_CN,
    }

    [Serializable]
    public class GameConfigTypePlatFormConfig : SerializedScriptableObject
    {
        private static GameConfigTypePlatFormConfig _instance;
        private static string AssetPath => $"Assets/Resources/{nameof(GameConfigTypePlatFormConfig)}.asset";

        public static GameConfigTypePlatFormConfig Instance
        {
            get
            {
                _instance ??= Resources.Load<GameConfigTypePlatFormConfig>(AssetPath.ToResourcesPath());
#if UNITY_EDITOR
                if (!_instance)
                {
                    UnityEditor.AssetDatabase.Refresh();
                    _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<GameConfigTypePlatFormConfig>(AssetPath);
                    if (!_instance)
                    {
                        if (System.IO.File.Exists(AssetPath)) // 引擎在重导入期间可能会加载失败，这里暂时报错了
                            throw new System.Exception($"配置加载失败！ {nameof(GameConfigTypePlatFormConfig)}");
                        var asset = CreateInstance<GameConfigTypePlatFormConfig>();
                        UnityEditor.AssetDatabase.CreateAsset(asset, AssetPath);
                        UnityEditor.AssetDatabase.SaveAssets();
                        UnityEditor.AssetDatabase.Refresh();
                        _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<GameConfigTypePlatFormConfig>(AssetPath);
                    }
                }
#endif
                return _instance;
            }
        }

        //configs
        public GameConfigPlatForm gameConfigPlatFormNow = GameConfigPlatForm.Default;

        private static GameConfigType[] AndroidGameConfigTypes { get; } = new GameConfigType[]
        {
            GameConfigType.Android,
        };

        private static GameConfigType[] IOSGameConfigTypes { get; } = new GameConfigType[]
        {
            GameConfigType.IOS_GL,
            GameConfigType.IOS_CN,
        };

        public GameConfigType[] GetGameConfigTypesNow()
        {
            return GetGameConfigTypesByPlatForm(gameConfigPlatFormNow);
        }

        public static GameConfigType[] GetGameConfigTypesByPlatForm(GameConfigPlatForm gameConfigPlatForm)
        {
            switch (gameConfigPlatForm)
            {
                case GameConfigPlatForm.Android:
                    return AndroidGameConfigTypes;
                case GameConfigPlatForm.IOS_GL:
                    return IOSGameConfigTypes;
                case GameConfigPlatForm.IOS_CN:
                    return IOSGameConfigTypes;
            }

            return null;
        }
    }
}