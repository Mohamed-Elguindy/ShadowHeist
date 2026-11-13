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

out vec4 finalColor;

// Stylized 3-band cel ramp with a crisp Fresnel rim highlight.
void main()
{
    vec4 albedo = texture(texture0, fragTexCoord) * colDiffuse;
    vec3 normal = normalize(fragNormal);
    vec3 lightDir = normalize(-lightDirection);
    float ndotl = dot(normal, lightDir);

    float band = 0.28;
    band += 0.36 * smoothstep(0.08, 0.14, ndotl);
    band += 0.36 * smoothstep(0.55, 0.62, ndotl);

    vec3 viewDir = normalize(viewPos - fragWorldPos);
    float fresnel = 1.0 - max(dot(normal, viewDir), 0.0);
    float rim = smoothstep(0.68, 0.78, fresnel) * max(ndotl * 0.5 + 0.5, 0.25);

    vec3 lit = albedo.rgb * (ambientColor + lightColor * band) + rimColor * rim;
    finalColor = vec4(lit, albedo.a);
}
