using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Framework.ConfigHelper;
using System.Collections.Generic;

namespace WindowPage
{
    [ConfigMenu(category = "不常用/UI配置")]
    [CreateAssetMenu(fileName = "UIConfig", menuName = "LightWeightUI/UIPriorityConfig")]
    public class UIConfig : GameConfig<UIConfig>
    {
        [TabGroup("UIConfig", "层级设置", Icon = SdfIconType.LayersFill, Order = -1000),
         LabelText("层级列表（排序Sorting值越大代表越显示在上面）"),
         ListDrawerSettings(ShowPaging = false, ListElementLabelName = "LayerName")]
        public List<UILayerConfig> LayerSetting;

        [TabGroup("UIConfig", "层级设置", Order = -1000), Button("排序", Icon = SdfIconType.HeartFill), PropertyOrder(-1000)]
        private void ResortLayer()
        {
            LayerSetting = LayerSetting.OrderByDescending((layer) => { return layer.SortingOrder; }).ToList();
        }

        [TabGroup("UIConfig", "页面预加载", Icon = SdfIconType.BoxArrowInDown, Order = 1000)] [HideLabel] [DisplayAsString]
        public string Hint = "没有配置的页面默认为低优先级页面，且权重取最低";

        [TabGroup("UIConfig", "页面预加载", Order = 1000)]
        [LabelText("高优先级页面", SdfIconType.CircleFill)]
        [DictionaryDrawerSettings(KeyLabel = "页面ID", ValueLabel = "权重值（越大越优先）")]
        public Dictionary<UIPageId, int> HighPriorityPage;

        [TabGroup("UIConfig", "页面预加载", Order = 1000)]
        [LabelText("低优先级页面", SdfIconType.Circle)]
        [DictionaryDrawerSettings(KeyLabel = "页面ID", ValueLabel = "权重值（越大越优先）")]
        public Dictionary<UIPageId, int> LowPriorityPage;
    }
}