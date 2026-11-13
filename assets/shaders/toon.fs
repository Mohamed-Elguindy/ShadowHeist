#version 330

in vec2 fragTexCoord;
in vec3 fragNormal;
in vec3 fragWorldPos;

uniform sampler2D texture0;
uniform vec4 colDiffuse;
uniform vec3 lightDirection;
uniform vec3 lightColor;
uniform vec3 ambientColor;
uniform vec3 rimColor;
uniform vec3 viewPos;

uniform vec3 lampPos0;
uniform vec3 lampPos1;
uniform vec3 guardPos0;
uniform vec3 guardDir0;
uniform vec3 guardPos1;
uniform vec3 guardDir1;
uniform vec3 playerPos;
uniform float alarmActive;

out vec4 finalColor;

float quantizeBand(float v)
{
    float b = 0.15;
    b += 0.42 * smoothstep(0.08, 0.16, v);
    b += 0.43 * smoothstep(0.52, 0.62, v);
    return b;
}

vec3 evalPointLamp(vec3 lampPos, vec3 worldPos, vec3 n, vec3 color)
{
    vec3 toLight = lampPos - worldPos;
    float dist = length(toLight);
    float atten = clamp(1.0 - dist / 290.0, 0.0, 1.0);
    atten = smoothstep(0.0, 1.0, atten * atten);
    float ndotl = max(dot(n, normalize(toLight)), 0.0);
    return color * quantizeBand(ndotl) * atten;
}

vec3 evalSpotlight(vec3 gPos, vec3 gDir, vec3 worldPos, vec3 n, vec3 color)
{
    vec3 offset = worldPos - gPos;
    float dist = length(offset.xz);
    if (dist > 275.0 || dist < 1.0) return vec3(0.0);
    vec2 dirXZ = normalize(offset.xz);
    float align = dot(dirXZ, normalize(gDir.xz));
    float cone = smoothstep(0.76, 0.88, align);
    float rangeFade = clamp(1.0 - dist / 275.0, 0.0, 1.0);
    float ndotl = max(dot(n, normalize(gPos - worldPos)), 0.25);
    return color * quantizeBand(ndotl) * cone * rangeFade;
}

void main()
{
    vec4 albedo = texture(texture0, fragTexCoord) * colDiffuse;
    vec3 normal = normalize(fragNormal);
    vec3 lightDir = normalize(-lightDirection);
    float ndotl = dot(normal, lightDir);

    float dirBand = 0.22 + 0.38 * smoothstep(0.06, 0.14, ndotl) + 0.40 * smoothstep(0.54, 0.62, ndotl);
    vec3 lighting = ambientColor + lightColor * dirBand;

    lighting += evalPointLamp(lampPos0, fragWorldPos, normal, vec3(0.98, 0.78, 0.42));
    lighting += evalPointLamp(lampPos1, fragWorldPos, normal, vec3(0.98, 0.78, 0.42));

    vec3 spotColor = mix(vec3(0.95, 0.80, 0.36), vec3(1.0, 0.24, 0.28), alarmActive);
    lighting += evalSpotlight(guardPos0, guardDir0, fragWorldPos, normal, spotColor);
    lighting += evalSpotlight(guardPos1, guardDir1, fragWorldPos, normal, spotColor);

    float pDist = length(fragWorldPos.xz - playerPos.xz);
    float pFill = clamp(1.0 - pDist / 135.0, 0.0, 1.0);
    lighting += vec3(0.06, 0.14, 0.22) * (pFill * pFill);

    vec3 viewDir = normalize(viewPos - fragWorldPos);
    float fresnel = 1.0 - max(dot(normal, viewDir), 0.0);
    float rim = smoothstep(0.66, 0.78, fresnel) * max(ndotl * 0.5 + 0.5, 0.3);

    vec3 lit = albedo.rgb * lighting + rimColor * rim;
    finalColor = vec4(lit, albedo.a);
}
