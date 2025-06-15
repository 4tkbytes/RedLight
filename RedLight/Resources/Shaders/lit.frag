// lighting for all 3d models
#version 330 core

in vec2 TexCoord;
in vec3 Normal;
in vec3 FragPos;

out vec4 FragColour;

uniform sampler2D uTexture;
uniform vec3 lightColor;
uniform vec3 lightPos;

void main() {
    // ambient
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * lightColor;

    // get object color from texture
    vec3 objectColor = texture(uTexture, TexCoord).rgb;

    // diffuse 
    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(lightPos - FragPos);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;

    vec3 result = (ambient + diffuse) * objectColor;
    FragColour = vec4(result, 1.0);
}