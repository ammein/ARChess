// From: https://docs.unity3d.com/Packages/com.unity.shadergraph@17.6/manual/Custom-Function-Node.html#multiple-functions-and-multiple-files
//UNITY_SHADER_NO_UPGRADE
#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED
#define FBM_NOISE_FNC(POS_UV) abs(snoise(POS_UV))
#define FBM_OCTAVES 2
#define FBM_SCALE_SCALAR 3.0
#include "../lygia/generative/fbm.hlsl"
void simplex_noise_turbulence_float(float3 v, float noise_scale, out float Noise ){
    Noise = fbm(v * noise_scale);
}
#endif //MYHLSLINCLUDE_INCLUDED