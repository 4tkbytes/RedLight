#version 330 core

in vec2 v_uv;
out vec4 fragColor;

uniform vec4 u_color;
uniform sampler2D u_texture;

void main()
{
    vec4 texColor = texture(u_texture, v_uv);
    fragColor = texColor * u_color;
}