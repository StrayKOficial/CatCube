#pragma once

#include <GL/glew.h>
#include <string>
#include <vector>
#include <iostream>
#include <fstream>
#include <sstream>

namespace CatCube {

class Shader {
public:
    Shader();
    ~Shader();

    bool load(const std::string& vertexSource, const std::string& fragmentSource);
    void use();
    
    // Uniform helpers
    void setInt(const std::string& name, int value);
    void setFloat(const std::string& name, float value);
    void setMat4(const std::string& name, const float* matrix);
    void setVec3(const std::string& name, float x, float y, float z);

    GLuint getProgramID() const { return m_programID; }

private:
    GLuint compileShader(GLenum type, const std::string& source);
    GLuint m_programID = 0;
};

} // namespace CatCube
