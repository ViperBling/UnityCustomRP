using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace LiteRP
{
    public struct ShadowSliceData
    {
        /// <summary>
        /// The view matrix.
        /// </summary>
        public Matrix4x4 viewMatrix;

        /// <summary>
        /// The projection matrix.
        /// </summary>
        public Matrix4x4 projectionMatrix;

        /// <summary>
        /// The shadow transform matrix.
        /// </summary>
        public Matrix4x4 shadowTransform;

        /// <summary>
        /// The X offset to the shadow map.
        /// </summary>
        public int offsetX;

        /// <summary>
        /// The Y offset to the shadow map.
        /// </summary>
        public int offsetY;

        /// <summary>
        /// The maximum tile resolution in an Atlas.
        /// </summary>
        public int resolution;

        /// <summary>
        /// The shadow split data containing culling information.
        /// </summary>
        public ShadowSplitData splitData;

        /// <summary>
        /// Clears and resets the data.
        /// </summary>
        public void Clear()
        {
            viewMatrix = Matrix4x4.identity;
            projectionMatrix = Matrix4x4.identity;
            shadowTransform = Matrix4x4.identity;
            offsetX = offsetY = 0;
            resolution = 1024;
        }
    }

    public struct LightShadowCullingInfos
    {
        public NativeArray<ShadowSliceData> shadowSlices;
        public uint slicesValidMask;
        
        public readonly bool IsSliceValid(int i) => (slicesValidMask & (1u << i)) != 0; 
    }
}