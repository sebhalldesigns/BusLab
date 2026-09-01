/***************************************************************
**
** BusLab Source File
**
** File         :  database_reader.h
** Module       :  data/dbc
** Author       :  SH
** License      :  MIT
** Description  :  DBC file reader (port of the C#
**                 BusLab.Data.Dbc.DatabaseReader).
**
***************************************************************/

#ifndef DATABASE_READER_H
#define DATABASE_READER_H

#ifdef __cplusplus
extern "C" {
#endif

/***************************************************************
** MARK: INCLUDES
***************************************************************/

#include "can_database.h"

#include <stddef.h>

/***************************************************************
** MARK: FUNCTION DEFS
***************************************************************/

/* Reads and parses a DBC file. On success returns a newly allocated database
   (free with can_database_free). On failure returns NULL and fills the caller's
   error / detailed_error buffers (each NUL-terminated, truncated to capacity).
   Either out buffer may be NULL. */
can_database_t *database_reader_read(const char *file_path,
                                     char *error, size_t error_size,
                                     char *detailed_error, size_t detailed_error_size);

#ifdef __cplusplus
}
#endif

#endif /* DATABASE_READER_H */
