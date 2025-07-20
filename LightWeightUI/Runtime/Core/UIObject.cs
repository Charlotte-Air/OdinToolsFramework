using System;
using UnityEngine;
using System.Threading;
using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;

namespace LightWeightUI
{
    // [DisallowMultipleComponent]
    public abstract partial class UIObject : MonoBehaviour
    {
        [FoldoutGroup("通用动画"), TitleGroup("通用动画/打开"), LabelText("Open动画控制方式", SdfIconType.BoxArrowRight),
         SerializeField,
         PropertyOrder(-1)]
        private UIAnimControllType OpenAnimType;

        [FoldoutGroup("通用动画"), TitleGroup("通用动画/关闭"), LabelText("Close动画控制方式", SdfIconType.BoxArrowLeft),
         SerializeField]
        private UIAnimControllType CloseAnimType;

        /// <summary>打开时使用的动画控制器</summary>
        private IUIAnimController inAnimController;

        /// <summary>关闭时使用的动画控制器</summary>
        private IUIAnimController outAnimController;
        
        /// <summary>初始化动画控制方案</summary>
        private void InitAnimController()
        {
            switch (OpenAnimType)
            {
                case UIAnimControllType.Animator:
                    inAnimController = new LegacyAnimatorController();
                    break;
                case UIAnimControllType.AnimationSequencer:
                    inAnimController = new SequencerController();
                    break;
                case UIAnimControllType.Timeline:
                    inAnimController = new TimelineController();
                    break;
                case UIAnimControllType.SimpleAnimation:
                    inAnimController = new SimpleAnimationController();
                    break;
                default:
                    inAnimController = null;
                    break;
            }

            switch (CloseAnimType)
            {
                case UIAnimControllType.Animator:
                    outAnimController = new LegacyAnimatorController();
                    break;
                case UIAnimControllType.AnimationSequencer:
                    outAnimController = new SequencerController();
                    break;
                case UIAnimControllType.Timeline:
                    outAnimController = new TimelineController();
                    break;
                case UIAnimControllType.SimpleAnimation:
                    outAnimController = new SimpleAnimationController();
                    break;
                default:
                    outAnimController = null;
                    break;
            }

            inAnimController?.Init(this);
            outAnimController?.Init(this);
        }

        protected virtual void Awake()
        {
            InitAnimController();
        }

        /// <summary>UI对象生命周期方法，在UI对象首次每次被打开（显示出来）的时候调用一次，此方法调用在动画播放之前</summary>
        public virtual void OnOpenBefore() { }

        /// <summary>UI对象生命周期方法，在UI对象首次每次被打开（显示出来）的时候调用一次，此方法调用在动画播放之后</summary>
        public virtual void OnOpenAfter() { }

        /// <summary>UI对象生命周期方法，在UI对象每次被关闭（隐藏）的时候调用一次，此方法调用在动画播放之前</summary>
        public virtual void OnCloseBefore() { }

        /// <summary>UI对象生命周期方法，在UI对象每次被关闭（隐藏）的时候调用一次，此方法调用在动画播放之后</summary>
        public virtual void OnCloseAfter() { }

        /// <summary>UI对象打开动画的流程，异步</summary>
        /// <returns>返回等待动画播完的UniTask</returns>
        public virtual UniTask OpenAnim(CancellationToken cancelToken)
        {
            if (null == inAnimController)
            {
                return UniTask.CompletedTask;
            }

            return inAnimController.PlayOpen(cancelToken);
        }

        /// <summary>UI对象关闭动画的流程，异步</summary>
        /// <returns>返回等待动画播完的UniTask</returns>
        public virtual UniTask CloseAnim(CancellationToken cancelToken)
        {
            if (null == outAnimController)
            {
                return UniTask.CompletedTask;
            }

            return outAnimController.PlayClose(cancelToken);
        }
    }
}