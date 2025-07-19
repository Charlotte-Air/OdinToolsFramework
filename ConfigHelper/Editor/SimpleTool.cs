#if UNITY_EDITOR

namespace Framework.ConfigHelper.Editor
{
    /// <summary>
    /// 快速创建简单的工具页面
    /// 会在编辑器的配置窗口中生成条目（项目->配置）
    /// 通过 CategoryAttribute 可以指定条目的分类
    /// </summary>
    public class SimpleTool<T> where T : new()
    {
        private static T _instance;
        
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new T();
                }
                return _instance;
            }
        }
    }
}
#endif