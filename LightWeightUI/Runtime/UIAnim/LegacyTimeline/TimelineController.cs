using System;
using System.Threading;
using UnityEngine.Playables;
using Cysharp.Threading.Tasks;

namespace LightWeightUI
{
    /// <summary>Unity自带Timeline的动画控制方案</summary>
    public class TimelineController : IUIAnimController
    {
        private PlayableDirector inDirector;

        private PlayableDirector outDirector;

        public void Init(UIObject targetUI)
        {
            inDirector = targetUI.InTimelineDirector;
            outDirector = targetUI.OutTimelineDirector;

            InitializeDirector(inDirector);
            InitializeDirector(outDirector);
        }

        private void InitializeDirector(PlayableDirector playableDirector)
        {
            if (playableDirector)
            {
                playableDirector.playOnAwake = false;
                playableDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            }
        }

        public UniTask PlayOpen(CancellationToken cancelToken)
        {
            if (!inDirector || inDirector.playableAsset == null)
            {
                return UniTask.CompletedTask;
            }

            //注册取消事件
            cancelToken.Register(() => { RevertTimelineChanges(inDirector, false); });
            inDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            inDirector.Play();

            return UniTask.Delay(TimeSpan.FromSeconds(inDirector.duration), DelayType.UnscaledDeltaTime,
                cancellationToken: cancelToken).ContinueWith(() => { RevertTimelineChanges(inDirector, false); });
        }

        public UniTask PlayClose(CancellationToken cancelToken)
        {
            if (!outDirector || outDirector.playableAsset == null)
            {
                return UniTask.CompletedTask;
            }

            //注册取消事件
            cancelToken.Register(() => { RevertTimelineChanges(outDirector, true); });
            outDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            outDirector.Play();

            return UniTask.Delay(TimeSpan.FromSeconds(outDirector.duration), DelayType.UnscaledDeltaTime,
                cancellationToken: cancelToken).ContinueWith(() => { RevertTimelineChanges(outDirector, true); });
        }

        /// <summary>回滚Timeline动画对操控物体的改动，目前靠直接设置到第一帧或最后一帧实现</summary>
        /// <param name="firstOrLast">true重制到第一帧，false到最后一帧</param>
        private void RevertTimelineChanges(PlayableDirector playableDirector, bool firstOrLast)
        {
            //手动跳帧
            playableDirector.timeUpdateMode = DirectorUpdateMode.Manual;
            playableDirector.time = firstOrLast ? 0 : playableDirector.duration;
            playableDirector.Evaluate();
            //处理完后必须将当前playableDirector Stop，否则其中Graph一直存在会一直复写其中帧数据，干扰后续动画
            playableDirector.Stop();
        }
    }
}