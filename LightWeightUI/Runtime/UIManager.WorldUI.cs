using Lean.Pool;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Framework.Utility;

namespace LightWeightUI
{
    /// <summary>世界坐标UI相关</summary>
    public partial class UIManager
    {
        private Canvas WorldUICanvas;

        private Dictionary<WorldUIId, GameObject> prefabCache;

        private Dictionary<WorldUIId, LeanGameObjectPool> worldUIPools;

        private void InitWorldUIPart()
        {
            prefabCache = new();
            worldUIPools = new();
        }

        public WorldUIElement OpenWorldUI(WorldUIId id, Transform showTransform, bool needAnim = true, bool needFollowTransform = true, Vector3 offset = default)
        {
            if (!worldUIPools.TryGetValue(id, out var pool))
            {
                if (!prefabCache.TryGetValue(id, out var uiPrefab))
                {
                    uiPrefab = resLoader.LoadUIObjectSync(id);
                    prefabCache.Add(id, uiPrefab);
                }

                var poolObj = new GameObject();
                poolObj.name = $"WorldUIPool({uiPrefab.name})";
                pool = poolObj.AddComponent<LeanGameObjectPool>();
                pool.Prefab = uiPrefab;
                worldUIPools.Add(id, pool);
            }

            var newObj = pool.Spawn(WorldUICanvas.transform);
            var worldUIEle = newObj.GetComponent<WorldUIElement>();
            //生命周期 OnLoad
            if(needFollowTransform)
                worldUIEle.Init(id, showTransform, offset);
            else
            {
                worldUIEle.Init(id, null, offset);
                worldUIEle.transform.position = showTransform.position;
            }
            worldUIEle.OnOpenBefore();

            WaitForAnim(worldUIEle.GetCancellationTokenOnDestroy(), worldUIEle, needAnim).Forget();
            return worldUIEle;
        }


        private async UniTaskVoid WaitForAnim(CancellationToken cancellationToken, WorldUIElement worldUIOpened, bool needAnim)
        {
            //等UI处理完自己负责的入场动画播放
            if (needAnim)
            {
                await worldUIOpened.OpenAnim(new CancellationToken());
            }

            //生命周期 OnOpenAfter
            worldUIOpened.OnOpenAfter();
        }

        public void CloseWorldUI(WorldUIElement whichOne, bool needAnim = true)
        {
            if (!worldUIPools.ContainsKey(whichOne.ID))
            {
                return;
            }

            InternalCloseWorldUIFlow(whichOne.GetCancellationTokenOnDestroy(), whichOne, needAnim).Forget();
        }

        private async UniTaskVoid InternalCloseWorldUIFlow(CancellationToken cancellationToken, WorldUIElement worldUI, bool needAnim)
        {
            //生命周期 OnCloseBefore
            worldUI.OnCloseBefore();
            //等Page处理完自己的关闭动画流程
            if (needAnim)
            {
                await worldUI.CloseAnim(new CancellationToken());
            }

            //生命周期 OnCloseAfter
            worldUI.OnCloseAfter();

            if(GameUtility.IsApplicationQuit)
                return;
            worldUIPools[worldUI.ID].Despawn(worldUI.gameObject);
        }
    }
}