#version 330 core

in vec2 frag_texCoords;

out vec4 FragColor;

uniform sampler2D texture0;

void main()
{
    FragColor = texture(texture0, frag_texCoords);
}