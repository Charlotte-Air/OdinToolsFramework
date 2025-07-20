using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using Sirenix.OdinInspector.Editor;

namespace Framework.Utility.WeakReference
{
    public class SceneGameObjectWeakRefDrawer : OdinValueDrawer<SceneGameObjectWeakRef>
    {
        private bool _init = false;
        private GameObject _cacheObject;
        private GlobalObjectId _cacheGlobalObjectId;
        private Object _cacheSceneAsset;
        private bool _needCheckScenePathOnly;
        private bool _cacheCheckScenePathOnly = true;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var currentValue = ValueEntry.SmartValue;

            if (Application.isPlaying)
            {
                if(label != null)
                    EditorGUILayout.TextField(label, currentValue.TargetScenePath);
                else
                    EditorGUILayout.TextField(currentValue.TargetScenePath);
                return;
            }

            if (_init == false)
            {
                _init = true;
                _needCheckScenePathOnly = true;
                if (string.IsNullOrEmpty(currentValue.GlobalObjectId) == false)
                {
                    if (GlobalObjectId.TryParse(currentValue.GlobalObjectId, out _cacheGlobalObjectId))
                    {
                        _cacheObject = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(_cacheGlobalObjectId) as GameObject;
                    }
                }
            }

            // 因为 GlobalObjectId 需要在场景已经加载的时候才能找到具体对象，这里先检查场景是否加载
            if (_cacheObject == null && _cacheGlobalObjectId.identifierType == 2)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(_cacheGlobalObjectId.assetGUID.ToString());
                var scene = EditorSceneManager.GetSceneByPath(scenePath);
                if (scene.IsValid() && scene.isLoaded)
                {
                    _cacheObject = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(_cacheGlobalObjectId) as GameObject;
                }
                else
                {
                    if(_cacheSceneAsset == null)
                        _cacheSceneAsset = AssetDatabase.LoadAssetAtPath<Object>(scenePath);
                    EditorGUILayout.HelpBox("需要打开目标对象所在关卡才能解析！", MessageType.Warning);
                    EditorGUILayout.BeginHorizontal();
                    if(label != null)
                        EditorGUILayout.ObjectField(label, _cacheSceneAsset, typeof(Object), allowSceneObjects: false);
                    else
                        EditorGUILayout.ObjectField(_cacheSceneAsset, typeof(Object), allowSceneObjects: false);
                    if(GUILayout.Button("打开关卡"))
                    {
                        EditorSceneManager.OpenScene(scenePath);
                    }
                    EditorGUILayout.EndHorizontal();
                    if(label != null)
                        EditorGUILayout.TextField(label, currentValue.TargetScenePath);
                    else
                        EditorGUILayout.TextField(currentValue.TargetScenePath);
                }
            }

            if (_cacheObject != null)
            {
                // 检查当前对象在场景中的路径是否有变化
                string targetPath = GetTargetScenePath(_cacheObject);
                if (targetPath != currentValue.TargetScenePath)
                {
                    EditorGUILayout.HelpBox("当前对象在场景中的路径已经发生变化，需要更新路径，否则在运行时找不到！", MessageType.Error);
                    if(GUILayout.Button("更新路径"))
                    {
                        currentValue.TargetScenePath = targetPath;
                        ValueEntry.SmartValue = currentValue;
                        _needCheckScenePathOnly = true;
                    }
                }
                else
                {
                    if(_needCheckScenePathOnly) // 检查通过路径是否会找到多个对象
                    {
                        _needCheckScenePathOnly = false;
                        var b = currentValue.FindGameObject();
                        _cacheCheckScenePathOnly = b == _cacheObject;
                    }
                    if (!_cacheCheckScenePathOnly)
                    {
                        EditorGUILayout.HelpBox("当前路径下存在多个对象！", MessageType.Error);
                    }
                }
            }
            
            if (string.IsNullOrEmpty(currentValue.GlobalObjectId) || _cacheObject != null)
            {
                GameObject go = label != null ? 
                    (GameObject)EditorGUILayout.ObjectField(label, _cacheObject, typeof(GameObject), true) :
                    (GameObject)EditorGUILayout.ObjectField(_cacheObject, typeof(GameObject), true);
                if (go != _cacheObject)
                {
                    // 检查是否是场景物体
                    if (go != null && go.scene.name == null)
                    {
                        Debug.LogWarning("请确保选择的物体是场景物体");
                        return;
                    }
                
                    _cacheObject = go;
                    _cacheSceneAsset = AssetDatabase.LoadAssetAtPath<Object>(_cacheObject.scene.path);
                    if (go != null)
                    {
                        currentValue.GlobalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();
                        currentValue.TargetScenePath = GetTargetScenePath(go);
                    }
                    else
                    {
                        currentValue.GlobalObjectId = null;
                        currentValue.TargetScenePath = null;
                    }
                    ValueEntry.SmartValue = currentValue;
                    _needCheckScenePathOnly = true;
                }
            }
        }
        
        private string GetTargetScenePath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}