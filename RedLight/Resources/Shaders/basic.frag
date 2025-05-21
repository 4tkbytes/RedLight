#version 330 core
in vec2 fUv;
in vec3 fNormal;

uniform sampler2D texture0;
uniform vec4 diffuseColor;

out vec4 FragColor;

void main()
{
    vec4 texColor = texture(texture0, fUv);
    
    // If the texture has actual data, use it
    if (texColor.a > 0.1) {
        FragColor = texColor * diffuseColor;
    } 
    // Otherwise use material diffuse color
    else {
        FragColor = diffuseColor;
    }
}