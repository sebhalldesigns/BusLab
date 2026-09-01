/***************************************************************
**
** BusLab Source File
**
** File         :  database_writer.h
** Module       :  data/dbc
** Author       :  SH
** License      :  MIT
** Description  :  DBC file writer (port of the C#
**                 BusLab.Data.Dbc.DatabaseWriter).
**
***************************************************************/

#ifndef DATABASE_WRITER_H
#define DATABASE_WRITER_H

#ifdef __cplusplus
extern "C" {
#endif

/***************************************************************
** MARK: INCLUDES
***************************************************************/

#include "can_database.h"

#include <stdbool.h>
#include <stddef.h>

/***************************************************************
** MARK: FUNCTION DEFS
***************************************************************/

/* Serialises the database to a DBC file. Returns true on success; on failure
   returns false and fills the caller's error / detailed_error buffers (each
   NUL-terminated, truncated to capacity). Either out buffer may be NULL. */
bool database_writer_write(const char *file_path, const can_database_t *database,
                           char *error, size_t error_size,
                           char *detailed_error, size_t detailed_error_size);

#ifdef __cplusplus
}
#endif

#endif /* DATABASE_WRITER_H */
