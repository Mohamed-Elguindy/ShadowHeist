#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;

void main() {
    vec4 sum = vec4(0.0);
    vec2 texelScale = vec2(1.0 / 1280.0, 1.0 / 720.0);
    for (int x = -2; x <= 2; x++) {
        for (int y = -2; y <= 2; y++) {
            sum += texture(texture0, fragTexCoord + vec2(x, y) * texelScale * 1.5);
        }
    }
    vec4 base = texture(texture0, fragTexCoord);
    finalColor = (base + (sum / 25.0) * 0.45) * fragColor;
}
