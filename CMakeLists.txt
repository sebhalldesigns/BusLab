cmake_minimum_required(VERSION 3.16)

project(BusLab)

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

add_subdirectory(nanokit)

add_executable(BusLab
    src/main.c
    src/explorer.c
    src/data/dbc/can_database.c
    src/data/dbc/database_reader.c
    src/data/dbc/database_writer.c
    src/editor.c
)

target_include_directories(BusLab PRIVATE src)

set_target_properties(BusLab PROPERTIES WIN32_EXECUTABLE $<CONFIG:Release>)

target_link_libraries(BusLab PRIVATE nanokit)
