#version 330 core
out vec4 FragColor;

struct Material {
    sampler2D diffuse;
    sampler2D specular;
    float shininess;
};

struct Light {
    vec3 position;  // point
    vec3 direction; // directional

    vec3 ambient;
    vec3 diffuse;
    vec3 specular;

    // point
    float constant;
    float linear;
    float quadratic;
    
    // spotlight
    float cutOff;

    int type; // 0 for directional, 1 for point, 2 for spotlight
    // just like the LightType enum
};

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoords;

uniform vec3 viewPos;
uniform Material material;
uniform Light light;

void main()
{
    // ambient calculation (common base)
    vec3 ambient = light.ambient * texture(material.diffuse, TexCoords).rgb;
    vec3 norm = normalize(Normal);
    vec3 lightDir;
    vec3 result;

    if (light.type == 0) {
        // Directional light
        lightDir = normalize(-light.direction);

        float diff = max(dot(norm, lightDir), 0.0);
        vec3 diffuse = light.diffuse * diff * texture(material.diffuse, TexCoords).rgb;

        vec3 viewDir = normalize(viewPos - FragPos);
        vec3 reflectDir = reflect(-lightDir, norm);
        float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
        vec3 specular = light.specular * spec * texture(material.specular, TexCoords).rgb;

        result = ambient + diffuse + specular;
    }
    else if (light.type == 1) {
        // Point light
        lightDir = normalize(light.position - FragPos);

        float diff = max(dot(norm, lightDir), 0.0);
        vec3 diffuse = light.diffuse * diff * texture(material.diffuse, TexCoords).rgb;

        vec3 viewDir = normalize(viewPos - FragPos);
        vec3 reflectDir = reflect(-lightDir, norm);
        float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
        vec3 specular = light.specular * spec * texture(material.specular, TexCoords).rgb;

        float distance = length(light.position - FragPos);
        float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));

        ambient *= attenuation;
        diffuse *= attenuation;
        specular *= attenuation;

        result = ambient + diffuse + specular;
    }
    else if (light.type == 2) {
        // Spotlight
        lightDir = normalize(light.position - FragPos);

        // Check if lighting is inside the spotlight cone
        float theta = dot(lightDir, normalize(-light.direction));

        if(theta > light.cutOff) {
            // Inside the spotlight cone
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = light.diffuse * diff * texture(material.diffuse, TexCoords).rgb;

            vec3 viewDir = normalize(viewPos - FragPos);
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
            vec3 specular = light.specular * spec * texture(material.specular, TexCoords).rgb;

            float distance = length(light.position - FragPos);
            float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));

            // Keep ambient constant, only attenuate diffuse and specular
            diffuse *= attenuation;
            specular *= attenuation;

            result = ambient + diffuse + specular;
        } else {
            // Outside the spotlight cone - only ambient
            result = ambient;
        }
    }

    FragColor = vec4(result, 1.0);
}