#version 430

in vec4 vertexColor;
flat in int vertexType;
out vec4 FragColor;

uniform int enableSoftGlow;

void main()
{
    vec2 circCoord = 2.0 * gl_PointCoord - 1.0;
    float dist = length(circCoord);
    if (dist > 1.0)
        discard;

    // Match VertexBufferStars.hpp alphas (clamped disc; discard avoids negative
    // corner alpha which can erase the clear color under additive blend).
    float alpha;
    if (vertexType == 0) {
        alpha = 1.0 - dist;
    } else if (vertexType == 1) {
        alpha = enableSoftGlow != 0
            ? 0.06 * exp(-dist * dist * 1.9)
            : 0.06 * (1.0 - dist);
    } else if (vertexType == 2) {
        alpha = enableSoftGlow != 0
            ? 0.08 * exp(-dist * dist * 1.7)
            : 0.08 * (1.0 - dist);
    } else {
        alpha = 1.0 - dist;
    }

    FragColor = vec4(vertexColor.rgb, max(alpha, 0.0));
}
