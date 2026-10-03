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

    float alpha;

    if (vertexType == 0) {
        // Tight star cores.
        alpha = max(1.0 - dist, 0.0);
        alpha *= alpha;
    } else if (vertexType == 1) {
        if (enableSoftGlow != 0) {
            float g = exp(-dist * dist * 1.8);
            alpha = 0.10 * g;
        } else {
            alpha = 0.07 * (1.0 - dist);
        }
    } else if (vertexType == 2) {
        if (enableSoftGlow != 0) {
            float g = exp(-dist * dist * 1.6);
            alpha = 0.12 * g;
        } else {
            alpha = 0.09 * (1.0 - dist);
        }
    } else {
        alpha = 1.0 - dist;
    }

    FragColor = vec4(vertexColor.rgb, alpha * vertexColor.a);
}
