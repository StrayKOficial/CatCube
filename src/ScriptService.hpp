#pragma once

// Lua (C API)
extern "C" {
    #include "lua.h"
    #include "lualib.h"
    #include "lauxlib.h"
}

#include <string>
#include <iostream>

namespace CatCube {

class ScriptService {
public:
    ScriptService();
    ~ScriptService();
    
    void init();
    void shutdown();
    
    // Run string
    bool runString(const std::string& script);
    
    // Run file
    bool runFile(const std::string& path);
    
    // Get raw state (for bindings)
    lua_State* getState() { return L; }
    
private:
    lua_State* L = nullptr;
};

} // namespace CatCube
