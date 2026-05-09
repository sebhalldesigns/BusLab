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

#define NANOKIT_MAIN
#include <nanokit.h>
#include <string.h>

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

/***************************************************************
** MARK: STATIC FUNCTION DEFS
***************************************************************/

static void button_pressed(void);

/***************************************************************
** MARK: PUBLIC FUNCTIONS
***************************************************************/

void app_launched(void)
{
    /* ---- Labels ---- */
    static nk_label_t title = {
        .view = { .type = NK_LABEL },
        .text = "BusLab",
        .text_info = { .color = {1, 1, 1, 1}, .size = 16 }
    };

    static nk_label_t file_label = {
        .view = { .type = NK_LABEL },
        .text = "No file loaded",
        .text_info = { .color = {0.6f, 0.6f, 0.6f, 1}, .size = 12 }
    };

    static nk_label_t messages_title = {
        .view = { .type = NK_LABEL },
        .text = "Messages",
        .text_info = { .color = {0, 0, 0, 1}, .size = 14 }
    };

    static nk_label_t signals_title = {
        .view = { .type = NK_LABEL },
        .text = "Signals",
        .text_info = { .color = {0, 0, 0, 1}, .size = 14 }
    };

    static nk_label_t msg1 = {
        .view = { .type = NK_LABEL },
        .text = "0x100  EngineSpeed",
        .text_info = { .color = {0.2f, 0.2f, 0.2f, 1}, .size = 12 }
    };

    static nk_label_t msg2 = {
        .view = { .type = NK_LABEL },
        .text = "0x200  VehicleSpeed",
        .text_info = { .color = {0.2f, 0.2f, 0.2f, 1}, .size = 12 }
    };

    static nk_label_t msg3 = {
        .view = { .type = NK_LABEL },
        .text = "0x310  BrakeStatus",
        .text_info = { .color = {0.2f, 0.2f, 0.2f, 1}, .size = 12 }
    };

    static nk_label_t sig_placeholder = {
        .view = { .type = NK_LABEL },
        .text = "Select a message to view signals",
        .text_info = { .color = {0.5f, 0.5f, 0.5f, 1}, .size = 12 }
    };

    /* ---- Buttons ---- */
    static nk_button_t open_btn = {
        .view = {
            .type = NK_BUTTON,
            .id = "OpenBtn",
            .background = {0.25f, 0.25f, 0.25f, 1},
            .corner_radius = 3,
            .padding = {8, 4, 8, 4}
        },
        .text = "Open DBC",
        .text_info = { .color = {1, 1, 1, 1}, .size = 12 },
        .press_callback = button_pressed
    };

    static nk_button_t connect_btn = {
        .view = {
            .type = NK_BUTTON,
            .id = "ConnectBtn",
            .background = {0.15f, 0.45f, 0.25f, 1},
            .corner_radius = 3,
            .padding = {8, 4, 8, 4}
        },
        .text = "Connect",
        .text_info = { .color = {1, 1, 1, 1}, .size = 12 },
        .press_callback = button_pressed
    };

    /* ---- Layout panels ---- */

    /* Toolbar - dark bar across the top */
    static nk_view_t toolbar = {
        .type = NK_VIEW,
        .direction = NK_DIRECTION_HORIZONTAL,
        .width = { .type = NK_SIZING_GROW },
        .height = { .type = NK_SIZING_FIT },
        .background = {0.15f, 0.15f, 0.15f, 1},
        .padding = {8, 6, 8, 6},
        .gap = 8
    };

    /* Left sidebar - message list */
    static nk_view_t sidebar = {
        .type = NK_VIEW,
        .direction = NK_DIRECTION_VERTICAL,
        .width = { .type = NK_SIZING_FIXED, .value = 220 },
        .height = { .type = NK_SIZING_GROW },
        .background = {0.97f, 0.97f, 0.97f, 1},
        .padding = {10, 10, 10, 10},
        .gap = 6
    };

    /* Right content area */
    static nk_view_t content = {
        .type = NK_VIEW,
        .direction = NK_DIRECTION_VERTICAL,
        .width = { .type = NK_SIZING_GROW },
        .height = { .type = NK_SIZING_GROW },
        .background = {1, 1, 1, 1},
        .padding = {10, 10, 10, 10},
        .gap = 6
    };

    /* Horizontal body split */
    static nk_view_t body = {
        .type = NK_VIEW,
        .direction = NK_DIRECTION_HORIZONTAL,
        .width = { .type = NK_SIZING_GROW },
        .height = { .type = NK_SIZING_GROW }
    };

    /* Root container */
    static nk_view_t root = {
        .type = NK_VIEW,
        .direction = NK_DIRECTION_VERTICAL,
        .width = { .type = NK_SIZING_GROW },
        .height = { .type = NK_SIZING_GROW },
        .background = {0.9f, 0.9f, 0.9f, 1}
    };

    /* ---- Build tree ---- */

    /* Toolbar: title, file status, buttons */
    nk_view_add_child(&toolbar, &title.view);
    nk_view_add_child(&toolbar, &file_label.view);
    nk_view_add_child(&toolbar, &open_btn.view);
    nk_view_add_child(&toolbar, &connect_btn.view);

    /* Sidebar: heading + message entries */
    nk_view_add_child(&sidebar, &messages_title.view);
    nk_view_add_child(&sidebar, &msg1.view);
    nk_view_add_child(&sidebar, &msg2.view);
    nk_view_add_child(&sidebar, &msg3.view);

    /* Content area */
    nk_view_add_child(&content, &signals_title.view);
    nk_view_add_child(&content, &sig_placeholder.view);

    /* Body: sidebar + content */
    nk_view_add_child(&body, &sidebar);
    nk_view_add_child(&body, &content);

    /* Root: toolbar + body */
    nk_view_add_child(&root, &toolbar);
    nk_view_add_child(&root, &body);

    /* Create window */
    nk_window_create_info_t window_info = {
        .title = "BusLab",
        .start_width = 1024,
        .start_height = 640,
        .root = &root
    };

    if (!nk_window_create(&window_info, &window))
    {
        fprintf(stderr, "Failed to create window!\n");
    }
}

/***************************************************************
** MARK: STATIC FUNCTIONS
***************************************************************/

static void button_pressed(void)
{
    printf("Button was pressed!\n");
}
