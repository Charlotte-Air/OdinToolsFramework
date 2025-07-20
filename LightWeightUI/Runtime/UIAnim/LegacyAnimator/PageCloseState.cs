using System;
using UnityEngine;
using UnityEngine.Animations;

namespace LightWeightUI
{
    /// <summary>页面关闭动画状态的行为扩展，主要用于监听动画播放的事件</summary>
    public class PageCloseState : StateMachineBehaviour
    {
        /// <summary>关闭动画开始播放时的事件</summary>
        public Action OnAnimStart;

        /// <summary>关闭动画结束播放时的事件</summary>
        public Action OnAnimFinish;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex,
            AnimatorControllerPlayable controller)
        {
            base.OnStateExit(animator, stateInfo, layerIndex, controller);
            OnAnimStart?.Invoke();
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateExit(animator, stateInfo, layerIndex);
            OnAnimFinish?.Invoke();
        }
    }
}