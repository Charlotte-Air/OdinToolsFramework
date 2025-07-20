using System;
using System.Collections;
using System.Collections.Generic;

namespace LightWeightUI.Preload
{
    public interface IUIPreloadTool
    {
        IEnumerator PreloadPageHighPriority();

        void PreloadPageLowPriority();

        bool TryGetPage(UIPageId id, out UIPage page);

        UIPage HandlePreloadNotFinish(UIPageId id);
    }
}