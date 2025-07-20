using System;
using UnityEngine;
using System.Threading;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Framework.Utility.Extensions;

namespace LightWeightUI
{
    [Serializable]
    public struct UILayerConfig
    {
        [HorizontalGroup("LayerGrp"), LabelText("层级名")]
        public string LayerName;

        [HorizontalGroup("LayerGrp"), LabelText("排序（Sorting）")]
        public int SortingOrder;
        
        [HorizontalGroup("LayerGrp"), LabelText("是否为page层,判断顶层页面")]
        public bool isPageLayer;
    }

    public class UILayer : MonoBehaviour
    {
        /// <summary>存目前打开的页面栈，当前没有打开的页面不存在这里</summary>
        public UIStack pageStack { get; private set; }

        public string LayerName { get; private set; }

        protected Dictionary<UIPage, CancellationTokenSource> animatingPages;

        /// <summary>根画布</summary>
        public Canvas LayerRootCanvas { get; protected set; }

        /// <summary>层级的画布组</summary>
        protected CanvasGroup LayerCanvasGroup;

        /// <summary>层级排序基数</summary>
        public int LayerBase { get; private set; }
        
        /// <summary>该层级是否模态</summary>
        public bool IsModal { get; private set; }

        /// <summary>该层级是否是页面层</summary>
        public bool IsPageLayer { get; private set; }

        public Action OnLayerModalChange;

        /// <summary>初始化控制器需要的数据</summary>
        /// <param name="layerRootCanvas">本层的根画布</param>
        /// <param name="layerBase">层级的排序基础层级</param>
        /// <param name="loader">资源加载器</param>
        public virtual void Init(UILayerConfig layerConfig, ref UIStack stack, Canvas layerRootCanvas)
        {
            LayerRootCanvas = layerRootCanvas;
            LayerName = layerConfig.LayerName;
            LayerBase = layerConfig.SortingOrder;
            IsPageLayer = layerConfig.isPageLayer;
            pageStack = stack;
            animatingPages = new();
            layerRootCanvas.overrideSorting = true;
            layerRootCanvas.sortingOrder = layerConfig.SortingOrder;
            LayerCanvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
            LayerCanvasGroup.interactable = true;
        }

        /// <summary>设计层级模态是否开启</summary>
        /// <param name="bValue">true开启，false关闭</param>
        public void SetLayerModal(bool bValue)
        {
            IsModal = bValue;
            OnLayerModalChange?.Invoke();
        }

        /// <summary>设置此层是否可以交互</summary>
        /// <param name="bValue">打开交互传true，关闭交互传false</param>
        public void SetLayerInteractable(bool bValue)
        {
            LayerCanvasGroup.interactable = bValue;
        }

        /// <summary>处理动画冲突,处理办法为取消上一段冲突动画</summary>
        /// 用于处理页面在快速打开关闭的情况下,上一个动画还没播完,下一个动画就要开始播的问题
        /// <param name="page">要处理的页面</param>
        public void HandleAnimClash(UIPage page)
        {
            if (animatingPages.TryGetValue(page, out var cts))
            {
                using (cts)
                {
                    cts.Cancel();
                    //UIManager.Instance.SetRootBlockActive(false); //取消动画时也需要关掉挡场景的遮罩
                    animatingPages.Remove(page);
                }
            }
        }
    }
}