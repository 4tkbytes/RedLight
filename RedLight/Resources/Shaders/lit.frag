#version 330 core

in vec3 frag_worldPos;
in vec3 frag_normal;
in vec2 frag_texCoords;

out vec4 FragColor;

uniform sampler2D texture_diffuse1;
uniform vec3 viewPos;

// Lighting uniforms
uniform vec3 ambientColor;
uniform float ambientStrength;

// Directional light
uniform vec3 directionalLight_direction;
uniform vec3 directionalLight_color;
uniform float directionalLight_intensity;

// Point light
uniform vec3 pointLight_position;
uniform vec3 pointLight_color;
uniform float pointLight_intensity;
uniform float pointLight_constant;
uniform float pointLight_linear;
uniform float pointLight_quadratic;

vec3 calculateDirectionalLight(vec3 normal, vec3 viewDir)
{
    vec3 lightDir = normalize(-directionalLight_direction);
    float diff = max(dot(normal, lightDir), 0.0);
    return directionalLight_color * diff * directionalLight_intensity;
}

vec3 calculatePointLight(vec3 normal, vec3 viewDir)
{
    if (pointLight_intensity <= 0.0) return vec3(0.0);

    vec3 lightDir = normalize(pointLight_position - frag_worldPos);
    float distance = length(pointLight_position - frag_worldPos);
    float attenuation = 1.0 / (pointLight_constant + pointLight_linear * distance + pointLight_quadratic * (distance * distance));

    float diff = max(dot(normal, lightDir), 0.0);
    return pointLight_color * diff * pointLight_intensity * attenuation;
}

void main()
{
    vec3 color = texture(texture_diffuse1, frag_texCoords).rgb;
    vec3 normal = normalize(frag_normal);
    vec3 viewDir = normalize(viewPos - frag_worldPos);

    // VERY SIMPLE lighting - if this doesn't work, there's a deeper problem
    vec3 ambient = vec3(0.2, 0.2, 0.2); // Fixed ambient light
    vec3 directional = calculateDirectionalLight(normal, viewDir);
    vec3 point = calculatePointLight(normal, viewDir);

    // Simple combination
    vec3 result = (ambient + directional + point) * color;

    FragColor = vec4(result, 1.0);
}