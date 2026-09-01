/***************************************************************
**
** BusLab Source File
**
** File         :  can_database.c
** Module       :  data/dbc
** Author       :  SH
** License      :  MIT
** Description  :  CAN DBC in-memory data model implementation.
**
***************************************************************/

/***************************************************************
** MARK: INCLUDES
***************************************************************/

#include "can_database.h"

#include <stdlib.h>
#include <string.h>

/***************************************************************
** MARK: STATIC FUNCTION DEFS
***************************************************************/

static void free_string(void *s);
static void free_attribute_value(void *p);
static void free_node(void *p);
static void free_attribute(void *p);
static void free_value_table(void *p);
static void free_signal(void *p);
static void free_message(void *p);
static void free_environment_variable(void *p);

/***************************************************************
** MARK: PUBLIC FUNCTIONS
***************************************************************/

char *db_strdup(const char *s)
{
    if (!s) return NULL;
    return db_strndup(s, strlen(s));
}

char *db_strndup(const char *s, size_t n)
{
    if (!s) return NULL;

    char *out = (char *)malloc(n + 1);
    if (!out) return NULL;

    memcpy(out, s, n);
    out[n] = '\0';
    return out;
}

void db_str_set(char **dst, const char *s)
{
    db_str_setn(dst, s, s ? strlen(s) : 0);
}

void db_str_setn(char **dst, const char *s, size_t n)
{
    free(*dst);
    *dst = s ? db_strndup(s, n) : NULL;
}

void db_str_append(char **dst, const char *s)
{
    if (!s || !*s) return;

    size_t old_len = *dst ? strlen(*dst) : 0;
    size_t add_len = strlen(s);

    char *grown = (char *)realloc(*dst, old_len + add_len + 1);
    if (!grown) return;

    memcpy(grown + old_len, s, add_len + 1);
    *dst = grown;
}

const char *db_str(const char *s)
{
    return s ? s : "";
}

void db_array_init(db_array_t *array)
{
    kv_init(*array);
}

void *db_array_add(db_array_t *array, void *item)
{
    kv_push(void *, *array, item);
    return item;
}

void db_array_free(db_array_t *array, void (*free_item)(void *))
{
    if (free_item)
    {
        for (size_t i = 0; i < kv_size(*array); i++)
        {
            free_item(kv_A(*array, i));
        }
    }

    kv_destroy(*array);
    kv_init(*array);
}

can_database_node_t *can_database_node_create(void)
{
    can_database_node_t *node = (can_database_node_t *)calloc(1, sizeof(can_database_node_t));
    if (!node) return NULL;

    db_array_init(&node->attribute_values);
    return node;
}

can_database_attribute_t *can_database_attribute_create(void)
{
    can_database_attribute_t *attribute = (can_database_attribute_t *)calloc(1, sizeof(can_database_attribute_t));
    if (!attribute) return NULL;

    db_array_init(&attribute->enum_values);
    return attribute;
}

can_database_attribute_value_t *can_database_attribute_value_create(void)
{
    return (can_database_attribute_value_t *)calloc(1, sizeof(can_database_attribute_value_t));
}

can_database_value_table_t *can_database_value_table_create(void)
{
    return (can_database_value_table_t *)calloc(1, sizeof(can_database_value_table_t));
}

can_database_signal_t *can_database_signal_create(void)
{
    can_database_signal_t *signal = (can_database_signal_t *)calloc(1, sizeof(can_database_signal_t));
    if (!signal) return NULL;

    db_array_init(&signal->attribute_values);
    db_array_init(&signal->local_value_tables);
    db_array_init(&signal->global_value_tables);
    return signal;
}

can_database_message_t *can_database_message_create(void)
{
    can_database_message_t *message = (can_database_message_t *)calloc(1, sizeof(can_database_message_t));
    if (!message) return NULL;

    db_array_init(&message->signals);
    db_array_init(&message->attribute_values);
    return message;
}

can_database_environment_variable_t *can_database_environment_variable_create(void)
{
    return (can_database_environment_variable_t *)calloc(1, sizeof(can_database_environment_variable_t));
}

void can_database_value_table_set(can_database_value_table_t *table, long key, const char *value)
{
    /* Overwrite an existing key, mirroring Dictionary[key] = value. */
    for (size_t i = 0; i < kv_size(table->values); i++)
    {
        if (kv_A(table->values, i).key == key)
        {
            db_str_set(&kv_A(table->values, i).value, value);
            return;
        }
    }

    can_database_value_pair_t pair;
    pair.key   = key;
    pair.value = db_strdup(value);
    kv_push(can_database_value_pair_t, table->values, pair);
}

void can_database_node_destroy(can_database_node_t *node)                   { free_node(node); }
void can_database_attribute_destroy(can_database_attribute_t *attribute)    { free_attribute(attribute); }
void can_database_attribute_value_destroy(can_database_attribute_value_t *v){ free_attribute_value(v); }
void can_database_value_table_destroy(can_database_value_table_t *table)    { free_value_table(table); }
void can_database_signal_destroy(can_database_signal_t *signal)             { free_signal(signal); }
void can_database_message_destroy(can_database_message_t *message)          { free_message(message); }

can_database_t *can_database_create(void)
{
    can_database_t *database = (can_database_t *)calloc(1, sizeof(can_database_t));
    if (!database) return NULL;

    db_array_init(&database->namespace_symbols);
    db_array_init(&database->nodes);
    db_array_init(&database->messages);
    db_array_init(&database->global_comments);
    db_array_init(&database->attributes);
    db_array_init(&database->global_attribute_values);
    db_array_init(&database->global_value_tables);
    db_array_init(&database->environment_variables);

    return database;
}

void can_database_free(can_database_t *database)
{
    if (!database) return;

    free(database->version);
    free(database->bus_speed);

    db_array_free(&database->namespace_symbols, free_string);
    db_array_free(&database->nodes, free_node);
    db_array_free(&database->messages, free_message);
    db_array_free(&database->global_comments, free_string);
    db_array_free(&database->attributes, free_attribute);
    db_array_free(&database->global_attribute_values, free_attribute_value);
    db_array_free(&database->global_value_tables, free_value_table);
    db_array_free(&database->environment_variables, free_environment_variable);

    free(database);
}

/***************************************************************
** MARK: STATIC FUNCTIONS
***************************************************************/

static void free_string(void *s)
{
    free(s);
}

static void free_attribute_value(void *p)
{
    can_database_attribute_value_t *value = (can_database_attribute_value_t *)p;
    if (!value) return;

    free(value->attribute);
    free(value->value);
    free(value);
}

static void free_node(void *p)
{
    can_database_node_t *node = (can_database_node_t *)p;
    if (!node) return;

    free(node->name);
    free(node->comment);
    db_array_free(&node->attribute_values, free_attribute_value);
    free(node);
}

static void free_attribute(void *p)
{
    can_database_attribute_t *attribute = (can_database_attribute_t *)p;
    if (!attribute) return;

    free(attribute->name);
    free(attribute->min);
    free(attribute->max);
    free(attribute->default_value);
    db_array_free(&attribute->enum_values, free_string);
    free(attribute);
}

static void free_value_table(void *p)
{
    can_database_value_table_t *table = (can_database_value_table_t *)p;
    if (!table) return;

    free(table->name);
    for (size_t i = 0; i < kv_size(table->values); i++)
    {
        free(kv_A(table->values, i).value);
    }
    kv_destroy(table->values);
    free(table);
}

static void free_signal(void *p)
{
    can_database_signal_t *signal = (can_database_signal_t *)p;
    if (!signal) return;

    free(signal->name);
    free(signal->multiplex_value);
    free(signal->multiplexor_signal);
    free(signal->scale);
    free(signal->offset);
    free(signal->minimum);
    free(signal->maximum);
    free(signal->unit);
    free(signal->receiver_node);
    free(signal->comment);

    db_array_free(&signal->attribute_values, free_attribute_value);
    db_array_free(&signal->local_value_tables, free_value_table);
    db_array_free(&signal->global_value_tables, free_string);
    free(signal);
}

static void free_message(void *p)
{
    can_database_message_t *message = (can_database_message_t *)p;
    if (!message) return;

    free(message->name);
    free(message->sender_node);
    free(message->comment);
    db_array_free(&message->signals, free_signal);
    db_array_free(&message->attribute_values, free_attribute_value);
    free(message);
}

static void free_environment_variable(void *p)
{
    can_database_environment_variable_t *env = (can_database_environment_variable_t *)p;
    if (!env) return;

    free(env->name);
    free(env->min);
    free(env->max);
    free(env->unit);
    free(env->initial_value);
    free(env->ev_id);
    free(env->access_type);
    free(env->node);
    free(env);
}
