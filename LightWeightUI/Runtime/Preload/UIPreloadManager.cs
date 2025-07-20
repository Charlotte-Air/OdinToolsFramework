using System;
using System.Linq;
using UnityEngine;
using System.Threading;
using System.Collections;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace LightWeightUI.Preload
{
    public class UIPreloadManager : IUIPreloadTool
    {
        /// <summary>是否打开调试输出日志</summary>
        private bool DebugLogEnable = true;

        /// <summary>UI资源加载器</summary>
        private IResLoader resLoader;

        /// <summary>加载好的页面预制体缓存</summary>
        private Dictionary<UIPageId, GameObject> pagePrefabCache;

        /// <summary>页面实例的缓存</summary>
        private Dictionary<UIPageId, UIPage> pageInstanceCache;

        /// <summary>正在进行中的加载任务</summary>
        private Dictionary<UIPageId, UIPrefabLoadTask> loadingTaskSet;

        /// <summary>UI的缓存画布</summary>
        /// 用画布缓存的原因是，如果直接实例化制体到画布之外，这些页面预制体中RectTransform的锚点等配置会全部丢失
        private Canvas uiCacheCanvas;

        private MonoBehaviour coroutineRunner;

        public void Init(MonoBehaviour coroutineRunner, Canvas cacheCanvas)
        {
            //项目使用YooAssets作为资源加载
            resLoader = new YooAssetsLoader();
            this.coroutineRunner = coroutineRunner;
            uiCacheCanvas = cacheCanvas;
            pagePrefabCache = new();
            pageInstanceCache = new();
            loadingTaskSet = new();
        }

        private IEnumerator HighPriorityLoadCoro(UIPageId pageID)
        {
            Log($"[UI预载 高优先级] ID为 {pageID} 的页面开始预载");
            yield return resLoader.LoadUIObjectCoroutine(pageID,
                uiPrefab => { pagePrefabCache.Add(pageID, uiPrefab); });
            Log($"[UI预载 高优先级] ID为 {pageID} 的页面预载完毕");
        }

        public IEnumerator PreloadPageHighPriority()
        {
            //读配置中的高优先级UI
            List<UIPageId> pagePriList = UIConfig.Instance.HighPriorityPage
                .OrderByDescending((item) => { return item.Value; })
                .Select((item) => { return item.Key; }).ToList();
            //parallel调协程
            List<Coroutine> coroList = new();
            foreach (var pageID in pagePriList)
            {
                coroList.Add(coroutineRunner.StartCoroutine(HighPriorityLoadCoro(pageID)));
            }

            foreach (var coro in coroList)
            {
                yield return coro;
            }
            //sequence调协程
            // foreach (var pageID in pagePriList)
            // {
            //     Log($"[UI预载 高优先级] ID为 {pageID} 的页面开始预载");
            //     yield return resLoader.LoadUIObjectCoroutine(pageID,
            //         uiPrefab => { pagePrefabCache.Add(pageID, uiPrefab); });
            //     Log($"[UI预载 高优先级] ID为 {pageID} 的页面预载完毕");
            // }
        }

        public void PreloadPageLowPriority()
        {
            //读配置中的低优先级页面,权重降序排列
            List<UIPageId> pagePriList = UIConfig.Instance.LowPriorityPage
                .OrderByDescending((item) => { return item.Value; })
                .Select((item) => { return item.Key; }).ToList();
            //补充配置中没有的页面到最后
            foreach (UIPageId pageID in (UIPageId[])Enum.GetValues(typeof(UIPageId)))
            {
                if (!pagePriList.Contains(pageID))
                {
                    pagePriList.Add(pageID);
                }
            }

            //清理掉已经预载好的页面
            foreach (var item in pagePrefabCache)
            {
                pagePriList.Remove(item.Key);
            }

            //开始页面的加载任务
            foreach (var pageID in pagePriList)
            {
                StartLoadTask(pageID).Forget();
            }
        }

        public bool TryGetPage(UIPageId id, out UIPage page)
        {
            page = null;
            if (!pageInstanceCache.TryGetValue(id, out UIPage cachedPage))
            {
                if (!pagePrefabCache.TryGetValue(id, out GameObject uiPrefab))
                {
                    return false;
                }

                //实例缓存中没有证明页面从未实例化过,进行实例化
                //var uiObj = Instantiate(uiPrefab, transform);
                var uiObj = GameObject.Instantiate(uiPrefab, uiCacheCanvas.transform);
                UIPage pageScri = uiObj.GetComponent<UIPage>();
                pageScri.InitPage(id);
                pageInstanceCache.Add(id, pageScri);
            }

            page = pageInstanceCache[id];
            if (null == page)
            {
                Debug.LogError($"加载出的ID为{id}的页面预制上找不到UIPage类脚本,请见检查UI资源配置是否正确");
            }

            return true;
        }

        public UIPage HandlePreloadNotFinish(UIPageId id)
        {
            if (loadingTaskSet.TryGetValue(id, out var prefabLoadTask))
            {
                Log($"ID为{id}页面的预载已经开始并且未加载完成,取消预载任务,转为同步加载页面");
                //加载任务已经开始则取消任务
                using (prefabLoadTask.Cts)
                {
                    prefabLoadTask.Cts.Cancel();
                }

                loadingTaskSet.Remove(id);
            }

            //直接同步加载页面
            var uiPrefab = resLoader.LoadUIObjectSync(id);
            //加到预制体缓存
            pagePrefabCache.Add(id, uiPrefab);
            //实例化页面
            //var uiObj = Instantiate(uiPrefab, transform);
            var uiObj = GameObject.Instantiate(uiPrefab, uiCacheCanvas.transform);
            UIPage pageScri = uiObj.GetComponent<UIPage>();
            pageScri.InitPage(id);
            pageInstanceCache.Add(id, pageScri);
            return pageScri;
        }

        private async UniTaskVoid StartLoadTask(UIPageId pageID)
        {
            if (loadingTaskSet.ContainsKey(pageID) || pagePrefabCache.ContainsKey(pageID))
            {
                Log($"尝试开始ID为{pageID}页面的加载失败,已有相同ID页面正在加载中或页面已加载完毕");
                return;
            }

            var uTask = resLoader.LoadUIObject(pageID);
            var cts = new CancellationTokenSource();
            var prefabLoadTask = new UIPrefabLoadTask(uTask, cts);
            loadingTaskSet.Add(pageID, prefabLoadTask);
            Log($"[UI预载] 开始ID为{pageID}的页面加载");

            var uiPrefab = await prefabLoadTask.UTask;

            //加到缓存
            pagePrefabCache.Add(pageID, uiPrefab);
            Log($"[UI预载] ID为{pageID}的页面加载完成");
            //加载任务完成后移除
            loadingTaskSet.Remove(pageID);
        }

        private void Log(string context)
        {
            if (DebugLogEnable)
            {
                Debug.Log(context);
            }
        }
    }

    /// <summary>页面预载任务,封装了取消任务用的tokenSource</summary>
    public class UIPrefabLoadTask
    {
        public UniTask<GameObject> UTask;
        public CancellationTokenSource Cts;

        public UIPrefabLoadTask(UniTask<GameObject> task, CancellationTokenSource tokenSource)
        {
            UTask = task;
            Cts = tokenSource;
            UTask = UTask.AttachExternalCancellation(tokenSource.Token);
        }
    }
}