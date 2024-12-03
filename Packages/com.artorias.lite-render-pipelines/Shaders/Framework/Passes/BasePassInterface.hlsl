#pragma once

#include "Packages/com.artorias.lite-render-pipelines/ShadersLibrary/SurfaceData.hlsl"
#include "Packages/com.artorias.lite-render-pipelines/Shaders/Framework/Passes/ForwardPassInput.hlsl"

void virtual_Vertex(FVertexInputs input, float3 positionWS, inout FFragmentInputs vsOut);

void virtual_InitializeSurfaceData(FFragmentInputs input, out FSurfaceData surfaceData);