using System;
using UnityEngine;

namespace Framework.Utility.WeakReference
{
    /// <summary> 相对路径引用，主要用来解决一个由多个预制体组成的大对象中，子预制体对象之间的引用 </summary>
    [Serializable]
    public struct GameObjectRelativePath
    {
        public string RelativePath;
        
        /// <summary> 通过路径查找对象，性能很低，建议缓存起来 </summary>
        public GameObject FindGameObject(GameObject root)
        {
            var pathArray = RelativePath.Split('\\');
            var current = root.transform;
            foreach (var path in pathArray)
            {
                if (path == "..")
                {
                    current = current.parent;
                    continue;
                }
                
                current = current.Find(path);
                if (current == null)
                    return null;
            }
            return current.gameObject;
        }
    }
}