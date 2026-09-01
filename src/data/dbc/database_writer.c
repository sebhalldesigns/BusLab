/***************************************************************
**
** BusLab Source File
**
** File         :  database_writer.c
** Module       :  data/dbc
** Author       :  SH
** License      :  MIT
** Description  :  DBC file writer implementation. Emits each
**                 section in the same order and format as the
**                 C# DatabaseWriter.
**
**                 Lines are terminated with '\n' (the C# original
**                 used Environment.NewLine); the reader accepts
**                 both '\n' and '\r\n'.
**
***************************************************************/

/***************************************************************
** MARK: INCLUDES
***************************************************************/

#include "database_writer.h"

#include <kstring.h>   /* klib growable string (kputs / ksprintf) */
#include <kvec.h>      /* klib growable vector                    */

#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

/***************************************************************
** MARK: TYPEDEFS
***************************************************************/

/* The accumulated output, one owned C string per line. */
typedef kvec_t(char *) line_vec_t;

/***************************************************************
** MARK: STATIC FUNCTION DEFS
***************************************************************/

static void write_version(line_vec_t *lines, const can_database_t *db);
static void write_namespace_symbols(line_vec_t *lines, const can_database_t *db);
static void write_bus_speed(line_vec_t *lines, const can_database_t *db);
static void write_nodes(line_vec_t *lines, const can_database_t *db);
static void write_messages(line_vec_t *lines, const can_database_t *db);
static void write_environment_variables(line_vec_t *lines, const can_database_t *db);
static void write_comments(line_vec_t *lines, const can_database_t *db);
static void write_attribute_definitions(line_vec_t *lines, const can_database_t *db);
static void write_attribute_defaults(line_vec_t *lines, const can_database_t *db);
static void write_attribute_values(line_vec_t *lines, const can_database_t *db);
static void write_value_tables(line_vec_t *lines, const can_database_t *db);
static void write_value_table_instances(line_vec_t *lines, const can_database_t *db);
static void write_local_value_tables(line_vec_t *lines, const can_database_t *db);
static void write_signal_value_types(line_vec_t *lines, const can_database_t *db);

/***************************************************************
** MARK: STATIC HELPERS
***************************************************************/

static bool is_empty(const char *s)
{
    return !s || s[0] == '\0';
}

/* Append a finished line, taking ownership of the kstring's buffer. */
static void lines_take(line_vec_t *lines, kstring_t *sb)
{
    char *s = ks_release(sb);   /* hand over the buffer and reset sb */
    if (!s) s = db_strdup("");
    kv_push(char *, *lines, s);
}

/* Append a formatted line. */
static void lines_addf(line_vec_t *lines, const char *fmt, ...)
{
    kstring_t sb = { 0, 0, NULL };

    va_list args;
    va_start(args, fmt);
    kvsprintf(&sb, fmt, args);
    va_end(args);

    lines_take(lines, &sb);
}

static void lines_add(line_vec_t *lines, const char *s)
{
    kv_push(char *, *lines, db_strdup(s));
}

static const can_database_attribute_t *find_attribute(const can_database_t *db, const char *name)
{
    for (size_t i = 0; i < kv_size(db->attributes); i++)
    {
        const can_database_attribute_t *attribute =
            DB_ARRAY_AT(db->attributes, can_database_attribute_t, i);
        if (attribute->name && name && strcmp(attribute->name, name) == 0) return attribute;
    }
    return NULL;
}

/* Append an attribute value the way GetStringForAttributeValue() did: quoted for
   STRING/ENUM definitions, bare for INT/FLOAT/HEX, bare when the definition is
   missing or some other type. */
static void append_attribute_value(kstring_t *sb, const can_database_t *db,
                                   const can_database_attribute_value_t *value)
{
    const can_database_attribute_t *def = find_attribute(db, value->attribute);

    if (def)
    {
        switch (def->type)
        {
            case CAN_DB_ATTR_INT:
            case CAN_DB_ATTR_FLOAT:
            case CAN_DB_ATTR_HEX:
                kputs(db_str(value->value), sb);
                return;

            case CAN_DB_ATTR_STRING:
            case CAN_DB_ATTR_ENUM:
                ksprintf(sb, "\"%s\"", db_str(value->value));
                return;

            default:
                break;
        }
    }

    kputs(db_str(value->value), sb);
}

/***************************************************************
** MARK: PUBLIC FUNCTIONS
***************************************************************/

bool database_writer_write(const char *file_path, const can_database_t *db,
                           char *error, size_t error_size,
                           char *detailed_error, size_t detailed_error_size)
{
    if (error && error_size)                   error[0] = '\0';
    if (detailed_error && detailed_error_size) detailed_error[0] = '\0';

    line_vec_t lines;
    kv_init(lines);

    write_version(&lines, db);
    write_namespace_symbols(&lines, db);
    write_bus_speed(&lines, db);
    write_nodes(&lines, db);
    write_messages(&lines, db);
    write_environment_variables(&lines, db);

    lines_add(&lines, "");
    lines_add(&lines, "");

    write_comments(&lines, db);
    write_attribute_definitions(&lines, db);
    write_attribute_defaults(&lines, db);
    write_attribute_values(&lines, db);
    write_value_tables(&lines, db);
    write_value_table_instances(&lines, db);
    write_local_value_tables(&lines, db);
    write_signal_value_types(&lines, db);

    lines_add(&lines, "");

    FILE *f = fopen(file_path, "wb");
    if (!f)
    {
        if (error && error_size) snprintf(error, error_size, "Failed to write file!");
        if (detailed_error && detailed_error_size)
            snprintf(detailed_error, detailed_error_size, "Could not open '%s' for writing", file_path);
        for (size_t i = 0; i < kv_size(lines); i++) free(kv_A(lines, i));
        kv_destroy(lines);
        return false;
    }

    bool write_ok = true;
    for (size_t i = 0; i < kv_size(lines); i++)
    {
        const char *line = kv_A(lines, i);
        if (fputs(line, f) == EOF || fputc('\n', f) == EOF)
        {
            write_ok = false;
            break;
        }
    }

    if (fclose(f) != 0) write_ok = false;

    for (size_t i = 0; i < kv_size(lines); i++) free(kv_A(lines, i));
    kv_destroy(lines);

    if (!write_ok)
    {
        if (error && error_size) snprintf(error, error_size, "Failed to write file!");
        if (detailed_error && detailed_error_size)
            snprintf(detailed_error, detailed_error_size, "Error writing '%s'", file_path);
        return false;
    }

    return true;
}

/***************************************************************
** MARK: STATIC FUNCTIONS
***************************************************************/

static void write_version(line_vec_t *lines, const can_database_t *db)
{
    lines_addf(lines, "VERSION \"%s\"", db_str(db->version));

    /* The C# writer emits two blank lines here. */
    lines_add(lines, "");
    lines_add(lines, "");
}

static void write_namespace_symbols(line_vec_t *lines, const can_database_t *db)
{
    lines_add(lines, "NS_ :");

    for (size_t i = 0; i < kv_size(db->namespace_symbols); i++)
    {
        const char *symbol = (const char *)kv_A(db->namespace_symbols, i);
        lines_addf(lines, "\t%s", symbol);
    }

    lines_add(lines, "");
}

static void write_bus_speed(line_vec_t *lines, const can_database_t *db)
{
    lines_addf(lines, "BS_: %s", db_str(db->bus_speed));
    lines_add(lines, "");
}

static void write_nodes(line_vec_t *lines, const can_database_t *db)
{
    kstring_t sb = { 0, 0, NULL };
    kputs("BU_:", &sb);

    for (size_t i = 0; i < kv_size(db->nodes); i++)
    {
        const can_database_node_t *node = DB_ARRAY_AT(db->nodes, can_database_node_t, i);
        ksprintf(&sb, " %s", db_str(node->name));
    }

    lines_take(lines, &sb);

    /* The C# writer emits two blank lines here. */
    lines_add(lines, "");
    lines_add(lines, "");
}

static void write_messages(line_vec_t *lines, const can_database_t *db)
{
    for (size_t m = 0; m < kv_size(db->messages); m++)
    {
        const can_database_message_t *message = DB_ARRAY_AT(db->messages, can_database_message_t, m);

        const char *sender = is_empty(message->sender_node) ? "Vector__XXX" : message->sender_node;

        lines_addf(lines, "BO_ %u %s: %u %s",
                   message->id, db_str(message->name), (unsigned)message->length, sender);

        for (size_t s = 0; s < kv_size(message->signals); s++)
        {
            const can_database_signal_t *signal =
                DB_ARRAY_AT(message->signals, can_database_signal_t, s);

            const char *byte_order = (signal->byte_order == CAN_DB_BYTE_ORDER_LITTLE_ENDIAN) ? "1" : "0";

            /* Anything other than UNSIGNED is written signed (incl. float/double). */
            const char *signal_type = (signal->signal_type == CAN_DB_SIGNAL_UNSIGNED) ? "+" : "-";

            const char *receiver = is_empty(signal->receiver_node) ? "Vector__XXX" : signal->receiver_node;

            /* Optional multiplexing token: " M", " m<value>", or " m<value>M". */
            char mux[32];
            if (signal->is_multiplexed && signal->is_multiplexor)
                snprintf(mux, sizeof mux, " m%sM", db_str(signal->multiplex_value));
            else if (signal->is_multiplexor)
                snprintf(mux, sizeof mux, " M");
            else if (signal->is_multiplexed)
                snprintf(mux, sizeof mux, " m%s", db_str(signal->multiplex_value));
            else
                mux[0] = '\0';

            lines_addf(lines, " SG_ %s%s : %u|%u@%s%s (%s,%s) [%s|%s] \"%s\" %s",
                       db_str(signal->name), mux,
                       signal->start_bit, signal->bit_length, byte_order, signal_type,
                       db_str(signal->scale), db_str(signal->offset),
                       db_str(signal->minimum), db_str(signal->maximum),
                       db_str(signal->unit), receiver);
        }

        lines_add(lines, "");
    }
}

static void write_environment_variables(line_vec_t *lines, const can_database_t *db)
{
    for (size_t i = 0; i < kv_size(db->environment_variables); i++)
    {
        const can_database_environment_variable_t *env =
            DB_ARRAY_AT(db->environment_variables, can_database_environment_variable_t, i);

        const char *type_num = env->type == CAN_DB_ATTR_FLOAT  ? "1"
                             : env->type == CAN_DB_ATTR_STRING ? "2"
                             :                                   "0";

        lines_addf(lines, "EV_ %s: %s [%s|%s] \"%s\" %s %s %s %s;",
                   db_str(env->name), type_num,
                   db_str(env->min), db_str(env->max), db_str(env->unit),
                   db_str(env->initial_value), db_str(env->ev_id),
                   db_str(env->access_type), db_str(env->node));
    }
}

static void write_comments(line_vec_t *lines, const can_database_t *db)
{
    for (size_t i = 0; i < kv_size(db->global_comments); i++)
    {
        const char *comment = (const char *)kv_A(db->global_comments, i);
        lines_addf(lines, "CM_ \"%s\";", comment);
    }

    for (size_t i = 0; i < kv_size(db->nodes); i++)
    {
        const can_database_node_t *node = DB_ARRAY_AT(db->nodes, can_database_node_t, i);
        if (!is_empty(node->comment))
        {
            lines_addf(lines, "CM_ BU_ %s \"%s\";", db_str(node->name), node->comment);
        }
    }

    for (size_t i = 0; i < kv_size(db->messages); i++)
    {
        const can_database_message_t *message = DB_ARRAY_AT(db->messages, can_database_message_t, i);
        if (!is_empty(message->comment))
        {
            lines_addf(lines, "CM_ BO_ %u \"%s\";", message->id, message->comment);
        }
    }

    for (size_t i = 0; i < kv_size(db->messages); i++)
    {
        const can_database_message_t *message = DB_ARRAY_AT(db->messages, can_database_message_t, i);
        for (size_t s = 0; s < kv_size(message->signals); s++)
        {
            const can_database_signal_t *signal =
                DB_ARRAY_AT(message->signals, can_database_signal_t, s);
            if (!is_empty(signal->comment))
            {
                lines_addf(lines, "CM_ SG_ %u %s \"%s\";",
                           message->id, db_str(signal->name), signal->comment);
            }
        }
    }
}

static void write_attribute_definitions(line_vec_t *lines, const can_database_t *db)
{
    for (size_t i = 0; i < kv_size(db->attributes); i++)
    {
        const can_database_attribute_t *attribute =
            DB_ARRAY_AT(db->attributes, can_database_attribute_t, i);

        kstring_t sb = { 0, 0, NULL };
        kputs("BA_DEF_ ", &sb);

        switch (attribute->target)
        {
            case CAN_DB_TARGET_NODE:    kputs("BU_  ", &sb); break;
            case CAN_DB_TARGET_SIGNAL:  kputs("SG_  ", &sb); break;
            case CAN_DB_TARGET_MESSAGE: kputs("BO_  ", &sb); break;
            default:                    kputs(" ", &sb);     break;
        }

        ksprintf(&sb, "\"%s\" ", db_str(attribute->name));

        switch (attribute->type)
        {
            case CAN_DB_ATTR_INT:
            case CAN_DB_ATTR_FLOAT:
            case CAN_DB_ATTR_HEX:
            {
                kputs(attribute->type == CAN_DB_ATTR_INT   ? "INT"
                    : attribute->type == CAN_DB_ATTR_FLOAT ? "FLOAT"
                    :                                        "HEX", &sb);

                if (!is_empty(attribute->min) && !is_empty(attribute->max))
                {
                    ksprintf(&sb, " %s %s;", attribute->min, attribute->max);
                }
            } break;

            case CAN_DB_ATTR_STRING:
            {
                kputs("STRING ;", &sb);
            } break;

            case CAN_DB_ATTR_ENUM:
            {
                kputs("ENUM  ", &sb);

                for (size_t e = 0; e < kv_size(attribute->enum_values); e++)
                {
                    const char *enum_value = (const char *)kv_A(attribute->enum_values, e);
                    ksprintf(&sb, "\"%s\"", enum_value);

                    if (e < kv_size(attribute->enum_values) - 1) kputs(",", &sb);
                }

                kputs(";", &sb);
            } break;

            default:
            {
                kputs(";", &sb);
            } break;
        }

        lines_take(lines, &sb);
    }
}

static void write_attribute_defaults(line_vec_t *lines, const can_database_t *db)
{
    for (size_t i = 0; i < kv_size(db->attributes); i++)
    {
        const can_database_attribute_t *attribute =
            DB_ARRAY_AT(db->attributes, can_database_attribute_t, i);

        kstring_t sb = { 0, 0, NULL };
        ksprintf(&sb, "BA_DEF_DEF_  \"%s\" ", db_str(attribute->name));

        switch (attribute->type)
        {
            case CAN_DB_ATTR_INT:
            case CAN_DB_ATTR_FLOAT:
            case CAN_DB_ATTR_HEX:
                ksprintf(&sb, "%s;", db_str(attribute->default_value));
                break;

            case CAN_DB_ATTR_STRING:
            case CAN_DB_ATTR_ENUM:
                ksprintf(&sb, "\"%s\";", db_str(attribute->default_value));
                break;

            default:
                kputs(";", &sb);
                break;
        }

        lines_take(lines, &sb);
    }
}

static void write_attribute_values(line_vec_t *lines, const can_database_t *db)
{
    for (size_t i = 0; i < kv_size(db->global_attribute_values); i++)
    {
        const can_database_attribute_value_t *value =
            DB_ARRAY_AT(db->global_attribute_values, can_database_attribute_value_t, i);

        kstring_t sb = { 0, 0, NULL };
        ksprintf(&sb, "BA_ \"%s\" ", db_str(value->attribute));
        append_attribute_value(&sb, db, value);
        kputs(";", &sb);
        lines_take(lines, &sb);
    }

    for (size_t n = 0; n < kv_size(db->nodes); n++)
    {
        const can_database_node_t *node = DB_ARRAY_AT(db->nodes, can_database_node_t, n);
        for (size_t i = 0; i < kv_size(node->attribute_values); i++)
        {
            const can_database_attribute_value_t *value =
                DB_ARRAY_AT(node->attribute_values, can_database_attribute_value_t, i);

            kstring_t sb = { 0, 0, NULL };
            ksprintf(&sb, "BA_ \"%s\" BU_ %s ", db_str(value->attribute), db_str(node->name));
            append_attribute_value(&sb, db, value);
            kputs(";", &sb);
            lines_take(lines, &sb);
        }
    }

    for (size_t m = 0; m < kv_size(db->messages); m++)
    {
        const can_database_message_t *message = DB_ARRAY_AT(db->messages, can_database_message_t, m);

        for (size_t i = 0; i < kv_size(message->attribute_values); i++)
        {
            const can_database_attribute_value_t *value =
                DB_ARRAY_AT(message->attribute_values, can_database_attribute_value_t, i);

            kstring_t sb = { 0, 0, NULL };
            ksprintf(&sb, "BA_ \"%s\" BO_ %u ", db_str(value->attribute), message->id);
            append_attribute_value(&sb, db, value);
            kputs(";", &sb);
            lines_take(lines, &sb);
        }

        for (size_t s = 0; s < kv_size(message->signals); s++)
        {
            const can_database_signal_t *signal =
                DB_ARRAY_AT(message->signals, can_database_signal_t, s);

            for (size_t i = 0; i < kv_size(signal->attribute_values); i++)
            {
                const can_database_attribute_value_t *value =
                    DB_ARRAY_AT(signal->attribute_values, can_database_attribute_value_t, i);

                kstring_t sb = { 0, 0, NULL };
                ksprintf(&sb, "BA_ \"%s\" SG_ %u %s ",
                         db_str(value->attribute), message->id, db_str(signal->name));
                append_attribute_value(&sb, db, value);
                kputs(";", &sb);
                lines_take(lines, &sb);
            }
        }
    }
}

static void write_value_tables(line_vec_t *lines, const can_database_t *db)
{
    for (size_t i = 0; i < kv_size(db->global_value_tables); i++)
    {
        const can_database_value_table_t *table =
            DB_ARRAY_AT(db->global_value_tables, can_database_value_table_t, i);

        /* Name unquoted and pairs space-separated so the reader (VAL_TABLE_
           <name> <key> "<val>" ...) can read it back. */
        kstring_t sb = { 0, 0, NULL };
        ksprintf(&sb, "VAL_TABLE_ %s", db_str(table->name));

        for (size_t v = 0; v < kv_size(table->values); v++)
        {
            ksprintf(&sb, " %ld \"%s\"", kv_A(table->values, v).key, db_str(kv_A(table->values, v).value));
        }

        kputs(";", &sb);
        lines_take(lines, &sb);
    }
}

static void write_value_table_instances(line_vec_t *lines, const can_database_t *db)
{
    for (size_t m = 0; m < kv_size(db->messages); m++)
    {
        const can_database_message_t *message = DB_ARRAY_AT(db->messages, can_database_message_t, m);

        for (size_t s = 0; s < kv_size(message->signals); s++)
        {
            const can_database_signal_t *signal =
                DB_ARRAY_AT(message->signals, can_database_signal_t, s);

            for (size_t t = 0; t < kv_size(signal->global_value_tables); t++)
            {
                const char *table_name = (const char *)kv_A(signal->global_value_tables, t);
                lines_addf(lines, "VAL_ %u %s %s;", message->id, db_str(signal->name), table_name);
            }
        }
    }
}

static void write_local_value_tables(line_vec_t *lines, const can_database_t *db)
{
    for (size_t m = 0; m < kv_size(db->messages); m++)
    {
        const can_database_message_t *message = DB_ARRAY_AT(db->messages, can_database_message_t, m);

        for (size_t s = 0; s < kv_size(message->signals); s++)
        {
            const can_database_signal_t *signal =
                DB_ARRAY_AT(message->signals, can_database_signal_t, s);

            for (size_t t = 0; t < kv_size(signal->local_value_tables); t++)
            {
                const can_database_value_table_t *table =
                    DB_ARRAY_AT(signal->local_value_tables, can_database_value_table_t, t);

                kstring_t sb = { 0, 0, NULL };
                ksprintf(&sb, "VAL_ %u %s", message->id, db_str(signal->name));

                for (size_t v = 0; v < kv_size(table->values); v++)
                {
                    ksprintf(&sb, " %ld \"%s\"", kv_A(table->values, v).key, db_str(kv_A(table->values, v).value));
                }

                kputs(";", &sb);
                lines_take(lines, &sb);
            }
        }
    }
}

static void write_signal_value_types(line_vec_t *lines, const can_database_t *db)
{
    for (size_t m = 0; m < kv_size(db->messages); m++)
    {
        const can_database_message_t *message = DB_ARRAY_AT(db->messages, can_database_message_t, m);

        for (size_t s = 0; s < kv_size(message->signals); s++)
        {
            const can_database_signal_t *signal =
                DB_ARRAY_AT(message->signals, can_database_signal_t, s);

            if (signal->signal_type == CAN_DB_SIGNAL_FLOAT || signal->signal_type == CAN_DB_SIGNAL_DOUBLE)
            {
                const char *value_type = (signal->signal_type == CAN_DB_SIGNAL_FLOAT) ? "1" : "2";
                lines_addf(lines, "SIG_VALTYPE_ %u %s : %s;",
                           message->id, db_str(signal->name), value_type);
            }
        }
    }
}
