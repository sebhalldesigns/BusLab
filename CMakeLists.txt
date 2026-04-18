cmake_minimum_required(VERSION 3.16)

project(BusLab)

add_subdirectory(xpa)

add_executable(BusLab
    src/main.c
)

target_link_libraries(BusLab PRIVATE xpa)