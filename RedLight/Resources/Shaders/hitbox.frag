// fragment shader used to render lines for hitboxes
// DO NOT TOUCH THIS IS PERFECTED ANY SORT OF FUCKUP WILL BREAK THE HITBOXES
#version 330 core
out vec4 FragColor;

uniform vec4 uColor;

void main()
{
    FragColor = uColor;
}