
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;

namespace LightWeightUI.Editor
{
    public class UIPageTagStringDrawer : OdinAttributeDrawer<UIPageTagAttribute, UIPageTag>
    {
        private GUIContent m_buttonContent = new GUIContent();

        protected override void Initialize() => UpdateButtonContent();

        private void UpdateButtonContent()
        {
            var fieldInfo = ValueEntry.SmartValue.GetType().GetField(ValueEntry.SmartValue.ToString());
            var labelAttrs = fieldInfo.GetCustomAttributes(typeof(LabelTextAttribute), true);
            if (labelAttrs.Length <= 0)
            {
                m_buttonContent.text = ValueEntry.SmartValue.ToString();
            }
            else
            {
                m_buttonContent.text = ((LabelTextAttribute)labelAttrs[0]).Text;
            }
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var rect = EditorGUILayout.GetControlRect(label != null);

            if (label == null)
                rect = EditorGUI.IndentedRect(rect);
            else
                rect = EditorGUI.PrefixLabel(rect, label);

            if (!EditorGUI.DropdownButton(rect, m_buttonContent, FocusType.Passive)) return;

            var selector = new GenericSelector<UIPageTag>(UIPageTagAttribute.Tags);
            selector.SetSelection(ValueEntry.SmartValue);
            selector.ShowInPopup(rect.position);

            selector.SelectionChanged += x =>
            {
                ValueEntry.Property.Tree.DelayAction(() =>
                {
                    ValueEntry.SmartValue = x.FirstOrDefault();

                    UpdateButtonContent();
                });
            };
        }
    }

    public abstract class UIPageTagStringListBaseDrawer<T> : OdinAttributeDrawer<UIPageTagAttribute, T>
        where T : IList<UIPageTag>
    {
        private GUIContent m_buttonContent = new GUIContent();

        protected override void Initialize() => UpdateButtonContent();

        private void UpdateButtonContent()
        {
            List<string> tagNames = new List<string>();

            if (null != ValueEntry.SmartValue)
            {
                foreach (var tag in ValueEntry.SmartValue)
                {
                    var fieldInfo = tag.GetType().GetField(tag.ToString());
                    var labelAttrs = fieldInfo.GetCustomAttributes(typeof(LabelTextAttribute), true);
                    if (labelAttrs.Length <= 0)
                    {
                        tagNames.Add(tag.ToString());
                    }
                    else
                    {
                        tagNames.Add(((LabelTextAttribute)labelAttrs[0]).Text);
                    }
                }
            }

            m_buttonContent.text = m_buttonContent.tooltip = string.Join(", ", tagNames);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var rect = EditorGUILayout.GetControlRect(label != null);


            if (label == null)
                rect = EditorGUI.IndentedRect(rect);
            else
            {
                var fieldName = ValueEntry.Property.Name;
                var fieldInfo = ValueEntry.ParentType.GetField(fieldName);
                var labelAttrs = fieldInfo.GetCustomAttributes(typeof(LabelTextAttribute), true);
                string labelName = labelAttrs.Length <= 0
                    ? ValueEntry.Values.ToString()
                    : ((LabelTextAttribute)labelAttrs[0]).Text;
                label.text = labelName;
                rect = EditorGUI.PrefixLabel(rect, label);
            }

            if (!EditorGUI.DropdownButton(rect, m_buttonContent, FocusType.Passive)) return;

            var selector = new UIPageTagSelector(UIPageTagAttribute.Tags);

            rect.y += rect.height;

            selector.SetSelection(ValueEntry.SmartValue);
            selector.ShowInPopup(rect.position);

            selector.SelectionChanged += x =>
            {
                ValueEntry.Property.Tree.DelayAction(() =>
                {
                    UpdateValue(x);

                    UpdateButtonContent();
                });
            };
        }

        protected abstract void UpdateValue(IEnumerable<UIPageTag> x);
    }

    [DrawerPriority(1)]
    [DontApplyToListElements]
    public class UIPageUIPageTagStringArrayDrawer : UIPageTagStringListBaseDrawer<UIPageTag[]>
    {
        protected override void UpdateValue(IEnumerable<UIPageTag> x) => ValueEntry.SmartValue = x.ToArray();
    }

    [DrawerPriority(1)]
    [DontApplyToListElements]
    public class UIPageUIPageTagStringListDrawer : UIPageTagStringListBaseDrawer<List<UIPageTag>>
    {
        protected override void UpdateValue(IEnumerable<UIPageTag> x) => ValueEntry.SmartValue = x.ToList();
    }

    public class UIPageTagSelector : GenericSelector<UIPageTag>
    {
        private FieldInfo m_requestCheckboxUpdate;

        public UIPageTagSelector(UIPageTag[] tags) : base(null, true, GetMenuItemName, tags)
        {
            CheckboxToggle = true;

            m_requestCheckboxUpdate = typeof(GenericSelector<UIPageTag>).GetField("requestCheckboxUpdate",
                BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private static string GetMenuItemName(UIPageTag tag)
        {
            var fieldInfo = tag.GetType().GetField(tag.ToString());
            var labelAttrs = fieldInfo.GetCustomAttributes(typeof(LabelTextAttribute), true);
            return labelAttrs.Length <= 0 ? tag.ToString() : ((LabelTextAttribute)labelAttrs[0]).Text;
        }

        protected override void DrawSelectionTree()
        {
            base.DrawSelectionTree();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("清空"))
            {
                SetSelection(new List<UIPageTag>());

                m_requestCheckboxUpdate.SetValue(this, true);
                TriggerSelectionChanged();
            }

            if (GUILayout.Button("全选"))
            {
                SetSelection(UIPageTagAttribute.Tags);

                m_requestCheckboxUpdate.SetValue(this, true);
                TriggerSelectionChanged();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}