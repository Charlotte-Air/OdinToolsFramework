using System;
using UnityEngine;
using System.Collections.Generic;

namespace Framework.Utility
{
    public static class MessageSystem
    {
        public static Dictionary<Type, Delegate> Handlers = new Dictionary<Type, Delegate>();
        public static List<Action> _pushActions = new List<Action>();
        
        public static void Register<T>(Action<T> handler) where T : struct
        {
            if (Handlers.ContainsKey(typeof(T)))
            {
                Handlers[typeof(T)] = (Action<T>) Handlers[typeof(T)] + handler;
            }
            else
            {
                Handlers.Add(typeof(T), handler);
            }
        }
        
        public static void UnRegister<T>(Action<T> handler) where T : struct
        {
            if (Handlers.ContainsKey(typeof(T)))
            {
                Handlers[typeof(T)] = (Action<T>) Handlers[typeof(T)] - handler;
            }
        }
        
        public static void Send<T>(T message) where T : struct
        {
            if (Handlers.ContainsKey(typeof(T)))
            {
                ((Action<T>) Handlers[typeof(T)])?.Invoke(message);
            }
        }
        public static void Clear()
        {
            Handlers.Clear();
        }
        
        public static void Clear<T>() where T : struct
        {
            if (Handlers.ContainsKey(typeof(T)))
            {
                Handlers.Remove(typeof(T));
            }
        }
        
        public static void PushAllMergeMessage()
        {
            for (var i = 0; i < _pushActions.Count; i++)
            {
                _pushActions[i]?.Invoke();
            }
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            Clear();
        }
    }

    /// <summary> 在一帧的最后发出事件，并且去除相同的事件 </summary>
    public static class MergeMessageSystem<T> where T : struct
    {
        private static List<T> _messages = new List<T>();
        private static bool _isAddPushAction = false;
        
        public static void Send(T newMessage)
        {
            // 有相等的就不添加
            foreach (var message in _messages)
            {
                if (message.Equals(newMessage))
                {
                    return;
                }
            }

            _messages.Add(newMessage);
            if (!_isAddPushAction)
            {
                _isAddPushAction = true;
                MessageSystem._pushActions.Add(Push);
            }
        }
        
        private static void Push()
        {
            for (var i = 0; i < _messages.Count; i++)
            {
                MessageSystem.Send(_messages[i]);
            }
            
            _messages.Clear();
        }
    }
}