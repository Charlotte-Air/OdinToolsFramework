using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using Framework.Utility;
using LightWeightUI.Preload;
using Cysharp.Threading.Tasks;
using Singer.Scripts.Tutorial;
using System.Collections.Generic;
using Framework.Utility.Extensions;

namespace LightWeightUI
{
    public struct PageOpenEvent
    {
        public UIPage Page;
    }
    
    public struct PageCloseEvent
    {
        public UIPage Page;
    }
    
    /// <summary>最外层UI总控脚本</summary>
    public partial class UIManager
    {
        public static UIManager Instance { get; private set; }
        
        private Canvas RootCanvas;
        public Camera UICamera { get; private set; }
        public CanvasGroup RootCanvasGroup { get; private set; }

        private List<UILayer> layerStack;

        private Dictionary<UILayer, UIStack> pageStackDic;

        private Dictionary<UIPage, CancellationTokenSource> animatingPages;

        /// <summary>正在打开或关闭流程中的页面</summary>
        private HashSet<UIPage> _inFlowPages;

        /// <summary>UI资源加载器</summary>
        private IResLoader resLoader;
        
        private IUIPreloadTool uiPreloader;

        [SerializeField] private Image RootTransparentBlock;

        public void Init(Canvas rootCanvas, Canvas worldUICanvas, Camera uiCamera, IUIPreloadTool uiPreloader)
        {
            Instance = this;
            this.uiPreloader = uiPreloader;
            resLoader = new YooAssetsLoader();
            
            //初始化根画布
            RootCanvas = rootCanvas;
            WorldUICanvas = worldUICanvas;
            UICamera = uiCamera;
            RootCanvasGroup = RootCanvas.transform.GetOrAddComponent<CanvasGroup>();
            animatingPages = new();
            _inFlowPages = new();

            //根据层级配置生成各层级
            GenerateLayers();

            InitWorldUIPart();
        }

        public void Tick()
        {
            //处理的返回事件，主要用于移动端
            if (!TutorialManager.IsGuideRunning() && Input.GetKeyDown(KeyCode.Escape))
            {
                OnMobileNativeBackEvent();
            }
        }
        
        private void GenerateLayers()
        {
            var layerSettings = UIConfig.Instance.LayerSetting;
            //根据渲染顺序重新排列 重要！layerStack中顺序影响模态控制
            layerSettings = layerSettings.OrderBy((layer) => layer.SortingOrder).ToList();
            layerStack = new List<UILayer>();
            pageStackDic = new Dictionary<UILayer, UIStack>();
            foreach (var layerConfig in layerSettings)
            {
                layerStack.Add(InitLayer(layerConfig));
            }
        }

        private UILayer InitLayer(UILayerConfig layerConfig)
        {
            Canvas thisCanvas = new GameObject($"{layerConfig.LayerName} Layer").AddComponent<Canvas>();
            //设置新画布的锚点数据
            var rect = thisCanvas.GetComponent<RectTransform>();
            var parentRect = RootCanvas.transform.GetComponent<RectTransform>();
            rect.SetAndStretchToParentSize(parentRect);

            var newLayer = thisCanvas.gameObject.AddComponent<UILayer>();
            //修改画布的层级
            newLayer.gameObject.layer = LayerMask.NameToLayer("UI");
            //初始化此层级的UIPage栈
            UIStack stack = new UIStack();
            pageStackDic.Add(newLayer, stack);
            newLayer.Init(layerConfig, ref stack, thisCanvas);
            newLayer.OnLayerModalChange = OnLayerModelChange;
            return newLayer;
        }

        private void OnLayerModelChange()
        {
            int topModelLayerIndex = -1;
            for (int i = layerStack.Count - 1; i >= 0; i--)
            {
                if (layerStack[i].IsModal)
                {
                    topModelLayerIndex = i;
                    break;
                }
            }

            if (topModelLayerIndex == -1)
            {
                //所有层级都不是模态,将所有层级都置为可交互
                foreach (var layerCon in layerStack)
                {
                    layerCon.SetLayerInteractable(true);
                }
            }
            else
            {
                //最高的模态层级之前所有层级不可交互
                for (int i = topModelLayerIndex; i >= 0; i--)
                {
                    layerStack[i].SetLayerInteractable(false);
                }

                //最高的模态层级之后所有层级可交互
                for (int i = topModelLayerIndex; i < layerStack.Count; i++)
                {
                    layerStack[i].SetLayerInteractable(true);
                }
            }
        }
        
        /// <summary>获取所有打开的页面ID历史,按照栈中从底到顶的顺序排列</summary>
        private List<UIPageId> GetAllOpenedPageIDHistory()
        {
            List<UIPageId> pageHistory = new();
            foreach (var layer in layerStack)
            {
                foreach (var page in pageStackDic[layer])
                {
                    pageHistory.Add(page.ID);
                }
            }

            return pageHistory;
        }

        private List<UIPage> _tempPageList = new();
        /// <summary>获取所有打开的页面，所有层级中的都算</summary>
        public List<UIPage> GetAllOpenedPage(bool pageLayerOnly = false)
        {
            _tempPageList.Clear();
            foreach (var layer in layerStack)
            {
                if(layer.IsPageLayer || !pageLayerOnly)
                    _tempPageList.AddRange(pageStackDic[layer]);
            }

            return _tempPageList;
        }
        
        public bool IsThisPageTopmost(UIPage page)
        {
            return IsThisPageTopmost(page.ID);
        }
        

        /// <summary>检查当前此页面是否在所有页面的最顶</summary>
        /// 信息条cover层, ui特效层, 不属于page页面
        public bool IsThisPageTopmost(UIPageId id)
        {
            for (int i = layerStack.Count - 1; i >= 0; i--)
            {
                if (layerStack[i].IsPageLayer)
                {
                    var topPage = pageStackDic[layerStack[i]].PeekTop();
                    if ( topPage!=null )
                    {
                        if ( topPage.ID == id )
                        {
                            return true;
                        }
                        break;
                    }
                }
            }
            return false;
        }

        /// <summary>获取所有栈顶最顶的页面</summary>
        public UIPage GetPageTopmost()
        {
            //层级栈有内容则peek顶
            foreach (var layer in layerStack)
            {
                if (pageStackDic[layer].Count > 0)
                {
                    return pageStackDic[layer].PeekTop();
                }
            }

            return null;
        }

        /// <summary>给定ID页面是否有打开过,不管页面是否被其他盖住,只要有打开就算</summary>
        public bool IsPageOpen(UIPageId id)
        {
            foreach (var pair in pageStackDic)
            {
                foreach (var page in pair.Value.instanceList)
                {
                    if (page.ID == id)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private UILayer GetLayerByName(string targetLayerName)
        {
            return layerStack.Where((layer) => { return string.Equals(layer.LayerName, targetLayerName); })
                .FirstOrDefault();
        }

        /// <summary>获取层级的transform</summary>
        public Transform GetLayerTransform(string targetLayerName)
        {
            var layer = GetLayerByName(targetLayerName);
            return layer == null ? null : layer.transform;
        }
        
        /// <summary>根据ID获取页面实例</summary>
        public UIPage GetPage(UIPageId id)
        {
            UIPage uiPage = null;
            foreach (var item in pageStackDic.Values)
            {
                foreach (var page in item)
                {
                    if (page.ID == id)
                    {
                        return page;
                    }
                }
            }

            return uiPage;
        }

        private void InternalOpenPageFlow(UIPage pageOpened, bool needAnim, bool isModal, Action<UIPage> onInit)
        {
            var belongedLayer = GetLayerByName(pageOpened.LayerName);
            //defaultPageBehaviour.OpenFlow(pageOpened, layer, needAnim, isModal);
            //处理面板是否模态
            pageOpened.IsModal = isModal;
            belongedLayer.SetLayerModal(isModal);
            if (isModal)
            {
                //是模态的话，关闭目前栈中所有页面的交互
                foreach (var oldPage in belongedLayer.pageStack)
                {
                    oldPage.CanvasGroup.interactable = false;
                }
            }

            //记录页面，此段流程不可被打断，防止在此期间的打开或关闭操作打断此次流程
            _inFlowPages.Add(pageOpened);
            //压栈，留缓存
            belongedLayer.pageStack.Push(pageOpened);
            //设置渲染顺序
            int i = 0;
            foreach (var page in belongedLayer.pageStack)
            {
                Canvas pageCanvas = page.gameObject.GetOrAddComponent<Canvas>();
                pageCanvas.overrideSorting = true;
                pageCanvas.sortingOrder = belongedLayer.LayerBase + (++i);
            }

            //抓CanvasGroup,关交互
            pageOpened.CanvasGroup.interactable = false;
            pageOpened.gameObject.SetActive(true);
            onInit?.Invoke(pageOpened);
            //page生命周期 OnOpenBefore
            pageOpened.OnOpenBefore();

            _inFlowPages.Remove(pageOpened);
            
            SetRootBlockActive(true);
            WaitForAnim(pageOpened, needAnim).Forget();
            MessageSystem.Send(new PageOpenEvent{Page = pageOpened});
        }

        private async UniTaskVoid InternalClosePageFlow(UIPage pageClose, bool needAnim)
        {
            var belongedLayer = GetLayerByName(pageClose.LayerName);
            //面板没打开直接返回
            if (!belongedLayer.pageStack.Contains(pageClose))
            {
                return;
            }

            //记录页面，此段流程不可被打断，防止在此期间的打开或关闭操作打断此次流程
            _inFlowPages.Add(pageClose);
            //检查是否栈顶，栈顶panel关闭要特殊处理
            if (belongedLayer.pageStack.IsTop(pageClose)) //栈顶
            {
                //数据先行，先将数据从栈中移除
                belongedLayer.pageStack.Pop();
                //拿改动后的栈顶页面，打开当前栈顶页面交互
                var topPanel = belongedLayer.pageStack.PeekTop();
                if (topPanel)
                {
                    belongedLayer.SetLayerModal(topPanel.IsModal);
                    //先关全部交互
                    foreach (var page in belongedLayer.pageStack)
                    {
                        page.CanvasGroup.interactable = false;
                    }

                    if (topPanel.IsModal)
                    {
                        //是模态，只打开栈顶页面自己的交互
                        topPanel.CanvasGroup.interactable = true;
                    }
                    else
                    {
                        //不是模态，交互打开直到下一个模态面板
                        var pageStack = belongedLayer.pageStack;
                        for (int i = pageStack.Count - 1; i >= 0; i--)
                        {
                            if (pageStack[i].IsModal)
                            {
                                pageStack[i].CanvasGroup.interactable = true;
                                break;
                            }

                            pageStack[i].CanvasGroup.interactable = true;
                        }
                    }
                }
                else
                {
                    //栈里没东西了层级也肯定不需要模态了
                    belongedLayer.SetLayerModal(false);
                }
            }
            else
            {
                //非栈顶
                belongedLayer.pageStack.Remove(pageClose);
            }

            //关交互
            pageClose.CanvasGroup.interactable = false;
            //page生命周期 OnCloseAfter
            pageClose.OnCloseBefore();
            //等Page处理完自己的关闭动画流程
            if (needAnim)
            {
                var cancelToken = AddPageAnimating(pageClose);
                await pageClose.CloseAnim(cancelToken);
                ReleasePageAnimating(pageClose);
            }

            pageClose.gameObject.SetActive(false);
            
            SetRootBlockActive(false);

            //移除记录，从这里开始可以再次打开或关闭
            _inFlowPages.Remove(pageClose);
            
            //page生命周期 OnCloseAfter
            pageClose.OnCloseAfter();
            
            MessageSystem.Send(new PageCloseEvent{Page = pageClose});
        }

        private async UniTaskVoid WaitForAnim(UIPage pageOpened, bool needAnim)
        {
            //等Page处理完自己负责的入场动画播放
            if (needAnim)
            {
                var cancelToken = AddPageAnimating(pageOpened);
                await pageOpened.OpenAnim(cancelToken);
                ReleasePageAnimating(pageOpened);
            }

            //恢复交互
            pageOpened.CanvasGroup.interactable = true;
            SetRootBlockActive(false);
            //移除记录，从这里开始可以再次打开或关闭
            _inFlowPages.Remove(pageOpened);
            //page生命周期 OnOpenAfter
            pageOpened.OnOpenAfter();
        }
        
        /// <summary>打开一个页面</summary>
        /// <param name="isModal">页面是否模态（模态页面打开后会屏蔽其下层的页面交互）</param>
        public UIPage OpenPage(UIPageId id, bool needAnim = true, bool isModal = false, Action<UIPage> onInit = null)
        {
            if (IsPageOpen(id))
            {
                //栈中已有此页面
                //根据ID获取页面实例
                UIPage uiPage = GetPage(id);

                if (null == uiPage)
                {
                    Debug.LogError($"UIManager 尝试重新打开已存在页面失败 页面ID:{id}");
                    return null;
                }

                if (_inFlowPages.Contains(uiPage))
                {
                    throw new Exception(
                        $"页面 [{id.ToString()}] 正在打开或关闭流程中，此时不可被打断。" +
                        $"可能在OnOpenBefore或OnCloseBefore中进行了页面开关操作,请考虑使用OnOpenAfter或OnCloseAfter。");
                }
                
                //页面从栈中移除
                var layer = GetLayerByName(uiPage.LayerName);
                pageStackDic[layer].Remove(uiPage);
                //重新打开在最顶端
                InternalOpenPageFlow(uiPage, needAnim, isModal, onInit);
                return uiPage;
            }
            else
            {
                //需要新实例的情况
                UIPage pageOpen = null;

                //从缓存中取预载好的页面
                if (!uiPreloader.TryGetPage(id, out pageOpen))
                {
                    //无池则未预载好,
                    pageOpen = uiPreloader.HandlePreloadNotFinish(id);
                }

                //将页面移动到其所属的层级下
                var layer = GetLayerByName(pageOpen.LayerName);
                pageOpen.transform.SetParent(layer.LayerRootCanvas.transform, false);
                
                //打开页面
                InternalOpenPageFlow(pageOpen, needAnim, isModal, onInit);
                return pageOpen;
            }
        }
        
        /// <summary>关闭一个页面</summary>
        public void ClosePage(UIPageId id, bool needAnim = true)
        {
            UIPage pageClosed = GetPage(id);
            if (!pageClosed)
            {
                Debug.LogWarning($"关闭页面失败,找不到已打开的ID为{id}的页面");
                return;
            }

            ClosePage(pageClosed, needAnim);
        }

        /// <summary>关闭一个页面</summary>
        public void ClosePage(UIPage pageClosed, bool needAnim = true)
        {
            if (_inFlowPages.Contains(pageClosed))
            {
                throw new Exception(
                    $"页面 [{pageClosed.ID.ToString()}] 正在打开或关闭流程中，此时不可被打断。" +
                    $"可能在OnOpenBefore或OnCloseBefore中进行了页面开关操作,请考虑使用OnOpenAfter或OnCloseAfter。");
            }
            
            InternalClosePageFlow(pageClosed, needAnim).Forget();
        }

        /// <summary>尝试打开页面，返回是否成功</summary>
        /// 与OpenPage的区别是，调用此方法时如果目标页面已打开，不会再走一遍打开流程
        public bool TryOpenPage(UIPageId id, bool needAnim = true, bool isModal = false)
        {
            if (IsPageOpen(id)) return false;
            OpenPage(id, needAnim, isModal);
            return true;
        }

        /// <summary>尝试关闭页面，返回是否成功</summary>
        public bool TryClosePage(UIPageId id, bool needAnim = true)
        {
            if (!IsPageOpen(id)) return false;
            ClosePage(id, needAnim);
            return true;
        }
        
        public void CloseAllPages(List<UIPageId> ignoreList = null, bool needAnim = false)
        {
            List<UIPage> deletedPages = new List<UIPage>();

            foreach (var layer in layerStack)
            {
                if (ignoreList != null)
                {
                    foreach (var page in layer.pageStack)
                    {
                        if (!ignoreList.Any((ignoreID) => { return ignoreID == page.ID; }))
                        {
                            deletedPages.Add(page);
                        }
                    }
                }
                else
                {
                    foreach (var page in layer.pageStack)
                    {
                        deletedPages.Add(page);
                    }
                }
            }

            for (int i = 0; i < deletedPages.Count; i++)
            {
                if (_inFlowPages.Contains(deletedPages[i]))
                {
                    throw new Exception(
                        $"页面 [{deletedPages[i].ID.ToString()}] 正在打开或关闭流程中，此时不可被打断。" +
                        $"可能在OnOpenBefore或OnCloseBefore中进行了页面开关操作,请考虑使用OnOpenAfter或OnCloseAfter。");
                }
                
                InternalClosePageFlow(deletedPages[i], needAnim);
            }
        }

        /// <summary>页面播放动画时记录此页面，若此页面已在播放动画则取消上一次动画</summary>
        /// <returns>会返回一个CancelToken，动画的实现中需要注册此Token被取消时的处理方法，动画发生冲突时Token的取消方法会被调用</returns>
        private CancellationToken AddPageAnimating(UIPage page)
        {
            if (animatingPages.TryGetValue(page, out var cts))
            {
                using (cts)
                {
                    cts.Cancel();
                    SetRootBlockActive(false); //取消动画时也需要关掉挡场景的遮罩 TODO 
                    animatingPages.Remove(page);
                }
            }

            var tokenSource = new CancellationTokenSource();
            animatingPages.Add(page, tokenSource);
            return tokenSource.Token;
        }

        /// <summary>页面动画播放完移除其记录</summary>
        private void ReleasePageAnimating(UIPage page)
        {
            animatingPages.Remove(page);
        }

        /// <summary>隐藏、打开整个UI，基本上只有Debug会用到</summary>
        public void ShowHideRoot()
        {
            RootCanvasGroup.alpha = RootCanvasGroup.alpha == 0 ? 1 : 0;
            // RootCanvas.gameObject.SetActive(!RootCanvas.gameObject.activeSelf);
        }

        public void SetRootBlockActive(bool bValue)
        {
            if (!RootTransparentBlock)
            {
                return;
            }

            RootTransparentBlock.gameObject.SetActive(bValue);
        }

        private void OnMobileNativeBackEvent()
        {
            bool eventDied = false;
            for (int i = layerStack.Count - 1; i >= 0; i--)
            {
                var uiStack = pageStackDic[layerStack[i]];
                if (uiStack.Count < 0) continue;
                for (int j = uiStack.Count - 1; j >= 0; j--)
                {
                    if (!uiStack.instanceList[j].OnMobileNativeBackEvent())
                    {
                        //页面返回False则事件不继续往下传播
                        eventDied = true;
                        break;
                    }
                }

                if (eventDied) break;
            }
        }
    }
}