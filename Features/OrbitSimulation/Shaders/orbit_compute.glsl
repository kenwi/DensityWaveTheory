#version 430

layout(local_size_x = 256, local_size_y = 1, local_size_z = 1) in;

#define DEG_TO_RAD 0.01745329251

struct Star {
    float theta0;
    float velTheta;
    float tiltAngle;
    float a;
    float b;
    float temp;
    float mag;
    int type;
    float colorR;
    float colorG;
    float colorB;
    float colorA;
};

struct ParticleDraw {
    vec2 pos;
    float size;
    float type;
    vec4 color;
};

layout(std430, binding = 0) readonly buffer StarBuffer {
    Star stars[];
};

layout(std430, binding = 1) writeonly buffer DrawBuffer {
    ParticleDraw draws[];
};

uniform float time;
uniform int particleCount;
uniform int pertN;
uniform float pertAmp;
uniform int dustSize;
uniform float sizeFactor;
uniform float radCore;
uniform float radGalaxy;
uniform float radFarField;
uniform float exInner;
uniform float exOuter;
uniform float angleOffset;
uniform float h2SizeMax;
uniform float h2Threshold;
uniform int displayFeatures;
uniform int enableRadialPalette;
uniform float paletteStrength;
uniform int enableSoftGlow;
uniform float glowStrength;

vec2 calcPos(float a, float b, float theta, float velTheta, float timeSec, float tiltAngle) {
    float thetaActual = theta + velTheta * timeSec;
    float beta = -tiltAngle;
    float alpha = thetaActual * DEG_TO_RAD;
    float cosalpha = cos(alpha);
    float sinalpha = sin(alpha);
    float cosbeta = cos(beta);
    float sinbeta = sin(beta);
    vec2 ps = vec2(
        a * cosalpha * cosbeta - b * sinalpha * sinbeta,
        a * cosalpha * sinbeta + b * sinalpha * cosbeta);

    if (pertAmp > 0.0 && pertN > 0) {
        ps.x += (a / pertAmp) * sin(alpha * 2.0 * float(pertN));
        ps.y += (a / pertAmp) * cos(alpha * 2.0 * float(pertN));
    }
    return ps;
}

float excentricity(float r) {
    if (r < radCore)
        return 1.0 + (r / radCore) * (exInner - 1.0);
    else if (r <= radGalaxy)
        return exInner + (r - radCore) / (radGalaxy - radCore) * (exOuter - exInner);
    else if (r < radFarField)
        return exOuter + (r - radGalaxy) / (radFarField - radGalaxy) * (1.0 - exOuter);
    else
        return 1.0;
}

float tiltAt(float r) {
    return r * angleOffset;
}

vec3 radialPalette(float r) {
    float t = clamp(r / max(radGalaxy, 1.0), 0.0, 1.5);
    vec3 core = vec3(1.00, 0.72, 0.35);
    vec3 mid  = vec3(1.00, 0.55, 0.42);
    vec3 outer = vec3(0.45, 0.70, 1.10);
    vec3 halo = vec3(0.55, 0.45, 1.00);
    if (t < 0.35) return mix(core, mid, t / 0.35);
    if (t < 1.0) return mix(mid, outer, (t - 0.35) / 0.65);
    return mix(outer, halo, clamp((t - 1.0) / 0.5, 0.0, 1.0));
}

void main() {
    uint id = gl_GlobalInvocationID.x;
    if (id >= uint(particleCount))
        return;

    Star s = stars[id];
    vec2 ps = calcPos(s.a, s.b, s.theta0, s.velTheta, time, s.tiltAngle);

    vec4 color = vec4(s.colorR, s.colorG, s.colorB, s.colorA);
    // Cool bias on dust/filaments so mid/outer arms read blue-violet like the article.
    if (s.type == 1 || s.type == 2) {
        float t = clamp(s.a / max(radGalaxy, 1.0), 0.0, 1.5);
        float w = smoothstep(0.12, 0.95, t);
        color.r *= mix(1.0, 0.70, w);
        color.g *= mix(1.0, 0.90, w);
        color.b *= mix(1.05, 1.32, w);
    }
    if (enableRadialPalette != 0 && s.type != 3 && s.type != 4) {
        float strength = paletteStrength * ((s.type == 1 || s.type == 2) ? 0.35 : 0.2);
        color.rgb = mix(color.rgb, radialPalette(s.a), strength);
    }

    float pointSize = 1.0;
    vec4 vertexColor = color * s.mag;
    int type = s.type;

    // Exact size rules from VertexBufferStars.hpp
    float dustPx = float(dustSize);
    if (enableSoftGlow != 0)
        dustPx *= max(glowStrength, 0.5);

    if (type == 0) {
        pointSize = s.mag * 4.0;
        vertexColor = color * s.mag;
        if ((displayFeatures & 1) == 0) pointSize = 0.0;
    } else if (type == 1) {
        pointSize = s.mag * 5.0 * dustPx;
        vertexColor = color * s.mag;
        if ((displayFeatures & 2) == 0) pointSize = 0.0;
    } else if (type == 2) {
        pointSize = s.mag * 2.0 * dustPx;
        vertexColor = color * s.mag;
        if ((displayFeatures & 4) == 0) pointSize = 0.0;
    } else if (type == 3 || type == 4) {
        float delta = 1000.0;
        float aI = max(s.a - delta, 0.0);
        float dI = s.a - aI;
        float aO = s.a + delta;
        float tA = tiltAt(s.a);
        float tI = tiltAt(aI);
        float tO = tiltAt(aO);
        vec2 psI = calcPos(aI, aI * excentricity(aI), s.theta0 - (tA - tI) / DEG_TO_RAD, s.velTheta, time, tI);
        vec2 psO = calcPos(aO, aO * excentricity(aO), s.theta0 + (tO - tA) / DEG_TO_RAD, s.velTheta, time, tO);
        float rho = 0.5 * (dI / max(distance(ps, psI), 1.0) + delta / max(distance(ps, psO), 1.0));
        float ignite = smoothstep(h2Threshold, 1.5 * h2Threshold, rho);
        if ((displayFeatures & 8) == 0) ignite = 0.0;

        if (type == 3) {
            pointSize = h2SizeMax * ignite;
            vertexColor = color * s.mag * vec4(2.0, 0.5, 0.5, 1.0) * ignite;
        } else {
            pointSize = h2SizeMax * ignite / 10.0;
            vertexColor = vec4(1.0) * ignite;
        }
    }

    pointSize = max(pointSize * sizeFactor, 0.0);

    ParticleDraw d;
    d.pos = ps;
    d.size = pointSize;
    d.type = float(type);
    d.color = vertexColor;
    draws[id] = d;
}
