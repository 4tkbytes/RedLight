#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;

out vec2 frag_texCoords;

uniform mat4 transform;

void main()
{
    gl_Position = transform * vec4(aPosition, 1.0);
    frag_texCoords = aTexCoord;
}