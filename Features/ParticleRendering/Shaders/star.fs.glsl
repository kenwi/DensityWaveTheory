#version 430

in vec4 vertexColor;
flat in int vertexType;
out vec4 FragColor;

void main()
{
    vec2 circCoord = 2.0 * gl_PointCoord - 1.0;
    float dist = length(circCoord);
    if (dist > 1.0)
        discard;

    float alpha;
    if (vertexType == 0) {
        alpha = 1.0 - dist;
    } else if (vertexType == 1) {
        alpha = 0.05 * (1.0 - dist);
    } else if (vertexType == 2) {
        alpha = 0.07 * (1.0 - dist);
    } else {
        alpha = 1.0 - dist;
    }

    FragColor = vec4(vertexColor.rgb, alpha * vertexColor.a);
}
