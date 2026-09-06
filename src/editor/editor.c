/***************************************************************
**
** BusLab Source File
**
** File         :  editor.c
** Module       :  editor
** Author       :  SH
** Created      :  2026-08-31 (YYYY-MM-DD)
** License      :  MIT
** Description  :  BusLab Document Tab Implementation
**
***************************************************************/

/***************************************************************
** MARK: INCLUDES
***************************************************************/

#include "editor.h"

#include "dbc_editor/dbc_editor.h"

#include <nanokit.h>

#include <ctype.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

/***************************************************************
** MARK: CONSTANTS & MACROS
***************************************************************/

#define EDITOR_MAX_DOCUMENTS (32U)
#define EDITOR_MAX_PATH      (1024U)
#define EDITOR_MAX_TITLE     (256U)

/* Guards against opening something that is not meant to be read as text. */
#define EDITOR_MAX_FILE_BYTES (8U * 1024U * 1024U)
#define EDITOR_MAX_LINES      (200000U)

/* Bytes at the head of a file inspected for NULs before deciding it is text. */
#define EDITOR_SNIFF_BYTES (4096U)

#define EDITOR_TAB_WIDTH (4U)

/* Fixed per-line height, which is what lets the scroll view virtualise. */
#define EDITOR_ROW_HEIGHT (17.0f)

#define EDITOR_PADDING (8.0f)

/***************************************************************
** MARK: TYPEDEFS
***************************************************************/

typedef struct
{
    nk_dock_tab_t    tab;
    nk_scroll_view_t scroll_view;

    char path[EDITOR_MAX_PATH];
    char title[EDITOR_MAX_TITLE];

    /* The file, with every line terminated by a NUL so each nk_label_t can
       point straight into it instead of owning a copy. */
    char      *text;
    nk_label_t *lines;
    size_t      line_count;

    /* Bumped each time the document is focused, so the least recently used
       slot can be identified when the pool is full. */
    unsigned long last_used;

    bool in_use;
} editor_document_t;

/***************************************************************
** MARK: STATIC VARIABLES
***************************************************************/

static nk_dock_t *dock = NULL;

static editor_document_t documents[EDITOR_MAX_DOCUMENTS];

static unsigned long use_counter = 0;

/* Shown in place of the file body when it cannot be displayed. The label points
   at these directly, so they must outlive the document. */
static const char placeholder_empty[]  = "(empty file)";
static const char placeholder_binary[] = "(binary file — not shown)";
static const char placeholder_error[]  = "(could not be read)";

/***************************************************************
** MARK: STATIC FUNCTION DEFS
***************************************************************/

static const char *path_basename(const char *path);
static bool path_has_extension(const char *path, const char *extension);

static void document_release(editor_document_t *document);
static void reclaim_closed_documents(void);
static editor_document_t *document_find(const char *path);
static editor_document_t *document_alloc(void);

static char *read_file_expanded(const char *path, size_t *out_length, bool *out_binary);
static size_t split_lines(char *text, size_t length);
static bool document_build_lines(editor_document_t *document);
static void document_show_placeholder(editor_document_t *document, const char *message);
static void document_focus(editor_document_t *document);

/***************************************************************
** MARK: PUBLIC FUNCTIONS
***************************************************************/

void editor_init(nk_dock_t *target_dock)
{
    dock = target_dock;
    dbc_editor_init(target_dock);
}

void editor_open(const char *path)
{
    if (!path)
    {
        return;
    }

    if (path_has_extension(path, ".dbc"))
    {
        dbc_editor_open(path);
        return;
    }

    if (!dock)
    {
        return;
    }

    /* Tabs the user closed still hold their slot until now — the dock has no
       close callback, so a detached tab view is the signal. */
    reclaim_closed_documents();

    editor_document_t *existing = document_find(path);

    if (existing)
    {
        document_focus(existing);
        return;
    }

    editor_document_t *document = document_alloc();

    if (!document)
    {
        fprintf(stderr, "editor: no free document slot for %s\n", path);
        return;
    }

    snprintf(document->path, sizeof(document->path), "%s", path);
    snprintf(document->title, sizeof(document->title), "%s", path_basename(path));

    nk_scroll_view_init(&document->scroll_view);

    document->scroll_view.view.padding.left   = EDITOR_PADDING;
    document->scroll_view.view.padding.right  = EDITOR_PADDING;
    document->scroll_view.view.padding.top    = EDITOR_PADDING;
    document->scroll_view.view.padding.bottom = EDITOR_PADDING;

    document->scroll_view.virtualize  = true;
    document->scroll_view.item_height = EDITOR_ROW_HEIGHT;

    if (!document_build_lines(document))
    {
        /* build_lines has already installed a placeholder row. */
    }

    document->tab.is_tool = false;
    document->tab.title   = document->title;

    nk_view_add_child(&document->tab.view, &document->scroll_view.view);

    nk_dock_add_tab(dock, &document->tab, DOCK_TAB_MAIN_AREA);

    document_focus(document);
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

/* Case-insensitive suffix check, so "FILE.DBC" routes the same as "file.dbc". */
static bool path_has_extension(const char *path, const char *extension)
{
    size_t path_length = strlen(path);
    size_t extension_length = strlen(extension);

    if (extension_length > path_length)
    {
        return false;
    }

    const char *suffix = path + (path_length - extension_length);

    for (size_t i = 0; i < extension_length; i++)
    {
        if (tolower((unsigned char)suffix[i]) != tolower((unsigned char)extension[i]))
        {
            return false;
        }
    }

    return true;
}

static void document_release(editor_document_t *document)
{
    /* Drop the rows before their backing memory goes away, so nothing in the
       view tree is left pointing at a freed label. */
    nk_view_t *child = document->scroll_view.view.first_child;
    while (child)
    {
        nk_view_t *next = child->next_sibling;
        nk_view_remove(child);
        child = next;
    }

    nk_view_remove(&document->scroll_view.view);

    free(document->lines);
    free(document->text);

    document->lines      = NULL;
    document->text       = NULL;
    document->line_count = 0;
    document->in_use     = false;
}

static void reclaim_closed_documents(void)
{
    for (size_t i = 0; i < EDITOR_MAX_DOCUMENTS; i++)
    {
        editor_document_t *document = &documents[i];

        if (document->in_use && !nk_dock_tab_is_open(&document->tab))
        {
            document_release(document);
        }
    }
}

static editor_document_t *document_find(const char *path)
{
    for (size_t i = 0; i < EDITOR_MAX_DOCUMENTS; i++)
    {
        if (documents[i].in_use && strcmp(documents[i].path, path) == 0)
        {
            return &documents[i];
        }
    }

    return NULL;
}

static editor_document_t *document_alloc(void)
{
    for (size_t i = 0; i < EDITOR_MAX_DOCUMENTS; i++)
    {
        if (!documents[i].in_use)
        {
            memset(&documents[i], 0, sizeof(editor_document_t));
            documents[i].in_use = true;
            return &documents[i];
        }
    }

    /* Pool exhausted: close the document the user has looked at least recently
       rather than refusing to open the file they just clicked. */
    editor_document_t *oldest = &documents[0];

    for (size_t i = 1; i < EDITOR_MAX_DOCUMENTS; i++)
    {
        if (documents[i].last_used < oldest->last_used)
        {
            oldest = &documents[i];
        }
    }

    nk_dock_close_tab(&oldest->tab);
    document_release(oldest);

    memset(oldest, 0, sizeof(editor_document_t));
    oldest->in_use = true;

    return oldest;
}

/* Reads `path` whole, expanding tabs to spaces so rows line up without a
   monospace-aware renderer. Returns NULL on error or if the content is not
   text; *out_binary distinguishes the two. */
static char *read_file_expanded(const char *path, size_t *out_length, bool *out_binary)
{
    *out_length = 0;
    *out_binary = false;

    FILE *file = fopen(path, "rb");

    if (!file)
    {
        return NULL;
    }

    if (fseek(file, 0, SEEK_END) != 0)
    {
        fclose(file);
        return NULL;
    }

    long size = ftell(file);

    if (size < 0 || fseek(file, 0, SEEK_SET) != 0)
    {
        fclose(file);
        return NULL;
    }

    if ((size_t)size > EDITOR_MAX_FILE_BYTES)
    {
        size = (long)EDITOR_MAX_FILE_BYTES;
    }

    char *raw = (char *)malloc((size_t)size + 1);

    if (!raw)
    {
        fclose(file);
        return NULL;
    }

    size_t length = fread(raw, 1, (size_t)size, file);
    raw[length] = '\0';

    fclose(file);

    size_t sniff = (length < EDITOR_SNIFF_BYTES) ? length : EDITOR_SNIFF_BYTES;

    for (size_t i = 0; i < sniff; i++)
    {
        if (raw[i] == '\0')
        {
            free(raw);
            *out_binary = true;
            return NULL;
        }
    }

    /* Measure the expansion first so the copy can be allocated exactly. */
    size_t expanded_length = 0;
    size_t column = 0;

    for (size_t i = 0; i < length; i++)
    {
        if (raw[i] == '\t')
        {
            size_t advance = EDITOR_TAB_WIDTH - (column % EDITOR_TAB_WIDTH);
            expanded_length += advance;
            column += advance;
        }
        else if (raw[i] == '\n')
        {
            expanded_length++;
            column = 0;
        }
        else if (raw[i] != '\r')
        {
            expanded_length++;
            column++;
        }
    }

    char *text = (char *)malloc(expanded_length + 1);

    if (!text)
    {
        free(raw);
        return NULL;
    }

    size_t out = 0;
    column = 0;

    for (size_t i = 0; i < length; i++)
    {
        if (raw[i] == '\t')
        {
            size_t advance = EDITOR_TAB_WIDTH - (column % EDITOR_TAB_WIDTH);

            for (size_t s = 0; s < advance; s++)
            {
                text[out++] = ' ';
            }

            column += advance;
        }
        else if (raw[i] == '\n')
        {
            text[out++] = '\n';
            column = 0;
        }
        else if (raw[i] != '\r')
        {
            text[out++] = raw[i];
            column++;
        }
    }

    text[out] = '\0';

    free(raw);

    *out_length = out;

    return text;
}

/* Turns newlines into terminators in place and returns the line count. */
static size_t split_lines(char *text, size_t length)
{
    size_t count = 0;

    for (size_t i = 0; i < length; i++)
    {
        if (text[i] == '\n')
        {
            text[i] = '\0';
            count++;
        }
    }

    /* A file not ending in a newline still has a final line. */
    if (length > 0 && text[length - 1] != '\0')
    {
        count++;
    }

    return count;
}

static void document_show_placeholder(editor_document_t *document, const char *message)
{
    document->lines = (nk_label_t *)calloc(1, sizeof(nk_label_t));

    if (!document->lines)
    {
        return;
    }

    document->line_count = 1;

    nk_label_t *label = &document->lines[0];

    label->view.type          = NK_LABEL;
    label->view.height.type   = NK_SIZING_FIXED;
    label->view.height.value  = EDITOR_ROW_HEIGHT;
    label->text               = message;
    label->text_info.size     = 12.0f;
    label->text_info.variant  = NK_TEXT_NORMAL;
    label->text_info.color_resource = NKRES_COLOR_TEXT_SECONDARY;
    label->no_wrap            = true;

    nk_view_add_child(&document->scroll_view.view, &label->view);
}

static bool document_build_lines(editor_document_t *document)
{
    size_t length = 0;
    bool is_binary = false;

    document->text = read_file_expanded(document->path, &length, &is_binary);

    if (!document->text)
    {
        document_show_placeholder(document,
            is_binary ? placeholder_binary : placeholder_error);
        return false;
    }

    if (length == 0)
    {
        document_show_placeholder(document, placeholder_empty);
        return true;
    }

    size_t count = split_lines(document->text, length);

    if (count > EDITOR_MAX_LINES)
    {
        count = EDITOR_MAX_LINES;
    }

    document->lines = (nk_label_t *)calloc(count, sizeof(nk_label_t));

    if (!document->lines)
    {
        free(document->text);
        document->text = NULL;
        document_show_placeholder(document, placeholder_error);
        return false;
    }

    document->line_count = count;

    const char *cursor = document->text;

    for (size_t i = 0; i < count; i++)
    {
        nk_label_t *label = &document->lines[i];

        label->view.type          = NK_LABEL;
        label->view.height.type   = NK_SIZING_FIXED;
        label->view.height.value  = EDITOR_ROW_HEIGHT;
        label->text               = cursor;
        label->text_info.size     = 12.0f;
        label->text_info.variant  = NK_TEXT_NORMAL;
        label->text_info.color_resource = NKRES_COLOR_TEXT_PRIMARY;
        label->no_wrap            = true;

        nk_view_add_child(&document->scroll_view.view, &label->view);

        cursor += strlen(cursor) + 1;
    }

    return true;
}

static void document_focus(editor_document_t *document)
{
    document->last_used = ++use_counter;

    nk_dock_focus_tab(&document->tab);
}
