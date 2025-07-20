using System;
using UnityEngine;

namespace Framework.Utility.WeakReference
{
    /// <summary> 引用场景中的GameObject，主要用在动态对象需要引用场景静态对象的情况 </summary>
    [Serializable]
    public struct SceneGameObjectWeakRef
    {
#if UNITY_EDITOR
        [HideInInspector]
        public string GlobalObjectId;
#endif
        
        [HideInInspector]
        public string TargetScenePath;

        /// <summary> 通过路径查找对象，性能很低，建议缓存起来 </summary>
        public GameObject FindGameObject()
        {
            return GameObject.Find(TargetScenePath);
        }
    }
}