/***************************************************************
**
** BusLab Header File
**
** File         :  editor.h
** Module       :  editor
** Author       :  SH
** Created      :  2026-08-31 (YYYY-MM-DD)
** License      :  MIT
** Description  :  BusLab Document Tab Interface Definition
**
***************************************************************/

#ifndef EDITOR_H
#define EDITOR_H

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

/* Bind the editor to the dock whose main area holds document tabs. Must be
   called before editor_open(). */
void editor_init(nk_dock_t *dock);

/* Show `path` in the main area. If a tab for the file is already open it is
   focused; otherwise the file is read and a new tab is opened for it. */
void editor_open(const char *path);

#ifdef __cplusplus
}
#endif

#endif /* EDITOR_H */
