#version 330

in vec2 fragTexCoord;
in vec3 fragNormal;

uniform sampler2D texture0;
uniform vec4 colDiffuse;
uniform vec3 lightDirection;
uniform vec3 lightColor;
uniform vec3 ambientColor;

out vec4 finalColor;

// Cel-shading: quantise the diffuse term into hard bands for a cartoon look.
const float BAND_COUNT = 3.0;

void main()
{
    vec4 albedo = texture(texture0, fragTexCoord) * colDiffuse;
    float diffuse = max(dot(normalize(fragNormal), normalize(-lightDirection)), 0.0);
    float banded = ceil(diffuse * BAND_COUNT) / BAND_COUNT;
    vec3 lit = albedo.rgb * (ambientColor + lightColor * banded);
    finalColor = vec4(lit, albedo.a);
}
