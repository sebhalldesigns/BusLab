/***************************************************************
**
** BusLab Source File
**
** File         :  explorer.c
** Module       :  explorer
** Author       :  SH
** Created      :  2026-06-14 (YYYY-MM-DD)
** License      :  MIT
** Description  :  BusLab File Explorer Implementation
**
***************************************************************/

/***************************************************************
** MARK: INCLUDES
***************************************************************/

#include "explorer.h"

#include <nanokit.h>

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifndef _WIN32
#include <dirent.h>
#include <strings.h>
#include <sys/stat.h>
#endif

/***************************************************************
** MARK: CONSTANTS & MACROS
***************************************************************/

#define EXPLORER_MAX_ENTRIES (1024U)
#define EXPLORER_MAX_PATH    (1024U)
#define EXPLORER_MAX_NAME    (256U)

/* Must match the fixed row height used by the tree item control. */
#define EXPLORER_ROW_HEIGHT  (22.0f)

/***************************************************************
** MARK: TYPEDEFS
***************************************************************/

typedef struct
{
    nk_tree_item_t item; /* must be first — items are cast back to this struct */

    char path[EXPLORER_MAX_PATH];
    char name[EXPLORER_MAX_NAME];

    bool in_use;
} explorer_entry_t;

typedef struct
{
    char name[EXPLORER_MAX_NAME];
    bool is_dir;
} explorer_listing_t;

/***************************************************************
** MARK: STATIC VARIABLES
***************************************************************/

static nk_scroll_view_t scroll_view;

static explorer_entry_t entries[EXPLORER_MAX_ENTRIES];
static explorer_listing_t listing[EXPLORER_MAX_ENTRIES];

static explorer_entry_t *selected = NULL;

static explorer_file_callback_t file_callback = NULL;

/***************************************************************
** MARK: STATIC FUNCTION DEFS
***************************************************************/

static void item_pressed(nk_tree_item_t *item);

static explorer_entry_t *entry_alloc(void);
static int listing_compare(const void *a, const void *b);
static int list_directory(const char *dir_path);

static void explorer_clear(void);
static void explorer_populate(const char *dir_path, int depth, nk_view_t *after);
static void explorer_expand(explorer_entry_t *entry);
static void explorer_collapse(explorer_entry_t *entry);

/***************************************************************
** MARK: PUBLIC FUNCTIONS
***************************************************************/

void explorer_init(void)
{
    nk_scroll_view_init(&scroll_view);

    scroll_view.view.padding.top = 4;
    scroll_view.view.padding.bottom = 4;

    scroll_view.virtualize = true;
    scroll_view.item_height = EXPLORER_ROW_HEIGHT;
}

void explorer_set_file_callback(explorer_file_callback_t callback)
{
    file_callback = callback;
}

nk_view_t *explorer_get_view(void)
{
    return &scroll_view.view;
}

void explorer_load_directory(const char *path)
{
    explorer_clear();
    explorer_populate(path, 0, NULL);
}

/***************************************************************
** MARK: STATIC FUNCTIONS
***************************************************************/

static void item_pressed(nk_tree_item_t *item)
{
    explorer_entry_t *entry = (explorer_entry_t *)item->user_data;

    if (selected && selected != entry)
    {
        selected->item.selected = false;
    }
    entry->item.selected = true;
    selected = entry;

    if (item->is_folder)
    {
        if (item->is_expanded)
        {
            explorer_expand(entry);
        }
        else
        {
            explorer_collapse(entry);
        }
    }
    else if (file_callback)
    {
        file_callback(entry->path);
    }
}

static explorer_entry_t *entry_alloc(void)
{
    for (size_t i = 0; i < EXPLORER_MAX_ENTRIES; i++)
    {
        if (!entries[i].in_use)
        {
            entries[i].in_use = true;
            return &entries[i];
        }
    }

    return NULL;
}

static int listing_compare(const void *a, const void *b)
{
    const explorer_listing_t *la = (const explorer_listing_t *)a;
    const explorer_listing_t *lb = (const explorer_listing_t *)b;

    /* Folders sort before files, then alphabetically (case-insensitive). */
    if (la->is_dir != lb->is_dir)
    {
        return la->is_dir ? -1 : 1;
    }

#ifdef _WIN32
    return _stricmp(la->name, lb->name);
#else
    return strcasecmp(la->name, lb->name);
#endif
}

#ifdef _WIN32

static int list_directory(const char *dir_path)
{
    int count = 0;

    wchar_t pattern[EXPLORER_MAX_PATH];
    int length = MultiByteToWideChar(CP_UTF8, 0, dir_path, -1, pattern, EXPLORER_MAX_PATH);
    if (length <= 0)
    {
        return 0;
    }

    /* length includes the null terminator — overwrite it with "\*". */
    size_t pos = (size_t)length - 1;
    if (pos + 3 >= EXPLORER_MAX_PATH)
    {
        return 0;
    }
    pattern[pos++] = L'\\';
    pattern[pos++] = L'*';
    pattern[pos]   = L'\0';

    WIN32_FIND_DATAW find;
    HANDLE handle = FindFirstFileW(pattern, &find);
    if (handle == INVALID_HANDLE_VALUE)
    {
        return 0;
    }

    do
    {
        if (wcscmp(find.cFileName, L".") == 0 || wcscmp(find.cFileName, L"..") == 0)
        {
            continue;
        }

        if (count >= (int)EXPLORER_MAX_ENTRIES)
        {
            break;
        }

        explorer_listing_t *entry = &listing[count];

        if (WideCharToMultiByte(CP_UTF8, 0, find.cFileName, -1,
                                entry->name, EXPLORER_MAX_NAME, NULL, NULL) <= 0)
        {
            continue;
        }

        entry->is_dir = (find.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0;
        count++;
    }
    while (FindNextFileW(handle, &find));

    FindClose(handle);

    return count;
}

#else

static int list_directory(const char *dir_path)
{
    int count = 0;

    DIR *dir = opendir(dir_path);
    if (!dir)
    {
        return 0;
    }

    struct dirent *de;
    while ((de = readdir(dir)) != NULL)
    {
        if (strcmp(de->d_name, ".") == 0 || strcmp(de->d_name, "..") == 0)
        {
            continue;
        }

        if (count >= (int)EXPLORER_MAX_ENTRIES)
        {
            break;
        }

        explorer_listing_t *entry = &listing[count];
        snprintf(entry->name, sizeof(entry->name), "%s", de->d_name);

        char full[EXPLORER_MAX_PATH];
        snprintf(full, sizeof(full), "%s/%s", dir_path, de->d_name);

        struct stat st;
        entry->is_dir = (stat(full, &st) == 0) && S_ISDIR(st.st_mode);
        count++;
    }

    closedir(dir);

    return count;
}

#endif

static void explorer_clear(void)
{
    nk_view_t *child = scroll_view.view.first_child;
    while (child)
    {
        nk_view_t *next = child->next_sibling;
        nk_view_remove(child);
        child = next;
    }

    for (size_t i = 0; i < EXPLORER_MAX_ENTRIES; i++)
    {
        entries[i].in_use = false;
    }

    selected = NULL;
}

static void explorer_populate(const char *dir_path, int depth, nk_view_t *after)
{
    int count = list_directory(dir_path);
    qsort(listing, (size_t)count, sizeof(explorer_listing_t), listing_compare);

    for (int i = 0; i < count; i++)
    {
        explorer_entry_t *entry = entry_alloc();
        if (!entry)
        {
            break;
        }

        snprintf(entry->name, sizeof(entry->name), "%s", listing[i].name);
        snprintf(entry->path, sizeof(entry->path), "%s/%s", dir_path, listing[i].name);

        nk_tree_item_init(&entry->item, item_pressed);
        entry->item.text = entry->name;
        entry->item.depth = depth;
        entry->item.is_folder = listing[i].is_dir;
        entry->item.user_data = entry;

        if (after)
        {
            nk_view_insert_after(after, &entry->item.view);
            after = &entry->item.view;
        }
        else
        {
            nk_view_add_child(&scroll_view.view, &entry->item.view);
        }
    }
}

static void explorer_expand(explorer_entry_t *entry)
{
    /* Skip if the folder is already expanded — a deeper item directly follows. */
    nk_view_t *next = entry->item.view.next_sibling;
    if (next && ((nk_tree_item_t *)next)->depth > entry->item.depth)
    {
        return;
    }

    explorer_populate(entry->path, entry->item.depth + 1, &entry->item.view);
}

static void explorer_collapse(explorer_entry_t *entry)
{
    /* Remove every following item that sits deeper than the folder — this
       drops the whole subtree, including any expanded descendants. */
    nk_view_t *child = entry->item.view.next_sibling;
    while (child && ((nk_tree_item_t *)child)->depth > entry->item.depth)
    {
        nk_view_t *next = child->next_sibling;

        explorer_entry_t *child_entry = (explorer_entry_t *)((nk_tree_item_t *)child)->user_data;
        if (child_entry == selected)
        {
            selected = NULL;
        }

        nk_view_remove(child);
        child_entry->in_use = false;

        child = next;
    }
}
