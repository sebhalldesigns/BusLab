cmake_minimum_required(VERSION 3.16)

project(BusLab)

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

add_subdirectory(nanokit)

add_executable(BusLab
    src/main.c
)

set_target_properties(BusLab PROPERTIES WIN32_EXECUTABLE $<CONFIG:Release>)

target_link_libraries(BusLab PRIVATE nanokit)
