#include "Renderer.hpp"
#include <cmath>
#include <algorithm>
#include <iostream>
#include <cstring>

namespace CatCube {

// --- Shaders ---
const char* SHADOW_VS = R"(
    uniform mat4 u_LightSpaceMatrix;
    void main() {
        gl_Position = u_LightSpaceMatrix * gl_Vertex;
    }
)";

const char* SHADOW_FS = R"(
    void main() {
        // Depth only
    }
)";

const char* MAIN_VS = R"(
    varying vec3 v_Normal;
    varying vec4 v_ShadowCoord;
    uniform mat4 u_LightSpaceMatrix;
    
    void main() {
        v_Normal = gl_NormalMatrix * gl_Normal;
        gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
        // Transform local vertex to Light Clip Space
        v_ShadowCoord = u_LightSpaceMatrix * gl_Vertex;
    }
)";

const char* MAIN_FS = R"(
    varying vec3 v_Normal;
    varying vec4 v_ShadowCoord;
    uniform sampler2D u_ShadowMap;
    uniform vec3 u_LightDir; // In Eye Space
    uniform vec3 u_Color;
    
    void main() {
        vec3 normal = normalize(v_Normal);
        float diff = max(dot(normal, normalize(u_LightDir)), 0.0);
        
        // Shadow mapping
        vec3 projCoords = v_ShadowCoord.xyz / v_ShadowCoord.w;
        projCoords = projCoords * 0.5 + 0.5; // [0, 1] range
        
        float closestDepth = texture2D(u_ShadowMap, projCoords.xy).r;
        float currentDepth = projCoords.z;
        
        float bias = 0.005; 
        float shadow = currentDepth - bias > closestDepth ? 0.35 : 1.0;
        
        if(projCoords.z > 1.0) shadow = 1.0;

        vec3 ambient = u_Color * 0.45;
        vec3 diffuse = u_Color * diff * shadow;
        
        gl_FragColor = vec4(ambient + diffuse, 1.0);
    }
)";

// --- Matrix Helpers ---
void mat4_identity(float* m) {
    memset(m, 0, 16*sizeof(float));
    m[0]=m[5]=m[10]=m[15]=1.0f;
}

void mat4_mul(const float* A, const float* B, float* out) {
    float res[16];
    for (int i=0; i<4; i++) { // Row of A
        for (int j=0; j<4; j++) { // Col of B
            res[i + j*4] = 0;
            for (int k=0; k<4; k++)
                res[i + j*4] += A[i + k*4] * B[k + j*4];
        }
    }
    memcpy(out, res, 16*sizeof(float));
}

void mat4_translate(float* m, float x, float y, float z) {
    float t[16]; mat4_identity(t);
    t[12]=x; t[13]=y; t[14]=z;
    mat4_mul(m, t, m);
}

void mat4_rotateY(float* m, float angleDeg) {
    float rad = angleDeg * 3.14159f / 180.0f;
    float c = cos(rad), s = sin(rad);
    float r[16]; mat4_identity(r);
    r[0]=c; r[2]=-s; r[8]=s; r[10]=c;
    mat4_mul(m, r, m);
}

void mat4_scale(float* m, float x, float y, float z) {
    float s[16]; mat4_identity(s);
    s[0]=x; s[5]=y; s[10]=z;
    mat4_mul(m, s, m);
}

Renderer::Renderer() {}
Renderer::~Renderer() {
    if (m_cubeList) glDeleteLists(m_cubeList, 1);
    if (m_studList) glDeleteLists(m_studList, 1);
    if (m_shadowFBO) glDeleteFramebuffers(1, &m_shadowFBO);
    if (m_shadowMap) glDeleteTextures(1, &m_shadowMap);
}

void Renderer::init(int width, int height) {
    glewInit();
    m_width = width; m_height = height;
    glEnable(GL_DEPTH_TEST); glDepthFunc(GL_LESS); glEnable(GL_CULL_FACE);

    m_shaderShadow.load(SHADOW_VS, SHADOW_FS);
    m_shaderMain.load(MAIN_VS, MAIN_FS);

    glGenFramebuffers(1, &m_shadowFBO);
    glGenTextures(1, &m_shadowMap);
    glBindTexture(GL_TEXTURE_2D, m_shadowMap);
    glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, SHADOW_WIDTH, SHADOW_HEIGHT, 0, GL_DEPTH_COMPONENT, GL_FLOAT, NULL);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_BORDER);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_BORDER);
    float borderColor[] = { 1.0, 1.0, 1.0, 1.0 };
    glTexParameterfv(GL_TEXTURE_2D, GL_TEXTURE_BORDER_COLOR, borderColor);

    glBindFramebuffer(GL_FRAMEBUFFER, m_shadowFBO);
    glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D, m_shadowMap, 0);
    glDrawBuffer(GL_NONE); glReadBuffer(GL_NONE);
    glBindFramebuffer(GL_FRAMEBUFFER, 0);

    createDisplayLists();
}

void Renderer::renderHierarchy(InstancePtr root) {
    // 1. Light Matrix Calculation
    float lp[16]; mat4_identity(lp);
    float zoom = 150.0f;
    float n = 1.0f, f = 500.0f;
    lp[0] = 1.0f / zoom;
    lp[5] = 1.0f / zoom;
    lp[10] = -2.0f / (f - n);
    lp[14] = -(f + n) / (f - n);
    lp[15] = 1.0f;

    float lv[16]; mat4_identity(lv);
    glMatrixMode(GL_MODELVIEW); glPushMatrix(); glLoadIdentity();
    lookAt({100, 200, 100}, {0,0,0}, {0,1,0});
    glGetFloatv(GL_MODELVIEW_MATRIX, lv);
    glPopMatrix();

    float lightSpaceProjView[16];
    mat4_mul(lp, lv, lightSpaceProjView);

    auto renderAll = [&](auto self, InstancePtr inst, Shader& s, bool shadowPass) -> void {
        auto part = std::dynamic_pointer_cast<BasePart>(inst);
        if (part) {
            if (part->getTransparency() >= 1.0f) return;

            float model[16]; mat4_identity(model);
            Vector3 p = part->getPosition(); Vector3 r = part->getRotation(); Vector3 sz = part->getSize();
            mat4_translate(model, p.x, p.y, p.z);
            mat4_rotateY(model, r.y);
            mat4_scale(model, sz.x, sz.y, sz.z);
            
            float plm[16];
            mat4_mul(lightSpaceProjView, model, plm);
            s.setMat4("u_LightSpaceMatrix", plm);

            if(!shadowPass) s.setVec3("u_Color", part->getColor().r, part->getColor().g, part->getColor().b);

            glPushMatrix();
            glTranslatef(p.x, p.y, p.z);
            glRotatef(r.y, 0, 1, 0); glRotatef(r.x, 1, 0, 0); glRotatef(r.z, 0, 0, 1);
            glScalef(sz.x, sz.y, sz.z);
            glCallList(m_cubeList);
            glPopMatrix();

            // Studs
            if (!shadowPass) {
                s.setVec3("u_Color", part->getColor().r * 0.85f, part->getColor().g * 0.85f, part->getColor().b * 0.85f);
                float sx = sz.x, szz = sz.z, sy = sz.y;
                int studsX = std::max(1, (int)(sx / 1.0f));
                int studsZ = std::max(1, (int)(szz / 1.0f));
                if (studsX * studsZ <= 1024) {
                    float spX = sx / studsX; float spZ = szz / studsZ;
                    float stX = -sx/2.0f + spX/2.0f; float stZ = -szz/2.0f + spZ/2.0f;
                    float hy = sy / 2.0f;
                    for (int i=0; i<studsX; i++) {
                        for (int j=0; j<studsZ; j++) {
                            glPushMatrix();
                            glTranslatef(p.x, p.y, p.z);
                            glRotatef(r.y, 0, 1, 0); glRotatef(r.x, 1, 0, 0); glRotatef(r.z, 0, 0, 1);
                            glTranslatef(stX + i*spX, hy, stZ + j*spZ);
                            glScalef(0.4f, 0.15f, 0.4f);
                            glCallList(m_studList);
                            glPopMatrix();
                        }
                    }
                }
            }
        }
        for(auto& child : inst->getChildren()) self(self, child, s, shadowPass);
    };

    // PASS 1: SHADOW
    glBindFramebuffer(GL_FRAMEBUFFER, m_shadowFBO);
    glViewport(0, 0, SHADOW_WIDTH, SHADOW_HEIGHT);
    glClear(GL_DEPTH_BUFFER_BIT);
    m_shaderShadow.use();
    renderAll(renderAll, root, m_shaderShadow, true);
    glBindFramebuffer(GL_FRAMEBUFFER, 0);

    // PASS 2: MAIN
    glViewport(0, 0, m_width, m_height);
    glClearColor(0.52f, 0.81f, 0.92f, 1.0f);
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    setupProjection();
    glMatrixMode(GL_MODELVIEW); glLoadIdentity();
    lookAt(m_camera.position, m_camera.target, m_camera.up);
    
    float cv[16]; glGetFloatv(GL_MODELVIEW_MATRIX, cv);
    Vector3 lightWorld = Vector3(100, 200, 100).normalized();
    Vector3 eyeSun = {
        lightWorld.x*cv[0] + lightWorld.y*cv[4] + lightWorld.z*cv[8],
        lightWorld.x*cv[1] + lightWorld.y*cv[5] + lightWorld.z*cv[9],
        lightWorld.x*cv[2] + lightWorld.y*cv[6] + lightWorld.z*cv[10]
    };

    m_shaderMain.use();
    m_shaderMain.setVec3("u_LightDir", eyeSun.x, eyeSun.y, eyeSun.z);
    m_shaderMain.setInt("u_ShadowMap", 0);
    glActiveTexture(GL_TEXTURE0); glBindTexture(GL_TEXTURE_2D, m_shadowMap);
    renderAll(renderAll, root, m_shaderMain, false);
}

void Renderer::lookAt(const Vector3& eye, const Vector3& center, const Vector3& up) {
    Vector3 f = (center - eye).normalized();
    Vector3 s = f.cross(up).normalized();
    Vector3 u = s.cross(f);
    float M[16] = { s.x, u.x, -f.x, 0.0f, s.y, u.y, -f.y, 0.0f, s.z, u.z, -f.z, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f };
    glMultMatrixf(M); glTranslatef(-eye.x, -eye.y, -eye.z);
}

void Renderer::setupProjection() {
    glMatrixMode(GL_PROJECTION); glLoadIdentity();
    float aspect = (float)m_width / (float)m_height;
    float f = 1.0f / tan((m_camera.fov * 3.14159f / 180.0f) / 2.0f);
    float nf = 1.0f / (m_camera.nearPlane - m_camera.farPlane);
    float proj[16] = { f/aspect,0,0,0, 0,f,0,0, 0,0,(m_camera.farPlane+m_camera.nearPlane)*nf,-1, 0,0,2*m_camera.farPlane*m_camera.nearPlane*nf,0 };
    glLoadMatrixf(proj);
    glMatrixMode(GL_MODELVIEW);
}

void Renderer::createDisplayLists() {
    m_cubeList = glGenLists(1);
    glNewList(m_cubeList, GL_COMPILE);
    glBegin(GL_QUADS);
    float h = 0.5f;
    auto df = [&](float x1, float y1, float z1, float x2, float y2, float z2, float x3, float y3, float z3, float x4, float y4, float z4, float nx, float ny, float nz) {
        glNormal3f(nx, ny, nz); glVertex3f(x1, y1, z1); glVertex3f(x2, y2, z2); glVertex3f(x3, y3, z3); glVertex3f(x4, y4, z4);
    };
    df(-h,-h, h, h,-h, h, h, h, h,-h, h, h, 0, 0, 1); df( h,-h,-h,-h,-h,-h,-h, h,-h, h, h,-h, 0, 0,-1);
    df(-h, h, h, h, h, h, h, h,-h,-h, h,-h, 0, 1, 0); df(-h,-h,-h, h,-h,-h, h,-h, h,-h,-h, h, 0,-1, 0);
    df( h,-h, h, h,-h,-h, h, h,-h, h, h, h, 1, 0, 0); df(-h,-h,-h,-h,-h, h,-h, h, h,-h, h,-h,-1, 0, 0);
    glEnd(); glEndList();

    m_studList = glGenLists(1);
    glNewList(m_studList, GL_COMPILE);
    int segments = 8;
    glBegin(GL_TRIANGLE_FAN);
    glNormal3f(0, 1, 0); glVertex3f(0, 1, 0);
    for (int i = 0; i <= segments; i++) {
        float angle = (float)i / segments * 2.0f * 3.14159f;
        glVertex3f(cos(angle), 1, sin(angle));
    }
    glEnd();
    glBegin(GL_QUAD_STRIP);
    for (int i = 0; i <= segments; i++) {
        float angle = (float)i / segments * 2.0f * 3.14159f;
        float nx = cos(angle), nz = sin(angle);
        glNormal3f(nx, 0, nz); glVertex3f(nx, 0, nz); glVertex3f(nx, 1, nz);
    }
    glEnd(); glEndList();
}

void Renderer::resize(int width, int height) { m_width = width; m_height = height; }
void Renderer::setCamera(const Camera& camera) { m_camera = camera; }
void Renderer::beginFrame() {}
void Renderer::endFrame() { glUseProgram(0); }
void Renderer::renderPart(const BasePart&, Shader&) {}
void Renderer::renderStuds(const Vector3&, const Vector3&, const Vector3&, Shader&) {}

} // namespace CatCube
