using System;
using UnityEngine;
using System.Threading;
using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;

namespace LightWeightUI
{
    public abstract class WorldUIElement : UIObject
    {
        public WorldUIId ID { get; private set; }

        protected Transform transFollowTarget;
        protected Vector3 offset;

        public void Init(WorldUIId id, Transform targetPoint, Vector3 inOffset)
        {
            ID = id;
            transFollowTarget = targetPoint;
            offset = inOffset;
            UpdatePosition();
        }

        protected virtual void Update()
        {
            UpdatePosition();
        }

        protected virtual void UpdatePosition()
        {
            if (!transFollowTarget) return;
            transform.position = transFollowTarget.position + offset;
        }
    }
}