using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Framework.AssetBundleHelper;

namespace Framework.Utility.WeakReference
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AssetTypeAttribute : Attribute
    {
        public Type assetType;

        public AssetTypeAttribute(Type t)
        {
            assetType = t;
        }
    }
    
    [Serializable, HideReferenceObjectPicker]
    public struct AssetPathRef
    {
        [SerializeField] private int _val0;
        [SerializeField] private int _val1;
        [SerializeField] private int _val2;
        [SerializeField] private int _val3;
        
        [SerializeField] private long _fileId;
        
        public string AssetGuidStr => GetAssetGuid().ToString("N");
        
#if UNITY_EDITOR
        public string MainAssetPath => UnityEditor.AssetDatabase.GUIDToAssetPath(AssetGuidStr);
        [SerializeField] private bool _cachedGuidDirty;
        [SerializeField] private bool _cachedEditorAssetDirty;
#endif
        
        public AssetPathRef(int val0, int val1, int val2, int val3, long fileId)
        {
            _val0 = val0;
            _val1 = val1;
            _val2 = val2;
            _val3 = val3;
            _fileId = fileId;
            _cachedAssetGuid = null;
#if UNITY_EDITOR
            _cachedEditorAsset = null;
            _cachedGuidDirty = true;
            _cachedEditorAssetDirty = true;
#endif
        }
        
        public AssetPathRef(Guid assetGuid, long fileID)
        {
            var gb = assetGuid.ToByteArray();
            _val0 = BitConverter.ToInt32(gb, 0);
            _val1 = BitConverter.ToInt32(gb, 4);
            _val2 = BitConverter.ToInt32(gb, 8);
            _val3 = BitConverter.ToInt32(gb, 12);
            _cachedAssetGuid = assetGuid;
            
            _fileId = fileID;
#if UNITY_EDITOR
            _cachedEditorAsset = null;
            _cachedGuidDirty = true;
            _cachedEditorAssetDirty = true;
#endif
        }
        
        public AssetPathRef(string assetGuidStr, long fileID)
        {
            var guid = new Guid(assetGuidStr);
            var gb = guid.ToByteArray();
            _val0 = BitConverter.ToInt32(gb, 0);
            _val1 = BitConverter.ToInt32(gb, 4);
            _val2 = BitConverter.ToInt32(gb, 8);
            _val3 = BitConverter.ToInt32(gb, 12);
            _cachedAssetGuid = guid;
            
            _fileId = fileID;
#if UNITY_EDITOR
            _cachedEditorAsset = null;
            _cachedGuidDirty = true;
            _cachedEditorAssetDirty = true;
#endif
        }

#if UNITY_EDITOR
        public AssetPathRef(UnityEngine.Object asset)
        {
            UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guidStr, out long fileId);
            
            var guid = new Guid(guidStr);
            var gb = guid.ToByteArray();
            _val0 = BitConverter.ToInt32(gb, 0);
            _val1 = BitConverter.ToInt32(gb, 4);
            _val2 = BitConverter.ToInt32(gb, 8);
            _val3 = BitConverter.ToInt32(gb, 12);
            _cachedAssetGuid = guid;
            
            _fileId = fileId;
            _cachedEditorAsset = null;
            _cachedGuidDirty = true;
            _cachedEditorAssetDirty = true;
        }
#endif

        public static bool operator==(AssetPathRef a, AssetPathRef b)
        {
            return a._val0 == b._val0 && a._val1 == b._val1 && a._val2 == b._val2 && a._val3 == b._val3 && a._fileId == b._fileId;
        }
        
        public static bool operator!=(AssetPathRef a, AssetPathRef b)
        {
            return !(a == b);
        }
        
        public override int GetHashCode()
        {
            return _val0.GetHashCode() ^ _val1.GetHashCode() ^ _val2.GetHashCode() ^ _val3.GetHashCode() ^ _fileId.GetHashCode();
        }
        
        public bool IsValid()
        {
            if(_val0 == 0 || _val1 == 0 || _val2 == 0 || _val3 == 0)
                return false;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if(EditorLoad<UnityEngine.Object>() != null)
                    return true;
            }
#endif
            return GameAssetHelper.IsAssetExistInManifest(GetAssetGuid(), _fileId);
        }
        
        public void GetValue(out Guid assetGuid, out long fileId)
        {
            assetGuid = GetAssetGuid();
            fileId = _fileId;
        }
        
        public void GetValue(out string assetGuidStr, out long fileId)
        {
            assetGuidStr = AssetGuidStr;
            fileId = _fileId;
        }
        
        public void GetValue(out int val0, out int val1, out int val2, out int val3, out long fileId)
        {
            val0 = _val0;
            val1 = _val1;
            val2 = _val2;
            val3 = _val3;
            fileId = _fileId;
        }
        
        public void SetValue(Guid assetGuid, long fileId)
        {
            SetAssetGuid(assetGuid);
            _fileId = fileId;
#if UNITY_EDITOR
            _cachedEditorAsset = null;
#endif
        }

        public void SetValue(string assetGuidStr, long fileId)
        {
            SetAssetGuid(assetGuidStr);
            _fileId = fileId;
#if UNITY_EDITOR
            _cachedEditorAsset = null;
#endif
        }

        public void SetValue(UnityEngine.Object asset)
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guidStr, out long fileId);
            SetAssetGuid(guidStr);
            _fileId = fileId;
#endif
#if UNITY_EDITOR
            _cachedEditorAsset = null;
#endif
        }
        
        public void SetValue(int val0, int val1, int val2, int val3, long fileId)
        {
            SetAssetGuid(val0, val1, val2, val3);
            _fileId = fileId;
        }

        /// <summary> 获取游戏资产对象，要确保对应的游戏资产包已经加载了才能获取到 </summary>
        public T FindGameAsset<T>() where T : UnityEngine.Object
        {
            return GameAssetHelper.FindAsset<T>(GetAssetGuid(), _fileId);
        }
        
#if UNITY_EDITOR
        public T EditorLoad<T>() where T : UnityEngine.Object
        {
            var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(AssetGuidStr);
            if (string.IsNullOrEmpty(assetPath))
                return null;
                
            var mainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (mainAsset == null)
                return null;
            UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mainAsset, out var guid, out long fileId);
            if (fileId == _fileId)
                return mainAsset as T;
                
            if (assetPath.EndsWith(".unity") || assetPath.EndsWith(".prefab"))
                return null;
                
            var allAsset = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var asset in allAsset)
            {
                if (asset == mainAsset)
                    continue;
                UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid2, out long fileId2);
                if (fileId2 == _fileId)
                    return asset as T;
            }
            
            return null;
        }
        
        private UnityEngine.Object _cachedEditorAsset;
        public UnityEngine.Object GetEditorAsset()
        {
            if (_cachedEditorAsset == null || _cachedEditorAssetDirty)
                _cachedEditorAsset = EditorLoad<UnityEngine.Object>();
            _cachedEditorAssetDirty = false;
            return _cachedEditorAsset;
        }
#endif
        private static byte[] _gb = new byte[16];
        private Guid? _cachedAssetGuid;
        private Guid GetAssetGuid()
        {
#if UNITY_EDITOR
            if (_cachedAssetGuid.HasValue && !_cachedGuidDirty)
                return _cachedAssetGuid.Value;
#else
            if (_cachedAssetGuid.HasValue)
                return _cachedAssetGuid.Value;
#endif
            
#if UNITY_EDITOR
            _cachedGuidDirty = false;
#endif
            byte[] buf;
            buf = BitConverter.GetBytes(_val0);
            Array.Copy(buf, 0, _gb, 0, 4);
            buf = BitConverter.GetBytes(_val1);
            Array.Copy(buf, 0, _gb, 4, 4);
            buf = BitConverter.GetBytes(_val2);
            Array.Copy(buf, 0, _gb, 8, 4);
            buf = BitConverter.GetBytes(_val3);
            Array.Copy(buf, 0, _gb, 12, 4);

            _cachedAssetGuid = new Guid(_gb);
            return _cachedAssetGuid.Value;
        }
        
        private void SetAssetGuid(int val0, int val1, int val2, int val3)
        {
            _val0 = val0;
            _val1 = val1;
            _val2 = val2;
            _val3 = val3;
            _cachedAssetGuid = null;
        }
        
        private void SetAssetGuid(Guid guid)
        {
            var gb = guid.ToByteArray();
            _val0 = BitConverter.ToInt32(gb, 0);
            _val1 = BitConverter.ToInt32(gb, 4);
            _val2 = BitConverter.ToInt32(gb, 8);
            _val3 = BitConverter.ToInt32(gb, 12);
            _cachedAssetGuid = guid;
        }
        
        private void SetAssetGuid(string guid)
        {
            SetAssetGuid(new Guid(guid));
        }
    }
}