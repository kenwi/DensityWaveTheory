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
            ? 0.055 * exp(-dist * dist * 2.0)
            : 0.05 * (1.0 - dist);
    } else if (vertexType == 2) {
        alpha = enableSoftGlow != 0
            ? 0.07 * exp(-dist * dist * 1.8)
            : 0.065 * (1.0 - dist);
    } else if (vertexType == 5) {
        // Stronger brown occlusion so lanes actually cut the arms.
        alpha = 0.32 * exp(-dist * dist * 3.0) * clamp(vertexColor.a, 0.0, 1.0);
        FragColor = vec4(vertexColor.rgb, alpha);
        return;
    } else {
        alpha = 1.0 - dist;
    }

    FragColor = vec4(vertexColor.rgb, max(alpha, 0.0));
}
