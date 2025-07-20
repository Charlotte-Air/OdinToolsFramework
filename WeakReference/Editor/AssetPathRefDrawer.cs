using System;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector;
using Object = UnityEngine.Object;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Validation;

[assembly: RegisterValidator(typeof(AssetPathRefValidator))]
public class AssetPathRefValidator : ValueValidator<Framework.Utility.WeakReference.AssetPathRef>
{
    protected override void Validate(ValidationResult result)
    {
        // RequiredAttribute
        var requiredAttribute = GetAttribute<RequiredAttribute>(this.Property);
        var isRequired = requiredAttribute != null;

        var assetPathRef = this.ValueEntry.SmartValue;
            
        var assetObject = assetPathRef.EditorLoad<Object>();
        if (assetObject == null)
        {
            assetPathRef.GetValue(out Guid assetGuid, out long fileID);
            if (assetGuid == Guid.Empty)
            {
                if (isRequired)
                    result.AddError("资源不能为空");
            }
            else
                result.AddError($"资源无效 GUID:{assetPathRef.AssetGuidStr} FileID:{fileID}");
        }
    }
    
    private T GetAttribute<T>(InspectorProperty property) where T : Attribute
    {
        if (property == null)
            return null;
        var attribute = property.GetAttribute<T>();
        if (attribute != null)
            return attribute;
        return GetAttribute<T>(property.Parent);
    }
}

namespace Framework.Utility.WeakReference
{
    public class AssetPathRefDrawer : OdinValueDrawer<AssetPathRef>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            // AssetTypeAttribute
            var assetType = typeof(Object);
            var assetTypeAttribute = GetAttribute<AssetTypeAttribute>(this.Property);
            if(assetTypeAttribute != null)
                assetType = assetTypeAttribute.assetType;

            var assetPathRef = this.ValueEntry.SmartValue;
            
            var assetObject = assetPathRef.EditorLoad<Object>();
                
            //画EditorUI
            Rect rect = EditorGUILayout.GetControlRect();
            if (label != null)
                rect = EditorGUI.PrefixLabel(rect, label);
            var newObj = EditorGUI.ObjectField(rect, assetObject, assetType, false);
            
            //引用变动时
            if (newObj != assetObject)
            {
                AssetPathRef newRef = new AssetPathRef();
                if (newObj != null)
                {
                    // 判断类型是否是指定资源类型或子类型
                    if(!assetType.IsInstanceOfType(newObj))
                        return;
                    newRef.SetValue(newObj);
                }
                this.ValueEntry.SmartValue = newRef;
            }
        }

        private T GetAttribute<T>(InspectorProperty property) where T : Attribute
        {
            if (property == null)
                return null;
            var attribute = property.GetAttribute<T>();
            if (attribute != null)
                return attribute;
            return GetAttribute<T>(property.Parent);
        }
    }
}