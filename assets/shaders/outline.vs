#version 330

in vec3 vertexPosition;
in vec3 vertexNormal;

uniform mat4 mvp;
uniform float outlineWidth;

void main()
{
    vec3 extruded = vertexPosition + normalize(vertexNormal) * outlineWidth;
    gl_Position = mvp * vec4(extruded, 1.0);
}
