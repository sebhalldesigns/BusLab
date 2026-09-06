/***************************************************************
**
** BusLab Source File
**
** File         :  dbc_editor.c
** Module       :  dbc_editor
** Author       :  SH
** Created      :  2026-09-01 (YYYY-MM-DD)
** License      :  MIT
** Description  :  BusLab DBC Editor Implementation
**
***************************************************************/

/***************************************************************
** MARK: INCLUDES
***************************************************************/

#include "dbc_editor.h"

#include <nanokit.h>

#include "data/dbc/can_database.h"
#include "data/dbc/database_reader.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

/***************************************************************
** MARK: CONSTANTS & MACROS
***************************************************************/

#define DBC_EDITOR_MAX_DOCUMENTS (16U)
#define DBC_EDITOR_MAX_PATH      (1024U)
#define DBC_EDITOR_MAX_TITLE     (256U)
#define DBC_EDITOR_MAX_ERROR     (320U)

#define DBC_TABLE_MAX_COLUMNS      (8U)
#define DBC_TABLE_COLUMN_MIN_WIDTH (40.0f)

/* Must track table_row.c's own row height so the scroll view's virtualisation
   pitch matches what each row actually renders at. */
#define DBC_TABLE_ROW_HEIGHT    (24.0f)
#define DBC_TABLE_HEADER_HEIGHT (26.0f)

#define DBC_TAB_STRIP_HEIGHT    (32.0f)

/* Default height of the messages table before the user drags the splitter,
   and the floor neither pane can be dragged past. */
#define DBC_MESSAGES_TOP_DEFAULT_HEIGHT (220.0f)
#define DBC_MESSAGES_SPLIT_MIN          (80.0f)

/***************************************************************
** MARK: TYPEDEFS
***************************************************************/

typedef struct dbc_document_t dbc_document_t;

typedef enum
{
    DBC_SECTION_MESSAGES,
    DBC_SECTION_SIGNALS,
    DBC_SECTION_NODES,
    DBC_SECTION_COUNT
} dbc_section_t;

typedef struct
{
    const char *title;
    float       width; /* starting width in points; always > 0, see dbc_table_t */
} dbc_column_t;

/* A header + scrollable body of nk_table_row_t. Each column's width lives in
   its header label's own view.width.value — a column's resize handle targets
   that field directly, and column_width_ptrs hands every row a pointer
   straight at it, so header and body always agree on column widths with no
   separate bookkeeping.

   Every column is a fixed width; none of them grows. A growing column would
   have to size itself around its text, and since each body row holds
   different text, the rows would disagree with each other and with the header
   about where every later column starts. The slack is taken up by a trailing
   filler that holds no text and so has no width of its own to insist on —
   header and body carry the same one, which is what keeps them locked
   together. */
typedef struct
{
    /* Two nested scroll views, which is what keeps the table inside its panel
       and the header locked to the rows. `h_scroll` is the outward-facing one:
       it wraps the whole table, so the columns can total more than the panel
       is wide without spilling over the tabs beside it, and the one bar along
       its bottom slides header and rows together. `body` scrolls only
       vertically, and only the rows, so they travel under a header that stays
       put. Neither view scrolls the axis the other owns. */
    nk_scroll_view_t h_scroll;

    nk_view_t        view; /* the table proper: header stacked on body */
    nk_view_t        header;
    nk_label_t       header_cells[DBC_TABLE_MAX_COLUMNS];
    nk_splitter_t    header_splitters[DBC_TABLE_MAX_COLUMNS];
    nk_view_t        header_filler;
    nk_scroll_view_t body;

    const float *column_width_ptrs[DBC_TABLE_MAX_COLUMNS];
    size_t       column_count;
} dbc_table_t;

typedef struct
{
    nk_button_t button; /* must be first — buttons are cast back to this struct */

    dbc_document_t *doc;
    dbc_section_t   section;
} dbc_section_button_t;

typedef struct
{
    nk_table_row_t row; /* must be first — rows are cast back to this struct */

    dbc_document_t          *doc;
    can_database_message_t  *message;

    char id_text[16];
    char length_text[8];
} dbc_message_row_t;

typedef struct
{
    nk_table_row_t row; /* must be first — rows are cast back to this struct */

    char start_bit_text[16];
    char length_text[16];
} dbc_signal_row_t;

struct dbc_document_t
{
    nk_dock_tab_t tab;

    char path[DBC_EDITOR_MAX_PATH];
    char title[DBC_EDITOR_MAX_TITLE];

    can_database_t *database;

    /* Shown instead of the tabbed UI when the file failed to parse. */
    nk_label_t error_label;
    char       error_text[DBC_EDITOR_MAX_ERROR];

    nk_view_t root; /* tab strip + content, or just the error label */

    nk_view_t             tab_strip;
    dbc_section_button_t  section_buttons[DBC_SECTION_COUNT];
    nk_view_t             content; /* holds exactly one active section view */
    dbc_section_t         active_section;

    /* Messages section: messages table on top, signals of the selected
       message below, resizable by a splitter. */
    nk_view_t       messages_section;
    nk_splitter_t   messages_splitter;
    dbc_table_t     messages_table;
    dbc_table_t     message_signals_table;

    dbc_message_row_t *message_rows;
    size_t              message_row_count;

    can_database_message_t *selected_message;
    dbc_signal_row_t        *message_signal_rows;
    size_t                    message_signal_row_count;

    /* Signals section: every signal in the database, flattened. */
    dbc_table_t        signals_table;
    dbc_signal_row_t   *signal_rows;
    size_t               signal_row_count;

    /* Nodes section. */
    dbc_table_t     nodes_table;
    nk_table_row_t *node_rows;
    size_t           node_row_count;

    /* Bumped each time the document is focused, so the least recently used
       slot can be identified when the pool is full. */
    unsigned long last_used;

    bool in_use;
};

/***************************************************************
** MARK: STATIC VARIABLES
***************************************************************/

static nk_dock_t *dock = NULL;

static dbc_document_t documents[DBC_EDITOR_MAX_DOCUMENTS];

static unsigned long use_counter = 0;

/***************************************************************
** MARK: STATIC FUNCTION DEFS
***************************************************************/

static const char *path_basename(const char *path);
static const char *byte_order_text(can_database_signal_byte_order_t byte_order);
static const char *signal_type_text(can_database_signal_type_t type);

static void document_release(dbc_document_t *doc);
static void reclaim_closed_documents(void);
static dbc_document_t *document_find(const char *path);
static dbc_document_t *document_alloc(void);
static void document_focus(dbc_document_t *doc);

static void dbc_table_init(dbc_table_t *table, const dbc_column_t *columns, size_t column_count);
static void dbc_table_clear(dbc_table_t *table);
static void dbc_table_add_row(dbc_table_t *table, nk_table_row_t *row, const char *const *texts);

static void section_button_pressed(nk_button_t *button);
static void update_section_buttons(dbc_document_t *doc);
static void dbc_document_set_section(dbc_document_t *doc, dbc_section_t section);

static void message_row_pressed(nk_table_row_t *row);
static void populate_messages_table(dbc_document_t *doc);
static void populate_message_signals_table(dbc_document_t *doc, can_database_message_t *message);
static void populate_signals_table(dbc_document_t *doc);
static void populate_nodes_table(dbc_document_t *doc);

static void dbc_document_build_ui(dbc_document_t *doc);

/***************************************************************
** MARK: PUBLIC FUNCTIONS
***************************************************************/

void dbc_editor_init(nk_dock_t *target_dock)
{
    dock = target_dock;
}

void dbc_editor_open(const char *path)
{
    if (!dock || !path)
    {
        return;
    }

    /* Tabs the user closed still hold their slot until now — the dock has no
       close callback, so a detached tab view is the signal. */
    reclaim_closed_documents();

    dbc_document_t *existing = document_find(path);

    if (existing)
    {
        document_focus(existing);
        return;
    }

    dbc_document_t *doc = document_alloc();

    if (!doc)
    {
        fprintf(stderr, "dbc_editor: no free document slot for %s\n", path);
        return;
    }

    snprintf(doc->path, sizeof(doc->path), "%s", path);
    snprintf(doc->title, sizeof(doc->title), "%s", path_basename(path));

    doc->root.direction = NK_DIRECTION_VERTICAL;
    doc->root.width.type = NK_SIZING_GROW;
    doc->root.height.type = NK_SIZING_GROW;

    char error[256] = {0};
    doc->database = database_reader_read(path, error, sizeof(error), NULL, 0);

    if (!doc->database)
    {
        snprintf(doc->error_text, sizeof(doc->error_text), "Could not open %s: %s",
            doc->title, error[0] ? error : "unknown error");

        doc->error_label.view.type = NK_LABEL;
        doc->error_label.view.padding.left = 12.0f;
        doc->error_label.view.padding.top = 12.0f;
        doc->error_label.text = doc->error_text;
        doc->error_label.text_info.size = 12.0f;
        doc->error_label.text_info.variant = NK_TEXT_NORMAL;
        doc->error_label.text_info.color_resource = NKRES_COLOR_TEXT_SECONDARY;

        nk_view_add_child(&doc->root, &doc->error_label.view);
    }
    else
    {
        dbc_document_build_ui(doc);

        populate_messages_table(doc);
        populate_signals_table(doc);
        populate_nodes_table(doc);

        dbc_document_set_section(doc, DBC_SECTION_MESSAGES);
    }

    doc->tab.is_tool = false;
    doc->tab.title = doc->title;

    nk_view_add_child(&doc->tab.view, &doc->root);

    nk_dock_add_tab(dock, &doc->tab, DOCK_TAB_MAIN_AREA);

    document_focus(doc);
}

/***************************************************************
** MARK: STATIC FUNCTIONS
***************************************************************/

static const char *path_basename(const char *path)
{
    const char *name = path;

    for (const char *c = path; *c; c++)
    {
        if (*c == '/' || *c == '\\')
        {
            name = c + 1;
        }
    }

    return (*name != '\0') ? name : path;
}

static const char *byte_order_text(can_database_signal_byte_order_t byte_order)
{
    switch (byte_order)
    {
        case CAN_DB_BYTE_ORDER_BIG_ENDIAN:    return "Big Endian";
        case CAN_DB_BYTE_ORDER_LITTLE_ENDIAN: return "Little Endian";
        default:                              return "";
    }
}

static const char *signal_type_text(can_database_signal_type_t type)
{
    switch (type)
    {
        case CAN_DB_SIGNAL_UNSIGNED: return "Unsigned";
        case CAN_DB_SIGNAL_SIGNED:   return "Signed";
        case CAN_DB_SIGNAL_FLOAT:    return "Float";
        case CAN_DB_SIGNAL_DOUBLE:   return "Double";
        default:                     return "";
    }
}

/* --- document pool ------------------------------------------------------ */

static void document_release(dbc_document_t *doc)
{
    dbc_table_clear(&doc->messages_table);
    dbc_table_clear(&doc->message_signals_table);
    dbc_table_clear(&doc->signals_table);
    dbc_table_clear(&doc->nodes_table);

    free(doc->message_rows);
    free(doc->message_signal_rows);
    free(doc->signal_rows);
    free(doc->node_rows);

    can_database_free(doc->database);

    memset(doc, 0, sizeof(dbc_document_t));
}

static void reclaim_closed_documents(void)
{
    for (size_t i = 0; i < DBC_EDITOR_MAX_DOCUMENTS; i++)
    {
        dbc_document_t *doc = &documents[i];

        if (doc->in_use && !nk_dock_tab_is_open(&doc->tab))
        {
            document_release(doc);
        }
    }
}

static dbc_document_t *document_find(const char *path)
{
    for (size_t i = 0; i < DBC_EDITOR_MAX_DOCUMENTS; i++)
    {
        if (documents[i].in_use && strcmp(documents[i].path, path) == 0)
        {
            return &documents[i];
        }
    }

    return NULL;
}

static dbc_document_t *document_alloc(void)
{
    for (size_t i = 0; i < DBC_EDITOR_MAX_DOCUMENTS; i++)
    {
        if (!documents[i].in_use)
        {
            memset(&documents[i], 0, sizeof(dbc_document_t));
            documents[i].in_use = true;
            return &documents[i];
        }
    }

    /* Pool exhausted: close the document the user has looked at least recently
       rather than refusing to open the file they just clicked. */
    dbc_document_t *oldest = &documents[0];

    for (size_t i = 1; i < DBC_EDITOR_MAX_DOCUMENTS; i++)
    {
        if (documents[i].last_used < oldest->last_used)
        {
            oldest = &documents[i];
        }
    }

    nk_dock_close_tab(&oldest->tab);
    document_release(oldest);

    memset(oldest, 0, sizeof(dbc_document_t));
    oldest->in_use = true;

    return oldest;
}

static void document_focus(dbc_document_t *doc)
{
    doc->last_used = ++use_counter;

    nk_dock_focus_tab(&doc->tab);
}

/* --- table helper --------------------------------------------------------- */

static void dbc_table_init(dbc_table_t *table, const dbc_column_t *columns, size_t column_count)
{
    memset(table, 0, sizeof(dbc_table_t));

    nk_scroll_view_init(&table->h_scroll);
    table->h_scroll.axes = NK_SCROLL_AXES_HORIZONTAL;

    table->view.direction   = NK_DIRECTION_VERTICAL;
    table->view.width.type  = NK_SIZING_GROW;
    table->view.height.type = NK_SIZING_GROW;

    nk_view_add_child(&table->h_scroll.view, &table->view);

    table->header.direction           = NK_DIRECTION_HORIZONTAL;
    table->header.width.type          = NK_SIZING_GROW;
    table->header.height.type         = NK_SIZING_FIXED;
    table->header.height.value        = DBC_TABLE_HEADER_HEIGHT;
    table->header.background_resource = NKRES_COLOR_BACKGROUND_SECONDARY;

    table->column_count = (column_count > DBC_TABLE_MAX_COLUMNS) ? DBC_TABLE_MAX_COLUMNS : column_count;

    for (size_t i = 0; i < table->column_count; i++)
    {
        nk_label_t *cell = &table->header_cells[i];

        cell->view.type = NK_LABEL;
        cell->view.width.type  = NK_SIZING_FIXED;
        cell->view.width.value = (columns[i].width > 0.0f)
                               ? columns[i].width : DBC_TABLE_COLUMN_MIN_WIDTH;
        /* Fixed height forces label_render through its padded, vertically
           centred box, so header text lines up with the padding table_row.c
           applies to every body cell. */
        cell->view.height.type  = NK_SIZING_FIXED;
        cell->view.height.value = DBC_TABLE_HEADER_HEIGHT;
        cell->view.padding.left  = 8.0f;
        cell->view.padding.right = 8.0f;

        cell->text = columns[i].title;
        cell->text_info.variant = NK_TEXT_BOLD;
        cell->text_info.size = 12.0f;
        cell->text_info.color_resource = NKRES_COLOR_TEXT_SECONDARY;
        cell->no_wrap = true;

        nk_view_add_child(&table->header, &cell->view);

        /* Every row reads this same address each frame, so dragging a handle
           below (which targets it directly) is instantly visible in the
           body too. */
        table->column_width_ptrs[i] = &cell->view.width.value;
    }

    /* A resize handle sits on each column's right edge and resizes that
       column, and only that column — the trailing filler absorbs the
       difference, so no other column changes width and the handle stays
       under the pointer for the whole drag. The last column gets one too;
       its edge is a real boundary now that the filler sits beyond it. */
    for (size_t i = 0; i < table->column_count; i++)
    {
        nk_splitter_t *handle = &table->header_splitters[i];

        nk_splitter_init(handle, &table->header_cells[i].view.width.value,
            NK_DIRECTION_HORIZONTAL, false);

        handle->min = DBC_TABLE_COLUMN_MIN_WIDTH;

        nk_view_insert_after(&table->header_cells[i].view, &handle->view);
    }

    /* Mirrors the filler table_row.c puts at the end of every row, so the
       header ends the same way the body does. */
    table->header_filler.width.type  = NK_SIZING_GROW;
    table->header_filler.height.type = NK_SIZING_GROW;

    nk_view_add_child(&table->header, &table->header_filler);

    nk_view_add_child(&table->view, &table->header);

    nk_scroll_view_init(&table->body);
    table->body.virtualize = true;
    table->body.item_height = DBC_TABLE_ROW_HEIGHT;

    /* Sideways travel belongs to h_scroll, which carries the header along with
       the rows. Were this view to scroll horizontally too, only the rows would
       move and the two would slide apart. */
    table->body.axes = NK_SCROLL_AXES_VERTICAL;

    /* This view is as wide as the columns, so its own right edge can be well
       past the panel. Hang the vertical bar off h_scroll instead, so it stays
       against the panel edge rather than scrolling away with the content. */
    table->body.bar_anchor = &table->h_scroll;

    nk_view_add_child(&table->view, &table->body.view);
}

static void dbc_table_clear(dbc_table_t *table)
{
    nk_view_t *child = table->body.view.first_child;

    while (child)
    {
        nk_view_t *next = child->next_sibling;
        nk_view_remove(child);
        child = next;
    }
}

static void dbc_table_add_row(dbc_table_t *table, nk_table_row_t *row, const char *const *texts)
{
    row->cell_count = table->column_count;
    row->column_widths = table->column_width_ptrs;

    for (size_t i = 0; i < table->column_count; i++)
    {
        row->cell_text[i] = texts[i];
    }

    nk_view_add_child(&table->body.view, &row->view);
}

/* --- section switching ----------------------------------------------------- */

static void section_button_pressed(nk_button_t *button)
{
    dbc_section_button_t *section_button = (dbc_section_button_t *)button;

    dbc_document_set_section(section_button->doc, section_button->section);
}

static void update_section_buttons(dbc_document_t *doc)
{
    for (size_t i = 0; i < DBC_SECTION_COUNT; i++)
    {
        nk_button_t *button = &doc->section_buttons[i].button;
        bool is_active = ((dbc_section_t)i == doc->active_section);

        button->text_info.variant = is_active ? NK_TEXT_BOLD : NK_TEXT_NORMAL;
        button->text_info.color_resource = is_active ? NKRES_COLOR_TEXT_PRIMARY : NKRES_COLOR_TEXT_SECONDARY;
    }
}

static void dbc_document_set_section(dbc_document_t *doc, dbc_section_t section)
{
    nk_view_t *current = doc->content.first_child;

    if (current)
    {
        nk_view_remove(current);
    }

    doc->active_section = section;

    nk_view_t *next = NULL;

    switch (section)
    {
        case DBC_SECTION_MESSAGES: next = &doc->messages_section;       break;
        case DBC_SECTION_SIGNALS:  next = &doc->signals_table.h_scroll.view; break;
        case DBC_SECTION_NODES:    next = &doc->nodes_table.h_scroll.view;   break;
        default:                                                        break;
    }

    if (next)
    {
        nk_view_add_child(&doc->content, next);
    }

    update_section_buttons(doc);
}

/* --- data population -------------------------------------------------------- */

static void message_row_pressed(nk_table_row_t *row)
{
    dbc_message_row_t *message_row = (dbc_message_row_t *)row;
    dbc_document_t *doc = message_row->doc;

    if (doc->selected_message == message_row->message)
    {
        return;
    }

    for (size_t i = 0; i < doc->message_row_count; i++)
    {
        doc->message_rows[i].row.selected = false;
    }

    row->selected = true;

    doc->selected_message = message_row->message;

    populate_message_signals_table(doc, message_row->message);
}

static void populate_messages_table(dbc_document_t *doc)
{
    dbc_table_clear(&doc->messages_table);
    free(doc->message_rows);
    doc->message_rows = NULL;
    doc->message_row_count = 0;

    size_t count = kv_size(doc->database->messages);

    if (count == 0)
    {
        return;
    }

    doc->message_rows = (dbc_message_row_t *)calloc(count, sizeof(dbc_message_row_t));

    if (!doc->message_rows)
    {
        return;
    }

    doc->message_row_count = count;

    for (size_t i = 0; i < count; i++)
    {
        can_database_message_t *message = DB_ARRAY_AT(doc->database->messages, can_database_message_t, i);
        dbc_message_row_t *message_row = &doc->message_rows[i];

        nk_table_row_init(&message_row->row, message_row_pressed);
        message_row->doc = doc;
        message_row->message = message;

        snprintf(message_row->id_text, sizeof(message_row->id_text), "0x%03X", (unsigned)message->id);
        snprintf(message_row->length_text, sizeof(message_row->length_text), "%u", (unsigned)message->length);

        const char *texts[] = {
            message_row->id_text,
            message_row->length_text,
            db_str(message->name),
            db_str(message->sender_node)
        };

        dbc_table_add_row(&doc->messages_table, &message_row->row, texts);
    }

    /* Select the first message so the signals pane isn't empty on open. */
    message_row_pressed(&doc->message_rows[0].row);
}

static void populate_message_signals_table(dbc_document_t *doc, can_database_message_t *message)
{
    dbc_table_clear(&doc->message_signals_table);
    free(doc->message_signal_rows);
    doc->message_signal_rows = NULL;
    doc->message_signal_row_count = 0;

    if (!message)
    {
        return;
    }

    size_t count = kv_size(message->signals);

    if (count == 0)
    {
        return;
    }

    doc->message_signal_rows = (dbc_signal_row_t *)calloc(count, sizeof(dbc_signal_row_t));

    if (!doc->message_signal_rows)
    {
        return;
    }

    doc->message_signal_row_count = count;

    for (size_t i = 0; i < count; i++)
    {
        can_database_signal_t *signal = DB_ARRAY_AT(message->signals, can_database_signal_t, i);
        dbc_signal_row_t *signal_row = &doc->message_signal_rows[i];

        nk_table_row_init(&signal_row->row, NULL);

        snprintf(signal_row->start_bit_text, sizeof(signal_row->start_bit_text), "%u", (unsigned)signal->start_bit);
        snprintf(signal_row->length_text, sizeof(signal_row->length_text), "%u", (unsigned)signal->bit_length);

        const char *texts[] = {
            db_str(signal->name),
            signal_row->start_bit_text,
            signal_row->length_text,
            byte_order_text(signal->byte_order),
            signal_type_text(signal->signal_type),
            db_str(signal->unit)
        };

        dbc_table_add_row(&doc->message_signals_table, &signal_row->row, texts);
    }
}

static void populate_signals_table(dbc_document_t *doc)
{
    dbc_table_clear(&doc->signals_table);
    free(doc->signal_rows);
    doc->signal_rows = NULL;
    doc->signal_row_count = 0;

    size_t message_count = kv_size(doc->database->messages);
    size_t total = 0;

    for (size_t i = 0; i < message_count; i++)
    {
        can_database_message_t *message = DB_ARRAY_AT(doc->database->messages, can_database_message_t, i);
        total += kv_size(message->signals);
    }

    if (total == 0)
    {
        return;
    }

    doc->signal_rows = (dbc_signal_row_t *)calloc(total, sizeof(dbc_signal_row_t));

    if (!doc->signal_rows)
    {
        return;
    }

    doc->signal_row_count = total;

    size_t row_index = 0;

    for (size_t i = 0; i < message_count; i++)
    {
        can_database_message_t *message = DB_ARRAY_AT(doc->database->messages, can_database_message_t, i);
        size_t signal_count = kv_size(message->signals);

        for (size_t j = 0; j < signal_count; j++)
        {
            can_database_signal_t *signal = DB_ARRAY_AT(message->signals, can_database_signal_t, j);
            dbc_signal_row_t *signal_row = &doc->signal_rows[row_index++];

            nk_table_row_init(&signal_row->row, NULL);

            snprintf(signal_row->start_bit_text, sizeof(signal_row->start_bit_text), "%u", (unsigned)signal->start_bit);
            snprintf(signal_row->length_text, sizeof(signal_row->length_text), "%u", (unsigned)signal->bit_length);

            const char *texts[] = {
                db_str(message->name),
                db_str(signal->name),
                signal_row->start_bit_text,
                signal_row->length_text,
                signal_type_text(signal->signal_type),
                db_str(signal->unit)
            };

            dbc_table_add_row(&doc->signals_table, &signal_row->row, texts);
        }
    }
}

static void populate_nodes_table(dbc_document_t *doc)
{
    dbc_table_clear(&doc->nodes_table);
    free(doc->node_rows);
    doc->node_rows = NULL;
    doc->node_row_count = 0;

    size_t count = kv_size(doc->database->nodes);

    if (count == 0)
    {
        return;
    }

    doc->node_rows = (nk_table_row_t *)calloc(count, sizeof(nk_table_row_t));

    if (!doc->node_rows)
    {
        return;
    }

    doc->node_row_count = count;

    for (size_t i = 0; i < count; i++)
    {
        can_database_node_t *node = DB_ARRAY_AT(doc->database->nodes, can_database_node_t, i);
        nk_table_row_t *node_row = &doc->node_rows[i];

        nk_table_row_init(node_row, NULL);

        const char *texts[] = {
            db_str(node->name),
            db_str(node->comment)
        };

        dbc_table_add_row(&doc->nodes_table, node_row, texts);
    }
}

/* --- ui construction --------------------------------------------------------- */

static void dbc_document_build_ui(dbc_document_t *doc)
{
    static const char *section_titles[DBC_SECTION_COUNT] = { "Messages", "Signals", "Nodes" };

    static const dbc_column_t messages_columns[] = {
        { "ID (Hex)", 90.0f  },
        { "DLC",      60.0f  },
        { "Name",     260.0f },
        { "Sender",   160.0f }
    };

    static const dbc_column_t signal_columns[] = {
        { "Name",       240.0f },
        { "Start Bit",  80.0f  },
        { "Length",     70.0f  },
        { "Byte Order", 110.0f },
        { "Type",       90.0f  },
        { "Unit",       90.0f  }
    };

    static const dbc_column_t all_signal_columns[] = {
        { "Message", 160.0f },
        { "Signal",  220.0f },
        { "Start Bit", 80.0f },
        { "Length",  70.0f  },
        { "Type",    90.0f  },
        { "Unit",    90.0f  }
    };

    static const dbc_column_t node_columns[] = {
        { "Name",    160.0f },
        { "Comment", 360.0f }
    };

    /* Tab strip */

    doc->tab_strip.direction = NK_DIRECTION_HORIZONTAL;
    doc->tab_strip.width.type = NK_SIZING_GROW;
    doc->tab_strip.height.type = NK_SIZING_FIXED;
    doc->tab_strip.height.value = DBC_TAB_STRIP_HEIGHT;
    doc->tab_strip.background_resource = NKRES_COLOR_BACKGROUND_SECONDARY;
    doc->tab_strip.padding.left = 4.0f;
    doc->tab_strip.padding.top = 4.0f;
    doc->tab_strip.gap = 4.0f;
    doc->tab_strip.align_y = NK_ALIGN_CENTER;

    for (size_t i = 0; i < DBC_SECTION_COUNT; i++)
    {
        dbc_section_button_t *section_button = &doc->section_buttons[i];

        section_button->doc = doc;
        section_button->section = (dbc_section_t)i;

        section_button->button.text = section_titles[i];
        nk_button_init(&section_button->button, section_button_pressed);

        nk_view_add_child(&doc->tab_strip, &section_button->button.view);
    }

    nk_view_add_child(&doc->root, &doc->tab_strip);

    /* Content area */

    doc->content.width.type = NK_SIZING_GROW;
    doc->content.height.type = NK_SIZING_GROW;

    nk_view_add_child(&doc->root, &doc->content);

    /* Messages section: table + splitter + table */

    doc->messages_section.direction = NK_DIRECTION_VERTICAL;
    doc->messages_section.width.type = NK_SIZING_GROW;
    doc->messages_section.height.type = NK_SIZING_GROW;

    dbc_table_init(&doc->messages_table, messages_columns,
        sizeof(messages_columns) / sizeof(messages_columns[0]));
    doc->messages_table.h_scroll.view.height.type = NK_SIZING_FIXED;
    doc->messages_table.h_scroll.view.height.value = DBC_MESSAGES_TOP_DEFAULT_HEIGHT;

    nk_splitter_init(&doc->messages_splitter, &doc->messages_table.h_scroll.view.height.value,
        NK_DIRECTION_VERTICAL, false);
    doc->messages_splitter.min = DBC_MESSAGES_SPLIT_MIN;

    dbc_table_init(&doc->message_signals_table, signal_columns,
        sizeof(signal_columns) / sizeof(signal_columns[0]));

    nk_view_add_child(&doc->messages_section, &doc->messages_table.h_scroll.view);
    nk_view_add_child(&doc->messages_section, &doc->messages_splitter.view);
    nk_view_add_child(&doc->messages_section, &doc->message_signals_table.h_scroll.view);

    /* Signals section: one flat table */

    dbc_table_init(&doc->signals_table, all_signal_columns,
        sizeof(all_signal_columns) / sizeof(all_signal_columns[0]));

    /* Nodes section: one flat table */

    dbc_table_init(&doc->nodes_table, node_columns,
        sizeof(node_columns) / sizeof(node_columns[0]));
}
