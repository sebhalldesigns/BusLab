/***************************************************************
**
** BusLab Header File
**
** File         :  explorer.h
** Module       :  explorer
** Author       :  SH
** Created      :  2026-06-14 (YYYY-MM-DD)
** License      :  MIT
** Description  :  BusLab File Explorer Interface Definition
**
***************************************************************/

#ifndef EXPLORER_H
#define EXPLORER_H

#ifdef __cplusplus
extern "C" {
#endif

/***************************************************************
** MARK: INCLUDES
***************************************************************/

#include <nanokit.h>

/***************************************************************
** MARK: CONSTANTS & MACROS
***************************************************************/

/***************************************************************
** MARK: TYPEDEFS
***************************************************************/

/* Invoked when a file (never a folder) is clicked in the explorer. The path is
   owned by the explorer and stays valid until the entry is collapsed away. */
typedef void (*explorer_file_callback_t)(const char *path);

/***************************************************************
** MARK: FUNCTION DEFS
***************************************************************/

/* Initialise the explorer. Must be called before explorer_get_view(). */
void explorer_init(void);

/* Set the handler invoked when a file is clicked. */
void explorer_set_file_callback(explorer_file_callback_t callback);

/* The explorer root view, to be added as a child of a dock tab. */
nk_view_t *explorer_get_view(void);

/* Populate the explorer with the contents of the given directory. */
void explorer_load_directory(const char *path);

#ifdef __cplusplus
}
#endif

#endif /* EXPLORER_H */
