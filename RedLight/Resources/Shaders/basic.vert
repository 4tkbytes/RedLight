﻿#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoord;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec2 fUv;
out vec3 fNormal;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    fUv = aTexCoord;
    fNormal = aNormal;
}