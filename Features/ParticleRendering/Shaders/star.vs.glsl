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

out vec4 vertexColor;
flat out int vertexType;

void main()
{
    ParticleDraw d = draws[gl_VertexID];
    vertexColor = d.color;
    vertexType = int(d.type);

    // Stars / H2 keep computed size. Dust already sized in compute for nebula.
    float size = d.size;
    if (enableSoftGlow != 0 && (vertexType == 1 || vertexType == 2))
        size *= 1.15; // slight extra spread for soft dust discs

    gl_PointSize = max(size, 1.0);
    gl_Position = projMat * viewMat * vec4(d.pos, 0.0, 1.0);
}
