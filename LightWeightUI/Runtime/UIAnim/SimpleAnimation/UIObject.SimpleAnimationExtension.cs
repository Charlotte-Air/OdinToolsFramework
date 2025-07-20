using System;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Playables;

namespace LightWeightUI
{
    public partial class UIObject
    {
        [SerializeField, TitleGroup("通用动画/打开"),
         ShowIf("OpenAnimType", UIAnimControllType.SimpleAnimation),
         LabelText("Open name")]
        public String OpenAnimName = "OpenPage";

        [SerializeField, TitleGroup("通用动画/关闭"),
         ShowIf("CloseAnimType", UIAnimControllType.SimpleAnimation),
         LabelText("Close name")]
        public String CloseAnimName = "ClosePage";
    }
}