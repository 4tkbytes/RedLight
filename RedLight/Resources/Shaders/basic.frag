#version 330 core
in vec2 fUv;

uniform sampler2D texture0;
uniform vec4 diffuseColor;

out vec4 FragColor;

void main()
{
    vec4 texColor = texture(texture0, fUv);
    FragColor = texColor * diffuseColor;
}