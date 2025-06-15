#version 330 core

in vec2 frag_texCoords;

out vec4 FragColour;

uniform sampler2D uTexture;
uniform vec3 lightColor;

void main() {
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * lightColor;

    vec3 objectColour = texture(uTexture, frag_texCoords).rgb;

    vec3 result = ambient * objectColour;
    FragColour = vec4(result, 1.0);
}