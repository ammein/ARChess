// Inner outline shader for Match2 game cell.
// Based on shader created by @remonvv
// https://www.shadertoy.com/view/MdjfRK
//
// Thanks to t.me/ru_cocos2dx@satan_black for help

// From: https://docs.unity3d.com/Packages/com.unity.shadergraph@17.6/manual/Custom-Function-Node.html#multiple-functions-and-multiple-files
//UNITY_SHADER_NO_UPGRADE
#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED
float random(float2 n) {
    return frac(sin(dot(n, float2(12.9898,12.1414))) * 83758.5453);
}

float noise(float2 n) {
    const float2 d = float2(0.0, 1.0);
    float2 b = floor(n);
    float2 f = lerp(float2(0.0, 0.0), float2(1.0, 1.0), frac(n));
    return lerp(lerp(random(b), random(b + d.yx), f.x), lerp(random(b + d.xy), random(b + d.yy), f.x), f.y);   
}

float3 ramp(float t) {
	return t <= .5 ? float3( 1. - t * 1.4, .2, 1.05 ) / t : float3( .3 * (1. - t) * 2., .2, 1.05 ) / t;
}

float fire(float2 n) {
    return noise(n) + noise(n * 2.1) * .6 + noise(n * 5.4) * .42;
}

float3 getLine(float3 col, float2 fc, float2x2 mtx, float time, float shift){
    float t = time;
    float2 uv = mul(mtx, fc);
    
    uv.x += uv.y < .5 ? 23.0 + t * .35 : -11.0 + t * .3;    
    uv.y = abs(uv.y - shift);
    uv *= 5.0;
    
    float q = fire(uv - t * .013) / 2.0;
    float2 r = float2(fire(uv + q / 2.0 + t - uv.x - uv.y), fire(uv + q - t));
    float3 color = float3(1.0 / (pow(float3(0.5, 0.0, .1) + 1.61, float3(4.0, 4.0, 4.0))));
    
    float grad = pow((r.y + r.y) * max(.0, uv.y) + .1, 4.0);
    color = ramp(grad);
    color /= (1.50 + max(float3(0, 0, 0), color));
    
    if(color.b < .00000005)
        color = float3(.0, .0 , .0);
    
    return lerp(col, color, color.b);
}

void magic_float(float2 uv, float2x2 mtx1, float2x2 mtx2, float time, float2 shift, out float3 Magic ){
    float3 color = float3(0., 0., 0.);
    color = getLine(color, uv, mtx1, time, shift.x);
    color = getLine(color, uv, mtx2, time, shift.y);
    Magic = color;
}
#endif //MYHLSLINCLUDE_INCLUDED