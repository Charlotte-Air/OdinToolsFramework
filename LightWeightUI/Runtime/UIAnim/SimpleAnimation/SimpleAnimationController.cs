using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using Framework.Utility.SimpleAnimation;

namespace LightWeightUI
{
    /// <summary>Unity默认的Animator动画状态机控制方案</summary>
    /// 每个UI对象支持一个Animator，会从UI对象自己身上找Animator组件
    public class SimpleAnimationController : IUIAnimController
    {
        /// <summary>要控制的SimpleAnimation</summary>
        private SimpleAnimation _simpleAnimation;
        
        /// <summary>UI对象打开动画的TaskSource用于回调转Task</summary>
        private UniTaskCompletionSource openAnimFlow;

        /// <summary>UI对象关闭动画的TaskSource用于回调转Task</summary>
        private UniTaskCompletionSource closeAnimFlow;

        private string _openAnimName;
        private string _closeAnimName;


        /// <summary>初始化步骤，初始化时会把要控制的UI对象传进来，自己从上面找需要的组件</summary>
        /// <param name="targetUI">要控制的UI对象</param>
        public void Init(UIObject targetUI)
        {
            _simpleAnimation = targetUI.GetComponent<SimpleAnimation>();
            _openAnimName = targetUI.OpenAnimName;
            _closeAnimName = targetUI.CloseAnimName;
        }

        /// <summary>播放UI对象打开动画的Task</summary>
        /// <returns>UniTask对象，返回UniTask供上层进行异步操作</returns>
        public UniTask PlayOpen(CancellationToken cancelToken)
        {
            if (!_simpleAnimation)
            {
                return UniTask.CompletedTask;
            }
        
            //注册打开UI对象动画放完的监听
            openAnimFlow = new UniTaskCompletionSource();
            _simpleAnimation.OnClipEnd = (animationClipInfo) =>
            {
                openAnimFlow.TrySetResult();
                _simpleAnimation.SampleAnimationLastFrame(_openAnimName);
            }; //直接替换事件
            //注册取消事件
            cancelToken.Register(() =>
            {
                _simpleAnimation.Stop(false);
                _simpleAnimation.SampleAnimationLastFrame(_openAnimName);
                openAnimFlow.TrySetCanceled(cancelToken);
            });
            _simpleAnimation.Play(_openAnimName);

            return openAnimFlow.Task;
        }
        
        /// <summary>播放UI对象关闭动画的Task</summary>
        /// <returns>UniTask对象，返回UniTask供上层进行异步操作</returns>
        public UniTask PlayClose(CancellationToken cancelToken)
        {
            if (!_simpleAnimation)
            {
                return UniTask.CompletedTask;
            }
        
            //注册关闭UI对象动画放完的监听
            closeAnimFlow = new UniTaskCompletionSource();
            _simpleAnimation.OnClipEnd = (animationClipInfo) =>
            {
                closeAnimFlow.TrySetResult();
                _simpleAnimation.SampleAnimationFirstFrame(_closeAnimName);
            }; //直接替换事件
            //注册取消事件
            cancelToken.Register(() =>
            {
                _simpleAnimation.Stop(false);
                _simpleAnimation.SampleAnimationFirstFrame(_closeAnimName);
                closeAnimFlow.TrySetCanceled(cancelToken);
            });
            _simpleAnimation.Play(_closeAnimName);
            
            return closeAnimFlow.Task;
        }
    }
}