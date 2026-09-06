/***************************************************************
**
** BusLab Header File
**
** File         :  dbc_editor.h
** Module       :  dbc_editor
** Author       :  SH
** Created      :  2026-09-01 (YYYY-MM-DD)
** License      :  MIT
** Description  :  BusLab DBC Editor Interface Definition
**
***************************************************************/

#ifndef DBC_EDITOR_H
#define DBC_EDITOR_H

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

/***************************************************************
** MARK: FUNCTION DEFS
***************************************************************/

/* Bind the DBC editor to the dock whose main area holds document tabs. Must be
   called before dbc_editor_open(). */
void dbc_editor_init(nk_dock_t *dock);

/* Show the DBC database at `path` in the main area. If a tab for the file is
   already open it is focused; otherwise the file is parsed and a new tab is
   opened for it. */
void dbc_editor_open(const char *path);

#ifdef __cplusplus
}
#endif

#endif /* DBC_EDITOR_H */
