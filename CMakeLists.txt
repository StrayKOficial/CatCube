cmake_minimum_required(VERSION 3.16)
project(CatCube VERSION 0.1.0 LANGUAGES CXX C)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

# Find packages
find_package(PkgConfig REQUIRED)
pkg_check_modules(SDL2 REQUIRED sdl2)
find_package(OpenGL REQUIRED)
find_package(GLEW REQUIRED)

# Fetch Bullet Physics
include(FetchContent)
FetchContent_Declare(
    bullet
    GIT_REPOSITORY https://github.com/bulletphysics/bullet3.git
    GIT_TAG        3.25
)
# Disable Bullet demos and extras to speed up build
set(BUILD_CPU_DEMOS OFF CACHE BOOL "" FORCE)
set(BUILD_OPENGL3_DEMOS OFF CACHE BOOL "" FORCE)
set(BUILD_BULLET2_DEMOS OFF CACHE BOOL "" FORCE)
set(BUILD_EXTRAS OFF CACHE BOOL "" FORCE)
set(BUILD_UNIT_TESTS OFF CACHE BOOL "" FORCE)
set(INSTALL_LIBS ON CACHE BOOL "" FORCE)

FetchContent_MakeAvailable(bullet)

# Lua 5.4 (Manual Build)
FetchContent_Declare(
    lua_official
    URL https://www.lua.org/ftp/lua-5.4.6.tar.gz
)
FetchContent_MakeAvailable(lua_official)

# Collect Lua sources manually
file(GLOB LUA_SRCS "${lua_official_SOURCE_DIR}/src/*.c")
# Filter out standalone interpreter/compiler mains
list(FILTER LUA_SRCS EXCLUDE REGEX "src/lua.c|src/luac.c")

add_library(lua_static STATIC ${LUA_SRCS})
target_include_directories(lua_static PUBLIC ${lua_official_SOURCE_DIR}/src)

# ENet (Manual Build)
FetchContent_Declare(
    enet_official
    URL http://enet.bespin.org/download/enet-1.3.17.tar.gz
)
FetchContent_MakeAvailable(enet_official)

# ENet needs some platform defines
file(GLOB ENET_SRCS "${enet_official_SOURCE_DIR}/*.c")
add_library(enet_static STATIC ${ENET_SRCS})
target_include_directories(enet_static PUBLIC ${enet_official_SOURCE_DIR}/include)
if(UNIX)
    target_compile_definitions(enet_static PRIVATE HAS_GETHOSTBYNAME_R HAS_GETHOSTBYADDR_R HAS_POLL HAS_FCNTL HAS_INET_PTON HAS_INET_NTOP HAS_MSGHDR_FLAGS HAS_SOCKLEN_T)
endif()

# Source files
set(SOURCES
    src/main.cpp
    src/Engine.cpp
    src/Instance.cpp
    src/Part.cpp
    src/Renderer.cpp
    src/PhysicsService.cpp
    src/Humanoid.cpp
    src/ScriptService.cpp
    src/NetworkService.cpp
    src/Shader.cpp
)

# Create executable
add_executable(${PROJECT_NAME} ${SOURCES})

# Include directories
target_include_directories(${PROJECT_NAME} PRIVATE
    ${SDL2_INCLUDE_DIRS}
    ${OPENGL_INCLUDE_DIRS}
    ${GLEW_INCLUDE_DIRS}
    ${bullet_SOURCE_DIR}/src
    src
)

# Link directories
target_link_directories(${PROJECT_NAME} PRIVATE
    ${SDL2_LIBRARY_DIRS}
)

# Link libraries
target_link_libraries(${PROJECT_NAME} PRIVATE
    ${SDL2_LIBRARIES}
    ${OPENGL_LIBRARIES}
    ${GLEW_LIBRARIES}
    GL
    BulletDynamics
    BulletCollision
    LinearMath
    lua_static
    enet_static
)

# Compiler flags
target_compile_options(${PROJECT_NAME} PRIVATE
    ${SDL2_CFLAGS_OTHER}
    -Wall
    -Wextra
)
