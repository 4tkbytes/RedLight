#version 330 core
out vec4 FragColor;

struct Material {
    sampler2D diffuse;
    sampler2D specular;
    float shininess;
    float reflectivity; // Add reflection strength
};

struct DirLight {
    vec3 direction;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

struct PointLight {
    vec3 position;
    float constant;
    float linear;
    float quadratic;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

struct SpotLight {
    vec3 position;
    vec3 direction;
    float cutOff;
    float outerCutOff;
    float constant;
    float linear;
    float quadratic;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

// Fog structure
struct Fog {
    vec3 color;
    float density;
    float start;
    float end;
    int type; // 0 = linear, 1 = exponential, 2 = exponential squared
};

#define NR_POINT_LIGHTS 4

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoords;
in vec3 WorldPos; // Add this

uniform vec3 viewPos;
uniform DirLight dirLight;
uniform PointLight pointLights[NR_POINT_LIGHTS];
uniform SpotLight spotLight;
uniform Material material;
uniform Fog fog;
uniform samplerCube skybox; // Add skybox for reflections
uniform bool enableReflection; // Toggle reflection on/off

// function prototypes
vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir);
vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir);
vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir);
float CalcFogFactor(float distance);
vec3 CalcReflection(vec3 normal, vec3 viewDir);

void main()
{
    // alpha and discard transparent fragments
    vec4 texColor = texture(material.diffuse, TexCoords);
    if(texColor.a < 0.1)
    discard;

    // properties
    vec3 norm = normalize(Normal);
    vec3 viewDir = normalize(viewPos - WorldPos);

    // Calculate lighting
    vec3 result = CalcDirLight(dirLight, norm, viewDir);
    for(int i = 0; i < NR_POINT_LIGHTS; i++)
    result += CalcPointLight(pointLights[i], norm, FragPos, viewDir);
    result += CalcSpotLight(spotLight, norm, FragPos, viewDir);

    // Add reflection if enabled
    if (enableReflection) {
        vec3 reflection = CalcReflection(norm, viewDir);
        result = mix(result, reflection, material.reflectivity);
    }

    // Apply fog
    float distance = length(viewPos - FragPos);
    float fogFactor = CalcFogFactor(distance);

    // Mix lighting result with fog color
    vec3 finalColor = mix(fog.color, result, fogFactor);

    FragColor = vec4(finalColor, 1.0);
}

// Calculate reflection from skybox
vec3 CalcReflection(vec3 normal, vec3 viewDir)
{
    vec3 I = -viewDir; // Incident vector (from camera to fragment)
    vec3 R = reflect(I, normal); // Reflect around normal
    return texture(skybox, R).rgb;
}

// Calculate fog factor based on distance and fog type
float CalcFogFactor(float distance)
{
    float fogFactor = 1.0;

    if (fog.density <= 0.0) {
        return 1.0; // No fog
    }

    if (fog.type == 0) {
        // Linear fog
        fogFactor = (fog.end - distance) / (fog.end - fog.start);
    }
    else if (fog.type == 1) {
        // Exponential fog
        fogFactor = exp(-fog.density * distance);
    }
    else if (fog.type == 2) {
        // Exponential squared fog
        fogFactor = exp(-pow(fog.density * distance, 2.0));
    }

    return clamp(fogFactor, 0.0, 1.0);
}

// calculates the color when using a directional light.
vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir)
{
    vec3 lightDir = normalize(-light.direction);
    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    // combine results
    vec3 ambient = light.ambient * vec3(texture(material.diffuse, TexCoords));
    vec3 diffuse = light.diffuse * diff * vec3(texture(material.diffuse, TexCoords));
    vec3 specular = light.specular * spec * vec3(texture(material.specular, TexCoords));
    return (ambient + diffuse + specular);
}

// calculates the color when using a point light.
vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.position - fragPos);
    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    // attenuation
    float distance = length(light.position - fragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));
    // combine results
    vec3 ambient = light.ambient * vec3(texture(material.diffuse, TexCoords));
    vec3 diffuse = light.diffuse * diff * vec3(texture(material.diffuse, TexCoords));
    vec3 specular = light.specular * spec * vec3(texture(material.specular, TexCoords));
    ambient *= attenuation;
    diffuse *= attenuation;
    specular *= attenuation;
    return (ambient + diffuse + specular);
}

// calculates the color when using a spot light.
vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.position - fragPos);
    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    // attenuation
    float distance = length(light.position - fragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));
    // spotlight intensity
    float theta = dot(lightDir, normalize(-light.direction));
    float epsilon = light.cutOff - light.outerCutOff;
    float intensity = clamp((theta - light.outerCutOff) / epsilon, 0.0, 1.0);
    // combine results
    vec3 ambient = light.ambient * vec3(texture(material.diffuse, TexCoords));
    vec3 diffuse = light.diffuse * diff * vec3(texture(material.diffuse, TexCoords));
    vec3 specular = light.specular * spec * vec3(texture(material.specular, TexCoords));
    ambient *= attenuation * intensity;
    diffuse *= attenuation * intensity;
    specular *= attenuation * intensity;
    return (ambient + diffuse + specular);
}