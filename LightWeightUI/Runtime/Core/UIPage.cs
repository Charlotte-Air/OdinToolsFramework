using System;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace LightWeightUI
{
    /// <summary>UI页面的基类,增加新的页面由LightWeightUI管理需要继承此类</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract partial class UIPage : UIObject
    {
        [LabelText("层级"), ValueDropdown("GetLayerEnumerable"), PropertyOrder(-5), DisableInPlayMode]
        public string LayerName;

        [UIPageTag, LabelText("页面标签"), PropertyOrder(-4)]
        public List<UIPageTag> PageTags;

        public bool IsModal { get; set; }

        private CanvasGroup canvasGroup;

        /// <summary>画布组，统一控制UI物体的交互，透明度等数据</summary>
        public CanvasGroup CanvasGroup
        {
            get
            {
                if (!canvasGroup)
                {
                    canvasGroup = GetComponent<CanvasGroup>();
                }

                return canvasGroup;
            }
        }

        /// <summary>页面ID</summary>
        public UIPageId ID { get; private set; }

        public virtual void InitPage(UIPageId id)
        {
            ID = id;
        }

        private static IEnumerable<string> GetLayerEnumerable()
        {
            return UIConfig.Instance.LayerSetting.Select((layerConfig) => layerConfig.LayerName);
        }
        
        public bool HasTag(UIPageTag tag)
        {
            return PageTags.Contains(tag);
        }

        /// <summary>页面收到返回键事件</summary>
        /// <returns>如果处理完事件希望继续向下传递事件，返回true，不希望事件向下传递，返回false</returns>
        public virtual bool OnMobileNativeBackEvent()
        {
            return false;
        }
    }
}