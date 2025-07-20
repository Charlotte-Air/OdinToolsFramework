using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

namespace LightWeightUI
{
    /// <summary>UI对象的动画控制接口，分发具体动画实现到更底层</summary>
    public interface IUIAnimController
    {
        /// <summary>初始化步骤，初始化时会把要控制的对象传进来，自己从上面找需要的组件</summary>
        /// <param name="targetUI">要控制的UI对象</param>
        void Init(UIObject targetUI);
        
        /// <summary>播放UI对象打开动画的Task</summary>
        /// <returns>UniTask对象，返回UniTask供上层进行异步操作</returns>
        UniTask PlayOpen(CancellationToken cancelToken);
        
        /// <summary>播放UI对象关闭动画的Task</summary>
        /// <returns>UniTask对象，返回UniTask供上层进行异步操作</returns>
        UniTask PlayClose(CancellationToken cancelToken);
    }

    /// <summary>动画控制方案枚举</summary>
    public enum UIAnimControllType
    {
        [LabelText("无")] None,
        
        Animator,
        
        AnimationSequencer,

        Timeline,
        SimpleAnimation,
    }
}