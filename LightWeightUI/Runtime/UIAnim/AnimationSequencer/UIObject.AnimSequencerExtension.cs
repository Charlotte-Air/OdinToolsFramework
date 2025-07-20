using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;
using BrunoMikoski.AnimationSequencer;

namespace LightWeightUI
{
    public partial class UIObject
    {
        [SerializeField, TitleGroup("通用动画/打开"),
         ShowIf("OpenAnimType", UIAnimControllType.AnimationSequencer),
         LabelText("Open Sequence")]
        public AnimationSequencerController InSequence;

        [SerializeField, TitleGroup("通用动画/关闭"),
         ShowIf("CloseAnimType", UIAnimControllType.AnimationSequencer),
         LabelText("Close Sequence")]
        public AnimationSequencerController OutSequence;
    }
}