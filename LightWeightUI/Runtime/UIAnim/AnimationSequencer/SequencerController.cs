using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using BrunoMikoski.AnimationSequencer;

namespace LightWeightUI
{
    /// <summary>AnimationSequencer插件的动画控制方案</summary>
    public class SequencerController : IUIAnimController
    {
        private AnimationSequencerController OpenSequence;

        private AnimationSequencerController CloseSequence;

        /// <summary>UI对象打开动画的TaskSource用于回调转Task</summary>
        private UniTaskCompletionSource openAnimFlow;

        /// <summary>UI对象关闭动画的TaskSource用于回调转Task</summary>
        private UniTaskCompletionSource closeAnimFlow;

        public void Init(UIObject targetUI)
        {
            OpenSequence = targetUI.InSequence;
            CloseSequence = targetUI.OutSequence;

            if (OpenSequence)
            {
                OpenSequence.SetAutoKill(false);
                OpenSequence.SetAutoplayMode(AnimationSequencerController.AutoplayType.Nothing);
                if (OpenSequence.IsPlaying)
                {
                    OpenSequence.Pause();
                    OpenSequence.ResetToInitialState();
                    OpenSequence.Kill();
                }
            }

            if (CloseSequence)
            {
                OpenSequence.SetAutoKill(false);
                CloseSequence.SetAutoplayMode(AnimationSequencerController.AutoplayType.Nothing);
                if (CloseSequence.IsPlaying)
                {
                    CloseSequence.Pause();
                    CloseSequence.ResetToInitialState();
                    OpenSequence.Kill();
                }
            }
        }
        
        public UniTask PlayOpen(CancellationToken cancelToken)
        {
            if (!OpenSequence)
            {
                return UniTask.CompletedTask;
            }
        
            //注册打开UI对象动画放完的监听
            openAnimFlow = new UniTaskCompletionSource();
            //注册取消事件
            cancelToken.Register(() =>
            {
                OpenSequence.Rewind();
                openAnimFlow.TrySetCanceled(cancelToken);
            });
            OpenSequence.PlayForward(true, OnOpenAnimDone);
        
            return openAnimFlow.Task;
        }
        
        private void OnOpenAnimDone()
        {
            openAnimFlow.TrySetResult();
            OpenSequence.Complete(false);
        }
        
        public UniTask PlayClose(CancellationToken cancelToken)
        {
            if (!CloseSequence)
            {
                return UniTask.CompletedTask;
            }
        
            //注册打开UI对象动画放完的监听
            closeAnimFlow = new UniTaskCompletionSource();
            //注册取消事件
            cancelToken.Register(() =>
            {
                CloseSequence.Rewind();
                closeAnimFlow.TrySetCanceled(cancelToken);
            });
        
            CloseSequence.PlayForward(true, OnCloseAnimDone);
        
            return closeAnimFlow.Task;
        }
        
        private void OnCloseAnimDone()
        {
            closeAnimFlow.TrySetResult();
            CloseSequence.Rewind();
        }
    }
}