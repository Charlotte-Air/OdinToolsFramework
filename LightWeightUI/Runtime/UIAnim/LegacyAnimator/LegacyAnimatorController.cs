using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LightWeightUI
{
    /// <summary>Unity默认的Animator动画状态机控制方案</summary>
    /// 每个UI对象支持一个Animator，会从UI对象自己身上找Animator组件
    public class LegacyAnimatorController : IUIAnimController
    {
        /// <summary>要控制的Animator</summary>
        private Animator selfAnimator;

        /// <summary>UI对象打开动画状态行为</summary>
        private PageOpenState openState;

        /// <summary>UI对象关闭动画状态行为</summary>
        private PageCloseState closeState;

        /// <summary>UI对象打开动画的TaskSource用于回调转Task</summary>
        private UniTaskCompletionSource openAnimFlow;

        /// <summary>UI对象关闭动画的TaskSource用于回调转Task</summary>
        private UniTaskCompletionSource closeAnimFlow;

        private static readonly int AnimParamOpen = Animator.StringToHash("Open");

        private static readonly int AnimParamClose = Animator.StringToHash("Close");


        /// <summary>初始化步骤，初始化时会把要控制的UI对象传进来，自己从上面找需要的组件</summary>
        /// <param name="targetUI">要控制的UI对象</param>
        public void Init(UIObject targetUI)
        {
            selfAnimator = targetUI.GetComponent<Animator>();
            if (null != selfAnimator)
            {
                openState = selfAnimator.GetBehaviour<PageOpenState>();
                closeState = selfAnimator.GetBehaviour<PageCloseState>();   
            }
        }

        /// <summary>尝试获取stateMachineBehaviour</summary>
        /// <returns>需要的变量是否都获取成功</returns>
        /// 因Unity的特性，每次SetActive时，StateMachineBehaviour都会创建新的实例，所以没办法只能每次使用时获取
        private bool GetStateBehaviour()
        {
            if (null == selfAnimator)
            {
                return false;
            }

            openState = selfAnimator.GetBehaviour<PageOpenState>();
            closeState = selfAnimator.GetBehaviour<PageCloseState>();
            return !(null == openState ||
                     null == closeState);
        }

        /// <summary>播放UI对象打开动画的Task</summary>
        /// <returns>UniTask对象，返回UniTask供上层进行异步操作</returns>
        public UniTask PlayOpen(CancellationToken cancelToken)
        {
            if (!GetStateBehaviour())
            {
                return UniTask.CompletedTask;
            }
        
            //注册打开UI对象动画放完的监听
            openAnimFlow = new UniTaskCompletionSource();
            openState.OnAnimFinish = () => { openAnimFlow.TrySetResult(); }; //直接替换事件
            //注册取消事件
            cancelToken.Register(() =>
            {
                selfAnimator.ResetTrigger(AnimParamOpen); //重要!!一定要手动重置一次已经被启用的trigger,不然animator动画会有闪烁等问题
                openAnimFlow.TrySetCanceled(cancelToken);
            });
            selfAnimator.SetTrigger(AnimParamOpen);
            
            return openAnimFlow.Task;
        }
        
        /// <summary>播放UI对象关闭动画的Task</summary>
        /// <returns>UniTask对象，返回UniTask供上层进行异步操作</returns>
        public UniTask PlayClose(CancellationToken cancelToken)
        {
            if (!GetStateBehaviour())
            {
                return UniTask.CompletedTask;
            }
        
            //注册关闭UI对象动画放完的监听
            closeAnimFlow = new UniTaskCompletionSource();
            closeState.OnAnimFinish = () => { closeAnimFlow.TrySetResult(); }; //直接替换事件
            //注册取消事件
            cancelToken.Register(() =>
            {
                selfAnimator.ResetTrigger(AnimParamClose);
                closeAnimFlow.TrySetCanceled(cancelToken);
            });
            
            selfAnimator.SetTrigger(AnimParamClose);
            return closeAnimFlow.Task;
        }
    }
}