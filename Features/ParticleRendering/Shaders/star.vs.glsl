#version 430

struct ParticleDraw {
    vec2 pos;
    float size;
    float type;
    vec4 color;
};

layout(std430, binding = 1) readonly buffer DrawBuffer {
    ParticleDraw draws[];
};

uniform mat4 viewMat;
uniform mat4 projMat;
uniform int enableSoftGlow;
uniform int glowPass; // 0 = core, 1 = wide halo
uniform float glowStrength;

out vec4 vertexColor;
flat out int vertexType;

void main()
{
    ParticleDraw d = draws[gl_VertexID];
    vertexType = int(d.type);

    float size = d.size;
    vec4 color = d.color;

    if (enableSoftGlow != 0) {
        float strength = max(glowStrength, 0.5);
        bool dust = (vertexType == 1 || vertexType == 2);

        if (glowPass == 1) {
            // Huge soft bloom layer under the cores.
            size *= dust ? (5.5 * strength) : (3.8 * strength);
            color.rgb *= dust ? (0.55 * strength) : (0.40 * strength);
            color.a *= dust ? 0.9 : 0.75;
        } else {
            size *= dust ? (2.2 * strength) : (1.7 * strength);
            color.rgb *= 1.15;
        }
    }

    vertexColor = color;
    gl_PointSize = max(size, 1.0);
    gl_Position = projMat * viewMat * vec4(d.pos, 0.0, 1.0);
}
