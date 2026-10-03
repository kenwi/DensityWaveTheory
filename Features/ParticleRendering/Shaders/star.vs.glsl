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

out vec4 vertexColor;
flat out int vertexType;

void main()
{
    ParticleDraw d = draws[gl_VertexID];
    vertexColor = d.color;
    vertexType = int(d.type);
    gl_PointSize = max(d.size, 0.0);
    gl_Position = projMat * viewMat * vec4(d.pos, 0.0, 1.0);
}
