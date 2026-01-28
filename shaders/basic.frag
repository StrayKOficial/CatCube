#version 330 core

in vec3 FragPos;
in vec3 Normal;
in vec3 vLocalPos;
in vec4 FragPosLightSpace;

uniform vec3 objectColor;
uniform vec3 objectScale; // Used for tiling
uniform vec3 lightPos;
uniform vec3 viewPos;
uniform int renderMode; // 0 = Standard, 1 = UI (No Fog), 2 = Smooth (No Studs)
uniform sampler2D shadowMap;

out vec4 FragColor;

float ShadowCalculation(vec4 fragPosLightSpace, vec3 normal, vec3 lightDir)
{
    // Perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // Transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;
    
    // Get closest depth value from light's perspective
    float closestDepth = texture(shadowMap, projCoords.xy).r; 
    // Get depth of current fragment from light's perspective
    float currentDepth = projCoords.z;
    
    // Calculate bias (based on depth map resolution and slope)
    float bias = max(0.005 * (1.0 - dot(normal, lightDir)), 0.0005);
    
    // PCF (Percentage-Closer Filtering) for soft shadows
    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);
    for(int x = -1; x <= 1; ++x)
    {
        for(int y = -1; y <= 1; ++y)
        {
            float pcfDepth = texture(shadowMap, projCoords.xy + vec2(x, y) * texelSize).r; 
            shadow += currentDepth - bias > pcfDepth  ? 1.0 : 0.0;        
        }    
    }
    shadow /= 9.0;
    
    // Keep shadows at 0.0 when outside the far_plane region of the light's frustum.
    if(projCoords.z > 1.0)
        shadow = 0.0;
        
    return shadow;
}

void main()
{
    // --- 1. Color Dilution (Desaturation) ---
    float desaturate = 0.2; // 20% desaturated
    float gray = dot(objectColor, vec3(0.299, 0.587, 0.114));
    vec3 baseColor = mix(objectColor, vec3(gray), desaturate);
    
    // --- 2. Advanced Shading (Fake AO & Softness) ---
    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(lightPos - FragPos);
    
    // Soft Diffuse (Half-Lambert)
    float diff = dot(norm, lightDir) * 0.5 + 0.5; 
    diff = pow(diff, 1.2); // Contrast
    
    // Fake Ambient Occlusion (Shadow edges/bottom)
    // Darken based on proximity to cube edges in local space
    vec3 aoGrid = abs(vLocalPos); // -0.5 to 0.5
    float ao = 1.0;
    // Darken bottom surfaces (y coord local)
    if (vLocalPos.y < -0.4) ao *= 0.85;
    // Edge darkening
    float edge = max(aoGrid.x, max(aoGrid.y, aoGrid.z));
    ao *= smoothstep(0.55, 0.4, edge); 
    ao = mix(0.7, 1.0, ao); // Clamp AO strength

    // --- 3. Lighting Model ---
    vec3 ambient = 0.4 * baseColor;
    vec3 diffuse = diff * baseColor;
    
    // Specular (Dull Plastic)
    vec3 viewDir = normalize(viewPos - FragPos);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 16);
    vec3 specular = 0.3 * spec * vec3(1.0);
    
    // Shadow calculation
    float shadow = ShadowCalculation(FragPosLightSpace, norm, lightDir);
    vec3 result = ambient + (1.0 - shadow) * (diffuse + specular);
    result *= ao;
    
    if (renderMode == 0 || renderMode == 2) {
        // --- 4. Stud Tiling Fix ---
        if (norm.y > 0.9 && renderMode == 0) {
            vec2 worldTiling = vLocalPos.xz * objectScale.xz;
            vec2 grid = fract(worldTiling + 0.5); 
            float dist = length(grid - vec2(0.5, 0.5));
            if (dist < 0.25) result *= 1.15; // Brighter studs
            else if (dist < 0.3) result *= 0.8;
        } 
        else if (norm.y < -0.9 && renderMode == 0) {
            vec2 worldTiling = vLocalPos.xz * objectScale.xz;
            vec2 grid = fract(worldTiling + 0.5);
            float dist = length(grid - vec2(0.5, 0.5));
            if (dist < 0.25) result *= 0.85;
        }
        
        // Retro Fog
        float distToCam = length(viewPos - FragPos);
        float fogStart = 20.0;
        float fogEnd = 140.0;
        float fogFactor = clamp((distToCam - fogStart) / (fogEnd - fogStart), 0.0, 1.0);
        vec3 fogColor = vec3(0.55, 0.78, 1.0); // Classic 2009 Horizon
        result = mix(result, fogColor, fogFactor);
    }

    if (renderMode == 3) {
        // Selection Outline / Gizmos (Flat Unlit)
        FragColor = vec4(objectColor, 0.4);
        return;
    }

    FragColor = vec4(result, 1.0);
}
