// From: https://docs.unity3d.com/Packages/com.unity.shadergraph@17.6/manual/Custom-Function-Node.html#multiple-functions-and-multiple-files
//UNITY_SHADER_NO_UPGRADE
#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED
#include "../lygia/generative/voronoi.hlsl"
void voronoi_3d_float(float3 Position, out float3 Noise ){
    Noise = voronoi(Position);
}
#endif //MYHLSLINCLUDE_INCLUDED