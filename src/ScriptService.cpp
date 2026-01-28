#include "ScriptService.hpp"

namespace CatCube {

ScriptService::ScriptService() {}

ScriptService::~ScriptService() {
    shutdown();
}

void ScriptService::init() {
    if (L) return;
    
    // Create new Lua state
    L = luaL_newstate();
    
    // Open standard libraries (print, math, string, etc)
    luaL_openlibs(L);
    
    std::cout << "ScriptService initialized (Lua 5.4)" << std::endl;
    
    // Test hook
    runString("print('CatScript Engine Connected')");
}

void ScriptService::shutdown() {
    if (L) {
        lua_close(L);
        L = nullptr;
    }
}

bool ScriptService::runString(const std::string& script) {
    if (!L) return false;
    
    // Load string
    if (luaL_loadstring(L, script.c_str()) != LUA_OK) {
        // Error on stack
        std::cerr << "Lua Syntax Error: " << lua_tostring(L, -1) << std::endl;
        lua_pop(L, 1);
        return false;
    }
    
    // Run (pcall)
    if (lua_pcall(L, 0, 0, 0) != LUA_OK) {
        std::cerr << "Lua Runtime Error: " << lua_tostring(L, -1) << std::endl;
        lua_pop(L, 1);
        return false;
    }
    
    return true;
}

bool ScriptService::runFile(const std::string& path) {
    if (!L) return false;
    
    // Load file
    if (luaL_loadfile(L, path.c_str()) != LUA_OK) {
        std::cerr << "Lua Load Error (" << path << "): " << lua_tostring(L, -1) << std::endl;
        lua_pop(L, 1);
        return false;
    }
    
    // Run (pcall)
    if (lua_pcall(L, 0, 0, 0) != LUA_OK) {
        std::cerr << "Lua Runtime Error (" << path << "): " << lua_tostring(L, -1) << std::endl;
        lua_pop(L, 1);
        return false;
    }
    
    return true;
}

} // namespace CatCube
