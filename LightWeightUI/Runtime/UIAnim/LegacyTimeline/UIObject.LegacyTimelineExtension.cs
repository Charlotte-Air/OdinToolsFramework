using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Playables;
using BrunoMikoski.AnimationSequencer;

namespace LightWeightUI
{
    public partial class UIObject
    {
        [SerializeField, TitleGroup("通用动画/打开"),
         ShowIf("OpenAnimType", UIAnimControllType.Timeline),
         LabelText("Open Timeline")]
        public PlayableDirector InTimelineDirector;

        [SerializeField, TitleGroup("通用动画/关闭"),
         ShowIf("CloseAnimType", UIAnimControllType.Timeline),
         LabelText("Close Timeline")]
        public PlayableDirector OutTimelineDirector;
    }
}