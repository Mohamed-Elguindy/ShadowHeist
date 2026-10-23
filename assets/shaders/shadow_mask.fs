#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform sampler2D lightMask;
uniform float ambientStrength;

void main() {
    vec4 sceneCol = texture(texture0, fragTexCoord);
    vec4 lightCol = texture(lightMask, fragTexCoord);
    float lightFactor = clamp(ambientStrength + lightCol.r * lightCol.a, 0.0, 1.2);
    finalColor = vec4(sceneCol.rgb * lightFactor, sceneCol.a) * fragColor;
}
