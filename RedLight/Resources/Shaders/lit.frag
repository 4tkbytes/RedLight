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

    vec3 diffuse = directionalLight_color * diff * directionalLight_intensity;
    return diffuse;
}

vec3 calculatePointLight(vec3 normal, vec3 viewDir)
{
    vec3 lightDir = normalize(pointLight_position - frag_worldPos);
    float diff = max(dot(normal, lightDir), 0.0);

    float distance = length(pointLight_position - frag_worldPos);
    float attenuation = 1.0 / (pointLight_constant + pointLight_linear * distance + pointLight_quadratic * (distance * distance));

    vec3 diffuse = pointLight_color * diff * pointLight_intensity * attenuation;
    return diffuse;
}

void main()
{
    vec3 color = texture(texture_diffuse1, frag_texCoords).rgb;
    vec3 normal = normalize(frag_normal);
    vec3 viewDir = normalize(viewPos - frag_worldPos);

    // Ambient
    vec3 ambient = ambientColor * ambientStrength;

    // Directional light
    vec3 directional = calculateDirectionalLight(normal, viewDir);

    // Point light
    vec3 point = calculatePointLight(normal, viewDir);

    // Combine all lighting
    vec3 result = (ambient + directional + point) * color;

    FragColor = vec4(result, 1.0);
}