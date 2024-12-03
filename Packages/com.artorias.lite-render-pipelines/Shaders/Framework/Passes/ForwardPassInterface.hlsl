#pragma once

#include "BasePassInterface.hlsl"
#include "Packages/com.artorias.lite-render-pipelines/ShadersLibrary/Core.hlsl"

void virtual_InitializeFragmentData(FFragmentInputs input, out FFragmentData fragmentData);
void virtual_InitializeSurfaceData(inout FFragmentData fragmentData);
void virtual_InitializeCommonData(inout FFragmentData fragmentData);