/***************************************************************
**
** BusLab Source File
**
** File         :  can_database.h
** Module       :  data/dbc
** Author       :  SH
** License      :  MIT
** Description  :  CAN DBC in-memory data model (port of the
**                 C# BusLab.Data.Dbc.CanDatabase types).
**
***************************************************************/

#ifndef CAN_DATABASE_H
#define CAN_DATABASE_H

#ifdef __cplusplus
extern "C" {
#endif

/***************************************************************
** MARK: INCLUDES
***************************************************************/

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#include <kvec.h>   /* klib growable vectors */

/***************************************************************
** MARK: CONSTANTS & MACROS
***************************************************************/

/* Convenience for typed element access into a db_array_t. */
#define DB_ARRAY_AT(arr, type, i) ((type *)kv_A((arr), (i)))

/***************************************************************
** MARK: TYPEDEFS
***************************************************************/

typedef enum
{
    CAN_DB_ATTR_INT,
    CAN_DB_ATTR_FLOAT,
    CAN_DB_ATTR_HEX,
    CAN_DB_ATTR_STRING,
    CAN_DB_ATTR_ENUM
} can_database_attribute_type_t;

typedef enum
{
    CAN_DB_TARGET_NONE,
    CAN_DB_TARGET_NODE,
    CAN_DB_TARGET_SIGNAL,
    CAN_DB_TARGET_MESSAGE
} can_database_attribute_target_t;

typedef enum
{
    CAN_DB_BYTE_ORDER_BIG_ENDIAN,
    CAN_DB_BYTE_ORDER_LITTLE_ENDIAN
} can_database_signal_byte_order_t;

typedef enum
{
    CAN_DB_SIGNAL_UNSIGNED,
    CAN_DB_SIGNAL_SIGNED,
    CAN_DB_SIGNAL_FLOAT,
    CAN_DB_SIGNAL_DOUBLE
} can_database_signal_type_t;

/* Growable array of owned pointers — a klib kvec standing in for C#'s List<T>.
   Element ownership is the array's; freeing is done with a per-element free
   function via db_array_free. Fields are kvec's: .n (count), .m (capacity),
   .a (items); use kv_size/kv_A or the DB_ARRAY_AT helper to access them. */
typedef kvec_t(void *) db_array_t;

/* One key/value entry of a value table — stands in for a Dictionary entry.
   Insertion order is preserved (the writer round-trips it). */
typedef struct
{
    long  key;
    char *value;
} can_database_value_pair_t;

typedef kvec_t(can_database_value_pair_t) can_database_value_pair_vec_t;

typedef struct
{
    char                          *name;
    can_database_value_pair_vec_t  values;   /* klib kvec of value pairs */
} can_database_value_table_t;

typedef struct
{
    char *attribute;
    char *value;
} can_database_attribute_value_t;

typedef struct
{
    char      *name;
    char      *comment;
    db_array_t attribute_values;   /* can_database_attribute_value_t * */
} can_database_node_t;

typedef struct
{
    char                           *name;
    can_database_attribute_type_t   type;
    can_database_attribute_target_t target;

    char      *min;
    char      *max;

    db_array_t enum_values;        /* char * */

    char      *default_value;
} can_database_attribute_t;

typedef struct
{
    char                            *name;

    uint32_t                         start_bit;
    uint32_t                         bit_length;
    can_database_signal_byte_order_t byte_order;

    can_database_signal_type_t       signal_type;

    bool                             is_multiplexor;
    bool                             is_multiplexed;
    char                            *multiplex_value;
    char                            *multiplexor_signal;

    char                            *scale;
    char                            *offset;
    char                            *minimum;
    char                            *maximum;
    char                            *unit;
    char                            *receiver_node;

    char                            *comment;

    db_array_t                       attribute_values;    /* can_database_attribute_value_t * */
    db_array_t                       local_value_tables;  /* can_database_value_table_t *     */
    db_array_t                       global_value_tables; /* char *                           */
} can_database_signal_t;

typedef struct
{
    char      *name;
    uint32_t   id;
    bool       extended;
    uint8_t    length;
    char      *sender_node;

    db_array_t signals;            /* can_database_signal_t * */

    char      *comment;

    db_array_t attribute_values;   /* can_database_attribute_value_t * */
} can_database_message_t;

typedef struct
{
    char                          *name;
    can_database_attribute_type_t  type;
    char                          *min;
    char                          *max;
    char                          *unit;
    char                          *initial_value;
    char                          *ev_id;
    char                          *access_type;
    char                          *node;
} can_database_environment_variable_t;

typedef struct
{
    char      *version;
    db_array_t namespace_symbols;       /* char *                                */
    char      *bus_speed;

    db_array_t nodes;                   /* can_database_node_t *                 */
    db_array_t messages;                /* can_database_message_t *              */

    db_array_t global_comments;         /* char *                               */

    db_array_t attributes;              /* can_database_attribute_t *            */
    db_array_t global_attribute_values; /* can_database_attribute_value_t *      */

    db_array_t global_value_tables;     /* can_database_value_table_t *          */

    db_array_t environment_variables;   /* can_database_environment_variable_t * */
} can_database_t;

/***************************************************************
** MARK: FUNCTION DEFS
***************************************************************/

/* --- string helpers (all char* model fields are NULL-or-heap; NULL == "") --- */

char       *db_strdup(const char *s);
char       *db_strndup(const char *s, size_t n);
void        db_str_set(char **dst, const char *s);
void        db_str_setn(char **dst, const char *s, size_t n);
void        db_str_append(char **dst, const char *s);
const char *db_str(const char *s);   /* NULL-safe view: returns "" for NULL */

/* --- dynamic array --- */

void  db_array_init(db_array_t *array);
void *db_array_add(db_array_t *array, void *item);
void  db_array_free(db_array_t *array, void (*free_item)(void *));

/* --- element constructors (zeroed, arrays initialised, strings NULL) --- */

can_database_node_t            *can_database_node_create(void);
can_database_attribute_t       *can_database_attribute_create(void);
can_database_attribute_value_t *can_database_attribute_value_create(void);
can_database_value_table_t     *can_database_value_table_create(void);
can_database_signal_t          *can_database_signal_create(void);
can_database_message_t         *can_database_message_create(void);
can_database_environment_variable_t *can_database_environment_variable_create(void);

/* Destructors — free an element not (or not yet) owned by a database/array. */
void can_database_node_destroy(can_database_node_t *node);
void can_database_attribute_destroy(can_database_attribute_t *attribute);
void can_database_attribute_value_destroy(can_database_attribute_value_t *value);
void can_database_value_table_destroy(can_database_value_table_t *table);
void can_database_signal_destroy(can_database_signal_t *signal);
void can_database_message_destroy(can_database_message_t *message);

/* Insert or overwrite a key in a value table (preserves insertion order). */
void can_database_value_table_set(can_database_value_table_t *table, long key, const char *value);

/* --- database lifecycle --- */

can_database_t *can_database_create(void);
void            can_database_free(can_database_t *database);

#ifdef __cplusplus
}
#endif

#endif /* CAN_DATABASE_H */
