﻿using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("LiteRP.Editor")]

namespace LiteRP.Editor
{
    internal static class MaterialAccess
    {
        internal static int ReadMaterialRawRenderQueue(Material material)
        {
            return material.rawRenderQueue;
        }
    }
}