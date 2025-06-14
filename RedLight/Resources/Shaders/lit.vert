#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aNormal;

out vec2 frag_texCoords;
out vec3 frag_normal;
out vec3 frag_worldPos;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    vec4 worldPos = model * vec4(aPos, 1.0);
    frag_worldPos = worldPos.xyz;
    frag_normal = mat3(transpose(inverse(model))) * aNormal;
    frag_texCoords = aTexCoord;

    gl_Position = projection * view * worldPos;
}