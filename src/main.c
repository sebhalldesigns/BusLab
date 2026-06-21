/***************************************************************
**
** BusLab Source File
**
** File         :  main.c
** Module       :  root
** Author       :  SH
** Created      :  2026-04-18 (YYYY-MM-DD)
** License      :  MIT
** Description  :  BusLab Root File
**
***************************************************************/

/***************************************************************
** MARK: INCLUDES
***************************************************************/

#include <nanokit.h>
#include <stdio.h>
#include <string.h>

#include "explorer.h"

/***************************************************************
** MARK: CONSTANTS & MACROS
***************************************************************/

/***************************************************************
** MARK: TYPEDEFS
***************************************************************/

/***************************************************************
** MARK: STATIC VARIABLES
***************************************************************/

static nk_window_t window;
static nk_workbench_t workbench;

/***************************************************************
** MARK: STATIC FUNCTION DEFS
***************************************************************/

static void button_pressed(void);
static void app_launched(void);
static void command_callback(const char *command, const char *user_data);

static void file_open_callback(bool accepted, const char **paths, size_t path_count);
static void directory_open_callback(bool accepted, const char *path);

/***************************************************************
** MARK: PUBLIC FUNCTIONS
***************************************************************/

int main(int argc, char **argv)
{
    nk_run_info_t run_info = {
        .launch_callback = app_launched,
        .application_id = "org.buslab.app",
        .command_callback = command_callback
    };

    return nk_run(&run_info, argc, argv);
}

/***************************************************************
** MARK: STATIC FUNCTIONS
***************************************************************/

static void app_launched(void)
{

    static nk_menu_entry_t file_entries[] = {
        { .title = "New File", .shortcut = "Ctrl-N", .command = "file.new" },
        { .title = "New Window", .shortcut = "Ctrl-Shift-N", .command = "window.new" },
        { .is_separator = true },
        { .title = "Open File", .shortcut = "Ctrl-O", .command = "file.open" },
        { .title = "Open Folder", .shortcut = "Ctrl-Shift-O", .command = "workspace.open" },
        { .is_separator = true },
        { .title = "Save", .shortcut = "Ctrl-S", .command = "file.save" },
        { .title = "Save As", .shortcut = "Ctrl-Shift-S", .command = "file.save_as" },
        { .is_separator = true },
        { .title = "Settings", .shortcut = "Ctrl-,", .command = "settings.open" },
        { .is_separator = true },
        { .title = "Exit", .shortcut = "Ctrl-Q", .command = "env.exit" }
    };

    static nk_menu_entry_t help_entries[] = {
        { .title = "About", .command = "env.about" }
    };

    static nk_menu_t menus[] = {
        { .heading = "File", .entries = file_entries, .entries_count = sizeof(file_entries) / sizeof(nk_menu_entry_t)},
        { .heading = "Help", .entries = help_entries, .entries_count = sizeof(help_entries) / sizeof(nk_menu_entry_t)}
    };

    static nk_menubar_create_info_t menubar_create_info;
    menubar_create_info.menus = menus;
    menubar_create_info.menus_count = sizeof(menus) / sizeof(nk_menu_t);

    static nk_workbench_t workbench;

    static nk_workbench_create_info_t workbench_create_info;
    workbench_create_info.app_title = "BusLab";
    workbench_create_info.menubar_create_info = &menubar_create_info;

    nk_workbench_init(&workbench, &workbench_create_info);

    static nk_dock_tab_t explorer_tab;
    explorer_tab.is_tool = true;
    explorer_tab.title = "Explorer";
    nk_dock_add_tab(&workbench.dock, &explorer_tab, DOCK_TAB_LEFT_AREA);

    explorer_init();
    nk_view_add_child(&explorer_tab.view, explorer_get_view());

    static nk_dock_tab_t devices_tab;
    devices_tab.is_tool = true;
    devices_tab.title = "Devices";
    nk_dock_add_tab(&workbench.dock, &devices_tab, DOCK_TAB_LEFT_AREA);

    static nk_dock_tab_t symbols_tab;
    symbols_tab.is_tool = true;
    symbols_tab.title = "Symbols";
    nk_dock_add_tab(&workbench.dock, &symbols_tab, DOCK_TAB_LEFT_AREA);


    static nk_dock_tab_t properties_tab;
    properties_tab.is_tool = true;
    properties_tab.title = "Properties";
    nk_dock_add_tab(&workbench.dock, &properties_tab, DOCK_TAB_RIGHT_AREA);

    static nk_dock_tab_t problems_tab;
    problems_tab.is_tool = true;
    problems_tab.title = "Problems";
    nk_dock_add_tab(&workbench.dock, &problems_tab, DOCK_TAB_BOTTOM_AREA);

    static nk_dock_tab_t welcome_tab;
    welcome_tab.title = "Welcome";
    nk_dock_add_tab(&workbench.dock, &welcome_tab, DOCK_TAB_MAIN_AREA);
    welcome_tab.view.background_resource = NKRES_COLOR_RED;

    static nk_label_t label;
    label.view.type = NK_LABEL;
    label.text = "Welcome to BusLab!";
    label.text_info.size = 24.0f;
    label.text_info.variant = NK_TEXT_NORMAL;
    label.text_info.color_resource = NKRES_COLOR_TEXT_PRIMARY;
    nk_view_add_child(&welcome_tab.view, &label.view);

    static nk_dock_tab_t doc_tab_1;
    doc_tab_1.title = "example1.dbc";
    nk_dock_add_tab(&workbench.dock, &doc_tab_1, DOCK_TAB_MAIN_AREA);



    static nk_dock_tab_t doc_tab_2;
    doc_tab_2.title = "example2.dbc";
    nk_dock_add_tab(&workbench.dock, &doc_tab_2, DOCK_TAB_MAIN_AREA);

    static nk_dock_tab_t doc_tab_3;
    doc_tab_3.title = "example3.dbc";
    nk_dock_add_tab(&workbench.dock, &doc_tab_3, DOCK_TAB_MAIN_AREA);

    /* Create window */

    nk_window_create_info_t window_info = {
        .title = "BusLab",
        .start_width = 1024,
        .start_height = 640,
        .min_height = 480,
        .min_width = 640,
        .root = &workbench.view
    };

    if (!nk_window_create(&window_info, &window))
    {
        fprintf(stderr, "Failed to create window!\n");
    }
}


static void button_pressed(void)
{
    printf("Button was pressed!\n");
}

static void command_callback(const char *command, const char *user_data)
{

    if (strcmp(command, "file.new") == 0)
    {
        printf("New file command received.\n");
    }
    else if (strcmp(command, "file.open") == 0)
    {
        printf("Open file command received.\n");
        nk_io_open_files(true, file_open_callback);
    }
    else if (strcmp(command, "file.save") == 0)
    {
        printf("Save file command received.\n");
    }
    else if (strcmp(command, "file.save_as") == 0)
    {
        printf("Save As command received.\n");
    }
    else if (strcmp(command, "workspace.open") == 0)
    {
        printf("Open workspace command received.\n");
        nk_io_open_directory(directory_open_callback);
    }
    else if (strcmp(command, "env.exit") == 0)
    {
        printf("Exit command received.\n");
        exit(0);
    }
    else if (strcmp(command, "env.about") == 0)
    {
        printf("About command received.\n");
    }
    else
    {
        printf("Unknown command received: %s\n", command);
    }

}

static void file_open_callback(bool accepted, const char **paths, size_t path_count)
{
    if (accepted && path_count > 0)
    {
        printf("Selected file: %s\n", paths[0]);
    }
    else
    {
        printf("File selection canceled.\n");
    }
}

static void directory_open_callback(bool accepted, const char *path)
{
    if (accepted && path)
    {
        printf("Selected directory: %s\n", path);
        explorer_load_directory(path);
        nk_window_request_redraw(&window);
    }
    else
    {
        printf("Directory selection canceled.\n");
    }
}
