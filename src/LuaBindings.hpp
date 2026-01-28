#pragma once

#include "ScriptService.hpp"
#include "Instance.hpp"
#include "Part.hpp"
#include <memory>

namespace CatCube {

// Simple Lua Binding Helper
class LuaBindings {
public:
    static void registerBindings(lua_State* L, std::shared_ptr<Instance> game) {
        registerInstance(L);
        registerVector3(L);
        registerColor3(L);
        
        if (game) {
            pushInstance(L, game);
            lua_setglobal(L, "game");
        }
    }
    
private:
    static int getService(lua_State* L) {
        auto inst = checkInstance(L, 1);
        const char* name = luaL_checkstring(L, 2);
        auto service = inst->findFirstChild(name);
        pushInstance(L, service);
        return 1;
    }

    static void registerVector3(lua_State* L) {
        lua_newtable(L);
        lua_pushstring(L, "new");
        lua_pushcfunction(L, [](lua_State* L) -> int {
            float x = (float)luaL_optnumber(L, 1, 0);
            float y = (float)luaL_optnumber(L, 2, 0);
            float z = (float)luaL_optnumber(L, 3, 0);
            // Push as a simple table for now to keep it fast
            lua_newtable(L);
            lua_pushnumber(L, x); lua_setfield(L, -2, "x");
            lua_pushnumber(L, y); lua_setfield(L, -2, "y");
            lua_pushnumber(L, z); lua_setfield(L, -2, "z");
            return 1;
        });
        lua_settable(L, -3);
        lua_setglobal(L, "Vector3");
    }

    static void registerColor3(lua_State* L) {
        lua_newtable(L);
        lua_pushstring(L, "new");
        lua_pushcfunction(L, [](lua_State* L) -> int {
            float r = (float)luaL_optnumber(L, 1, 0);
            float g = (float)luaL_optnumber(L, 2, 0);
            float b = (float)luaL_optnumber(L, 3, 0);
            lua_newtable(L);
            lua_pushnumber(L, r); lua_setfield(L, -2, "r");
            lua_pushnumber(L, g); lua_setfield(L, -2, "g");
            lua_pushnumber(L, b); lua_setfield(L, -2, "b");
            return 1;
        });
        lua_settable(L, -3);
        lua_setglobal(L, "Color3");
    }

    // == Instance Binding ==
    static void registerInstance(lua_State* L) {
        // Metatable for Instance
        luaL_newmetatable(L, "CatCube.Instance");
        
        // __index
        lua_pushstring(L, "__index");
        lua_pushcfunction(L, instanceIndex);
        lua_settable(L, -3);
        
        // __newindex (property set)
        lua_pushstring(L, "__newindex");
        lua_pushcfunction(L, instanceNewIndex);
        lua_settable(L, -3);
        
        lua_pop(L, 1); // Pop metatable
        
        // Global "Instance" table
        lua_newtable(L);
        
        // Instance.new
        lua_pushstring(L, "new");
        lua_pushcfunction(L, instanceNew);
        lua_settable(L, -3);
        
        lua_setglobal(L, "Instance");
    }
    
    static int instanceNew(lua_State* L) {
        const char* className = luaL_checkstring(L, 1);
        
        std::shared_ptr<Instance> inst;
        if (std::string(className) == "Part") {
            inst = std::make_shared<Part>();
        } else {
            inst = std::make_shared<Instance>(className);
        }
        
        // Push UserData
        pushInstance(L, inst);
        return 1;
    }
    
    // Functions to push/check instances
    public:
    static void pushInstance(lua_State* L, std::shared_ptr<Instance> inst) {
        if (!inst) {
            lua_pushnil(L);
            return;
        }
        
        // Allocate userdata pointing to shared_ptr
        void* ud = lua_newuserdata(L, sizeof(std::shared_ptr<Instance>));
        new(ud) std::shared_ptr<Instance>(inst); // Placement new
        
        // Set metatable
        luaL_getmetatable(L, "CatCube.Instance");
        lua_setmetatable(L, -2);
    }
    
    static std::shared_ptr<Instance> checkInstance(lua_State* L, int index) {
        void* ud = luaL_checkudata(L, index, "CatCube.Instance");
        return *static_cast<std::shared_ptr<Instance>*>(ud);
    }
    
    private:
    static int instanceIndex(lua_State* L) {
        auto inst = checkInstance(L, 1);
        const char* key = luaL_checkstring(L, 2);
        std::string k(key);
        
        if (k == "Name") {
            lua_pushstring(L, inst->getName().c_str());
            return 1;
        }
        else if (k == "Parent") {
            pushInstance(L, inst->getParent());
            return 1;
        }
        else if (k == "GetService" && inst->getClassName() == "DataModel") {
            lua_pushcfunction(L, getService);
            return 1;
        }
        
        if (auto p = std::dynamic_pointer_cast<Part>(inst)) {
            if (k == "Position") {
                Vector3 pos = p->getPosition();
                lua_newtable(L);
                lua_pushnumber(L, pos.x); lua_setfield(L, -2, "x");
                lua_pushnumber(L, pos.y); lua_setfield(L, -2, "y");
                lua_pushnumber(L, pos.z); lua_setfield(L, -2, "z");
                return 1;
            } else if (k == "Size") {
                Vector3 sz = p->getSize();
                lua_newtable(L);
                lua_pushnumber(L, sz.x); lua_setfield(L, -2, "x");
                lua_pushnumber(L, sz.y); lua_setfield(L, -2, "y");
                lua_pushnumber(L, sz.z); lua_setfield(L, -2, "z");
                return 1;
            } else if (k == "Anchored") {
                lua_pushboolean(L, p->isAnchored());
                return 1;
            }
        }
        
        lua_pushnil(L);
        return 1;
    }
    
    static int instanceNewIndex(lua_State* L) {
        auto inst = checkInstance(L, 1);
        const char* key = luaL_checkstring(L, 2);
        std::string k(key);
        
        if (k == "Name") {
            inst->setName(luaL_checkstring(L, 3));
        }
        else if (k == "Parent") {
            if (lua_isnil(L, 3)) inst->setParent(nullptr);
            else inst->setParent(checkInstance(L, 3));
        }
        else if (auto p = std::dynamic_pointer_cast<Part>(inst)) {
            if (k == "Position" && lua_istable(L, 3)) {
                lua_getfield(L, 3, "x"); float x = (float)lua_tonumber(L, -1);
                lua_getfield(L, 3, "y"); float y = (float)lua_tonumber(L, -1);
                lua_getfield(L, 3, "z"); float z = (float)lua_tonumber(L, -1);
                p->setPosition({x, y, z});
                lua_pop(L, 3);
            } else if (k == "Size" && lua_istable(L, 3)) {
                lua_getfield(L, 3, "x"); float x = (float)lua_tonumber(L, -1);
                lua_getfield(L, 3, "y"); float y = (float)lua_tonumber(L, -1);
                lua_getfield(L, 3, "z"); float z = (float)lua_tonumber(L, -1);
                p->setSize({x, y, z});
                lua_pop(L, 3);
            } else if (k == "Anchored") {
                p->setAnchored(lua_toboolean(L, 3));
            } else if (k == "Color" && lua_istable(L, 3)) {
                lua_getfield(L, 3, "r"); float r = (float)lua_tonumber(L, -1);
                lua_getfield(L, 3, "g"); float g = (float)lua_tonumber(L, -1);
                lua_getfield(L, 3, "b"); float b = (float)lua_tonumber(L, -1);
                p->setColor({r, g, b});
                lua_pop(L, 3);
            }
        }
        
        return 0;
    }
    
    static void registerPart(lua_State*) {
        // Merged into RegisterInstance for simplicity in this MVP
    }
};

} // namespace CatCube
