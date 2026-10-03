#version 430

in vec4 vertexColor;
flat in int vertexType;
out vec4 FragColor;

uniform int enableSoftGlow;
uniform int renderPass; // 0 = luminous additive, 1 = dark dust lanes

void main()
{
    vec2 circCoord = 2.0 * gl_PointCoord - 1.0;
    float dist = length(circCoord);
    if (dist > 1.0)
        discard;

    bool isLane = vertexType == 5;
    if (renderPass == 0 && isLane)
        discard;
    if (renderPass == 1 && !isLane)
        discard;

    float alpha;
    if (vertexType == 0) {
        alpha = 1.0 - dist;
    } else if (vertexType == 1) {
        alpha = enableSoftGlow != 0
            ? 0.045 * exp(-dist * dist * 1.6)
            : 0.05 * (1.0 - dist);
    } else if (vertexType == 2) {
        alpha = enableSoftGlow != 0
            ? 0.06 * exp(-dist * dist * 1.7)
            : 0.065 * (1.0 - dist);
    } else if (vertexType == 5) {
        // Multiply-pass factor: 1 = no change, brown = soft veil (never near-black).
        float strength = 0.22 * exp(-dist * dist * 4.5) * clamp(vertexColor.a, 0.0, 1.0);
        vec3 factor = mix(vec3(1.0), clamp(vertexColor.rgb, 0.50, 1.0), strength);
        FragColor = vec4(factor, 1.0);
        return;
    } else {
        alpha = 1.0 - dist;
    }

    FragColor = vec4(vertexColor.rgb, max(alpha, 0.0));
}
