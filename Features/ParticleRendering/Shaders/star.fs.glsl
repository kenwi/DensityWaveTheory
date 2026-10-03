#version 430

in vec4 vertexColor;
flat in int vertexType;
out vec4 FragColor;

uniform int enableSoftGlow;
uniform int glowPass;

void main()
{
    vec2 circCoord = 2.0 * gl_PointCoord - 1.0;
    float dist = length(circCoord);
    if (dist > 1.0)
        discard;

    float alpha;
    if (enableSoftGlow != 0) {
        bool dust = (vertexType == 1 || vertexType == 2);
        // Halo pass: very wide Gaussian. Core pass: tight bright core + soft skirt.
        float sigma = glowPass == 1
            ? (dust ? 0.85 : 0.70)
            : (dust ? 0.60 : 0.38);
        float g = exp(-(dist * dist) / (2.0 * sigma * sigma));

        if (glowPass == 1) {
            if (vertexType == 0)
                alpha = 0.55 * g;
            else if (vertexType == 1)
                alpha = 0.22 * g;
            else if (vertexType == 2)
                alpha = 0.26 * g;
            else
                alpha = 0.50 * g;
        } else {
            if (vertexType == 0)
                alpha = g;
            else if (vertexType == 1)
                alpha = 0.16 * g;
            else if (vertexType == 2)
                alpha = 0.18 * g;
            else
                alpha = g;
        }
    } else {
        if (vertexType == 0)
            alpha = 1.0 - dist;
        else if (vertexType == 1)
            alpha = 0.05 * (1.0 - dist);
        else if (vertexType == 2)
            alpha = 0.07 * (1.0 - dist);
        else
            alpha = 1.0 - dist;
    }

    FragColor = vec4(vertexColor.rgb, alpha * vertexColor.a);
}
