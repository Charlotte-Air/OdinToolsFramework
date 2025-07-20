using System.IO;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector.Editor;

namespace Framework.Utility.WeakReference
{
    public class GameObjectRelativePathDrawer : OdinValueDrawer<GameObjectRelativePath>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var currentValue = ValueEntry.SmartValue;
            var root = (ValueEntry.Property.Tree.WeakTargets[0] as MonoBehaviour)?.gameObject;
            
            EditorGUILayout.BeginHorizontal();
            if (label != null)
                EditorGUILayout.TextField(label, currentValue.RelativePath);
            else
                EditorGUILayout.TextField(currentValue.RelativePath);
            if (!string.IsNullOrEmpty(currentValue.RelativePath) && root != null && GUILayout.Button("尝试选中", GUILayout.Width(60)))
            {
                var target = currentValue.FindGameObject(root);
                if (target != null)
                    Selection.activeGameObject = target;
            }
            EditorGUILayout.EndHorizontal();
            
            // TextField区域接受拖拽GameObject
            var evt = Event.current;
            var dropArea = GUILayoutUtility.GetLastRect();
            if (evt.type == EventType.DragUpdated && dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform && dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.AcceptDrag();
                evt.Use();
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    var draggedObject = DragAndDrop.objectReferences[0] as GameObject;
                    if (draggedObject != null)
                    {
                        if (root != null)
                        {
                            currentValue.RelativePath = GetRelativePath(root, draggedObject);
                            ValueEntry.SmartValue = currentValue;
                        }
                    }
                }
            }
        }
        
        private string GetRelativePath(GameObject root, GameObject target)
        {
            string rootPath = GetAbsolutePath(root);
            string targetPath = GetAbsolutePath(target);
            return Path.GetRelativePath(rootPath, targetPath);
        }
        
        private string GetAbsolutePath(GameObject target)
        {
            string path = target.name;
            Transform parent = target.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}