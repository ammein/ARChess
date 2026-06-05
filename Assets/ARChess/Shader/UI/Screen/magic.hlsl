// Inner outline shader for Match2 game cell.
// Based on shader created by @remonvv
// https://www.shadertoy.com/view/MdjfRK

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

float square_tunnel(float2 uv) {
    float2 p = -1.0 + 2.0 * uv;
    float2 t = p*p*p*p;
    return max(t.x, t.y);
}

float3 portal(float2 uv, float t)
{
    // create a fire texture
    float q = noise(uv + t);
    float f = fire(uv - (t * .934) - q);
    
    float sq = square_tunnel(uv);

    // apply fire everywhere to only select pixels
    float grad = f * (1. - (sq * 2. -1.));

    // can't really explain what this is doing
    grad = max(0., (1. - grad)/grad);

    // adjust fade
    grad /= (1. + grad);
    
    // add colour
    float3 portal = float3((grad * grad * grad), (grad * grad), grad); 
    
    return portal;
}

void magic_float(float2 uv, float time, out float3 Magic ){
    float p = portal(uv, time);
    Magic = p;
}
#endif //MYHLSLINCLUDE_INCLUDED