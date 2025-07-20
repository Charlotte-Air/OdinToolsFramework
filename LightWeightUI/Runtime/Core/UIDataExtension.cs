using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace LightWeightUI
{
    /// <summary>UI栈结构</summary>
    public class UIStack : IEnumerable<UIPage>
    {
        public List<UIPage> instanceList;

        /// <summary>栈中页面总数，不分层级，计算所有层级栈里的所有页面</summary>
        public int Count => instanceList.Count;

        public UIPage this[int index] => instanceList[index];

        public UIStack()
        {
            instanceList = new();
        }

        /// <summary>入栈</summary>
        /// <param name="instance">要入栈的UI页面</param>
        public void Push(UIPage instance)
        {
            instanceList.Add(instance);
        }

        /// <summary>出栈，弹出指定层级栈顶的页面</summary>
        /// <returns>栈空返回null</returns>
        /// >
        public UIPage Pop()
        {
            if (instanceList.Count <= 0)
            {
                return null;
            }

            var lastOne = instanceList.Last();
            instanceList.Remove(lastOne);
            return lastOne;
        }

        /// <summary>获取栈顶元素实例，不会将元素出栈</summary>
        /// <returns>返回栈顶元素，栈空返回null</returns>
        public UIPage PeekTop()
        {
            if (instanceList.Count <= 0)
            {
                return null;
            }

            return instanceList.Last();
        }

        /// <summary>指定移除某个UI页面</summary>
        /// <returns>移除成功返回true,失败false</returns>
        public bool Remove(UIPage uiPage)
        {
            return instanceList.Remove(uiPage);
        }


        /// <summary>清空所有层级栈中所有记录</summary>
        public void Clear()
        {
            instanceList.Clear();
        }

        /// <summary>检查给定页面是否栈顶元素</summary>
        /// <returns>位于栈顶返回true，不位于栈顶或栈为空返回false</returns>
        public bool IsTop(UIPage uiPage)
        {
            if (instanceList.Count <= 0)
            {
                return false;
            }

            return uiPage.GetInstanceID() == instanceList.Last().GetInstanceID();
        }

        /// <summary>获取物体下标位置</summary>
        /// <returns>返回其在线性表中的下标</returns>
        public int IndexOf(UIPage uiPage)
        {
            return instanceList.IndexOf(uiPage);
        }

        public IEnumerator<UIPage> GetEnumerator()
        {
            return instanceList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}