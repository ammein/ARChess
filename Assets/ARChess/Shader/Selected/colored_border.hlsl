// From: https://docs.unity3d.com/Packages/com.unity.shadergraph@17.6/manual/Custom-Function-Node.html#multiple-functions-and-multiple-files
//UNITY_SHADER_NO_UPGRADE
#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED
#include "../lygia/sampler.hlsl"
void colored_border_float(Texture2D tex, float2 uv, float2 p1, float2 p2, float2 p3, float2 p4, float speed, float time, float glowIntensity,  out float3 color, out float alpha)
{
    float4 c2 = SAMPLER_FNC(tex, uv + time / speed);
    
    float d1 = step(p1.x,uv.x)*step(uv.x,p4.x)*abs(uv.y-p1.y)+
        step(uv.x,p1.x)*distance(uv,p1)+step(p4.x,uv.x)*distance(uv,p4);
    d1 = min(step(p3.x,uv.x)*step(uv.x,p2.x)*abs(uv.y-p2.y)+
        step(uv.x,p3.x)*distance(uv,p3)+step(p2.x,uv.x)*distance(uv,p2),d1);
    d1 = min(step(p1.y,uv.y)*step(uv.y,p3.y)*abs(uv.x-p1.x)+
        step(uv.y,p1.y)*distance(uv,p1)+step(p3.y,uv.y)*distance(uv,p3),d1);
    d1 = min(step(p4.y,uv.y)*step(uv.y,p2.y)*abs(uv.x-p2.x)+
        step(uv.y,p4.y)*distance(uv,p4)+step(p2.y,uv.y)*distance(uv,p2),d1);
        
    float f1 = .01 / abs(d1 + c2.r/100.);
    
    // Time varying pixel color
    float3 col = 0.5 + 0.5*cos(time+uv.xyx+float3(0,2,4));

    color = float4(f1 * col, 1.0) * glowIntensity;
    alpha = f1;
}
#endif //MYHLSLINCLUDE_INCLUDED