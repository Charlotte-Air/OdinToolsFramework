using UnityEditor;

namespace LightWeightUI.Editor
{
    public static class EditorUtility
    {
        [MenuItem("Assets/Create/LightWeightUI/UIPage")]
        public static void CreateUIPage()
        {
            var templatePath = "Assets/LightWeightUI/Editor/UIPageTemplate.prefab";
            var template = AssetDatabase.LoadAssetAtPath(templatePath, typeof(UnityEngine.Object));
            if (template == null)
            {
                UnityEngine.Debug.LogError($"Can't find template at {templatePath}");
                return;
            }
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }
            else if (System.IO.Path.GetExtension(path) != "")
            {
                path = path.Replace(System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }
            var name = "New UIPage";
            var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath($"{path}/{name}.prefab");
            AssetDatabase.CopyAsset(templatePath, assetPathAndName);
            AssetDatabase.RenameAsset(assetPathAndName, name);
            var asset = AssetDatabase.LoadAssetAtPath(assetPathAndName, typeof(UnityEngine.Object));
            Selection.activeObject = asset;
        }
    }
}