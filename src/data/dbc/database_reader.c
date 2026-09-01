/***************************************************************
**
** BusLab Source File
**
** File         :  database_reader.c
** Module       :  data/dbc
** Author       :  SH
** License      :  MIT
** Description  :  DBC file reader implementation.
**
**                 The C# original leans on System.Text.Regex;
**                 here each line form is parsed by hand so the
**                 reader stays dependency-free and cross-platform.
**                 The line-by-line state machine mirrors the C#.
**
***************************************************************/

/***************************************************************
** MARK: INCLUDES
***************************************************************/

#include "database_reader.h"

#include <ctype.h>
#include <errno.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

/***************************************************************
** MARK: TYPEDEFS
***************************************************************/

typedef enum
{
    PARSE_NONE,
    PARSE_NAMESPACES,
    PARSE_NODES,
    PARSE_MESSAGE,
    PARSE_GLOBAL_COMMENT,
    PARSE_NODE_COMMENT,
    PARSE_MESSAGE_COMMENT,
    PARSE_SIGNAL_COMMENT
} parse_state_t;

typedef struct
{
    parse_state_t state;
    char          error[1024];   /* empty string == no error */

    can_database_node_t    *current_node;
    can_database_message_t *current_message;
    can_database_signal_t  *current_signal;
} parser_t;

/***************************************************************
** MARK: STATIC FUNCTION DEFS
***************************************************************/

static bool read_all_lines(const char *path, db_array_t *lines);

static void parse_line(parser_t *ps, const char *line, can_database_t *db);

static void parse_version(parser_t *ps, db_array_t *tokens, can_database_t *db);
static void parse_namespaces(parser_t *ps, db_array_t *tokens, can_database_t *db);
static void parse_speed(parser_t *ps, db_array_t *tokens, can_database_t *db);
static void parse_nodes(parser_t *ps, db_array_t *tokens, can_database_t *db);
static void parse_message_signal(parser_t *ps, const char *line, db_array_t *tokens, can_database_t *db);
static void parse_message(parser_t *ps, const char *line, can_database_t *db);
static void parse_signal(parser_t *ps, const char *line, can_database_t *db);
static void parse_comment(parser_t *ps, const char *line, can_database_t *db);
static void parse_attribute_definition(parser_t *ps, const char *line, can_database_t *db);
static void parse_attribute_default(parser_t *ps, const char *line, can_database_t *db);
static void parse_attribute_assignment(parser_t *ps, const char *line, can_database_t *db);
static void parse_value_table(parser_t *ps, const char *line, can_database_t *db);
static void parse_value_table_assignment(parser_t *ps, const char *line, can_database_t *db);
static void parse_signal_value_type(parser_t *ps, const char *line, can_database_t *db);
static void parse_environment_variable(parser_t *ps, const char *line, can_database_t *db);

/* lookups */
static can_database_node_t    *find_node(can_database_t *db, const char *name);
static can_database_message_t *find_message(can_database_t *db, uint32_t id);
static can_database_signal_t  *find_signal(can_database_message_t *message, const char *name);
static can_database_attribute_t *find_attribute(can_database_t *db, const char *name);

/***************************************************************
** MARK: STATIC HELPERS — formatting / numbers
***************************************************************/

static void set_error(parser_t *ps, const char *fmt, ...)
{
    /* First error wins, mirroring the abort-on-first-error C# loop. */
    if (ps->error[0] != '\0') return;

    va_list args;
    va_start(args, fmt);
    vsnprintf(ps->error, sizeof(ps->error), fmt, args);
    va_end(args);

    /* Guard against an accidentally-empty formatted message. */
    if (ps->error[0] == '\0')
    {
        snprintf(ps->error, sizeof(ps->error), "Parse error");
    }
}

static bool parse_uint32(const char *s, uint32_t *out)
{
    if (!s || !*s) return false;

    errno = 0;
    char *end = NULL;
    unsigned long long v = strtoull(s, &end, 10);

    if (errno != 0 || end == s || *end != '\0') return false;
    if (v > 0xFFFFFFFFULL) return false;

    *out = (uint32_t)v;
    return true;
}

static bool parse_byte(const char *s, uint8_t *out)
{
    if (!s || !*s) return false;

    errno = 0;
    char *end = NULL;
    unsigned long long v = strtoull(s, &end, 10);

    if (errno != 0 || end == s || *end != '\0') return false;
    if (v > 0xFFULL) return false;

    *out = (uint8_t)v;
    return true;
}

static bool parse_long(const char *s, long *out)
{
    if (!s || !*s) return false;

    errno = 0;
    char *end = NULL;
    long v = strtol(s, &end, 10);

    if (errno != 0 || end == s || *end != '\0') return false;

    *out = v;
    return true;
}

/***************************************************************
** MARK: STATIC HELPERS — string scanning
***************************************************************/

static bool is_word_char(char c)
{
    return isalnum((unsigned char)c) || c == '_';
}

static bool is_blank(const char *s)
{
    for (; *s; s++)
    {
        if (!isspace((unsigned char)*s)) return false;
    }
    return true;
}

static bool token_eq(const char *token, const char *literal)
{
    return strcmp(token, literal) == 0;
}

static void skip_ws(const char **p)
{
    while (**p == ' ' || **p == '\t') (*p)++;
}

static bool consume_literal(const char **p, const char *literal)
{
    size_t len = strlen(literal);
    if (strncmp(*p, literal, len) == 0)
    {
        *p += len;
        return true;
    }
    return false;
}

/* Like consume_literal but requires the keyword to be followed by whitespace or
   end-of-line, so it can't swallow the prefix of a longer identifier. */
static bool consume_keyword(const char **p, const char *keyword)
{
    size_t len = strlen(keyword);
    if (strncmp(*p, keyword, len) != 0) return false;

    char after = (*p)[len];
    if (after != '\0' && after != ' ' && after != '\t') return false;

    *p += len;
    return true;
}

static bool consume_char(const char **p, char c)
{
    if (**p == c)
    {
        (*p)++;
        return true;
    }
    return false;
}

static char *read_word(const char **p)
{
    const char *start = *p;
    while (is_word_char(**p)) (*p)++;
    if (*p == start) return NULL;

    char *out = db_strndup(start, (size_t)(*p - start));
    return out;
}

static char *read_digits(const char **p)
{
    const char *start = *p;
    while (isdigit((unsigned char)**p)) (*p)++;
    if (*p == start) return NULL;

    return db_strndup(start, (size_t)(*p - start));
}

static char *read_int_literal(const char **p)
{
    const char *start = *p;
    if (**p == '+' || **p == '-') (*p)++;

    const char *digits = *p;
    while (isdigit((unsigned char)**p)) (*p)++;

    if (*p == digits)
    {
        *p = start; /* no digits — rewind the sign */
        return NULL;
    }
    return db_strndup(start, (size_t)(*p - start));
}

static char *read_nonspace(const char **p)
{
    const char *start = *p;
    while (**p != '\0' && !isspace((unsigned char)**p)) (*p)++;
    if (*p == start) return NULL;

    return db_strndup(start, (size_t)(*p - start));
}

/* Reads up to (but not consuming) the delimiter or end-of-string. May be "". */
static char *read_until(const char **p, char delim)
{
    const char *start = *p;
    while (**p != '\0' && **p != delim) (*p)++;
    return db_strndup(start, (size_t)(*p - start));
}

/* Reads a bare value token: up to whitespace, ';' or end. NULL if empty. */
static char *read_value_token(const char **p)
{
    const char *start = *p;
    while (**p != '\0' && !isspace((unsigned char)**p) && **p != ';') (*p)++;
    if (*p == start) return NULL;

    return db_strndup(start, (size_t)(*p - start));
}

static void trim_inplace(char *s)
{
    if (!s) return;

    size_t len = strlen(s);
    size_t a = 0;
    while (a < len && isspace((unsigned char)s[a])) a++;

    size_t b = len;
    while (b > a && isspace((unsigned char)s[b - 1])) b--;

    memmove(s, s + a, b - a);
    s[b - a] = '\0';
}

static void rstrip(char *s)
{
    size_t n = strlen(s);
    while (n > 0 && isspace((unsigned char)s[n - 1])) n--;
    s[n] = '\0';
}

/* true if the line, once trimmed, ends with ';' (mirrors line.Trim().EndsWith) */
static bool trimmed_ends_with_semicolon(const char *line)
{
    const char *end = line + strlen(line);
    while (end > line && isspace((unsigned char)end[-1])) end--;
    return end > line && end[-1] == ';';
}

/* trim + split on ' ' (mirrors line.Trim().Split(' ')); always yields >= 1 token */
static void tokenize(const char *line, db_array_t *out)
{
    db_array_init(out);

    const char *s = line;
    while (*s && isspace((unsigned char)*s)) s++;

    const char *e = line + strlen(line);
    while (e > s && isspace((unsigned char)e[-1])) e--;

    const char *p = s;
    const char *tok = s;

    for (;;)
    {
        if (p >= e || *p == ' ')
        {
            db_array_add(out, db_strndup(tok, (size_t)(p - tok)));
            if (p >= e) break;
            p++;
            tok = p;
        }
        else
        {
            p++;
        }
    }
}

/***************************************************************
** MARK: PUBLIC FUNCTIONS
***************************************************************/

can_database_t *database_reader_read(const char *file_path,
                                     char *error, size_t error_size,
                                     char *detailed_error, size_t detailed_error_size)
{
    if (error && error_size)                   error[0] = '\0';
    if (detailed_error && detailed_error_size) detailed_error[0] = '\0';

    can_database_t *database = can_database_create();
    if (!database)
    {
        if (error && error_size) snprintf(error, error_size, "Out of memory");
        return NULL;
    }

    db_array_t lines;
    if (!read_all_lines(file_path, &lines))
    {
        if (error && error_size) snprintf(error, error_size, "Failed to open file!");
        if (detailed_error && detailed_error_size)
            snprintf(detailed_error, detailed_error_size, "Could not read '%s'", file_path);
        can_database_free(database);
        return NULL;
    }

    parser_t ps;
    memset(&ps, 0, sizeof(ps));
    ps.state = PARSE_NONE;

    for (size_t i = 0; i < lines.n; i++)
    {
        const char *line = (const char *)lines.a[i];

        parse_line(&ps, line, database);

        if (ps.error[0] != '\0')
        {
            if (error && error_size)
                snprintf(error, error_size, "Error parsing file %s at line %zu", file_path, i + 1);
            if (detailed_error && detailed_error_size)
                snprintf(detailed_error, detailed_error_size, "%s", ps.error);

            db_array_free(&lines, free);
            can_database_free(database);
            return NULL;
        }
    }

    db_array_free(&lines, free);
    return database;
}

/***************************************************************
** MARK: STATIC FUNCTIONS — file / dispatch
***************************************************************/

static bool read_all_lines(const char *path, db_array_t *lines)
{
    db_array_init(lines);

    FILE *f = fopen(path, "rb");
    if (!f) return false;

    if (fseek(f, 0, SEEK_END) != 0) { fclose(f); return false; }
    long size = ftell(f);
    if (size < 0) { fclose(f); return false; }
    rewind(f);

    char *buffer = (char *)malloc((size_t)size + 1);
    if (!buffer) { fclose(f); return false; }

    size_t read = fread(buffer, 1, (size_t)size, f);
    fclose(f);
    buffer[read] = '\0';

    /* Split on '\n', dropping a trailing '\r', so '\n' and '\r\n' both work.
       Matches File.ReadAllLines: no phantom empty element after a final
       newline, and an empty file yields no lines. */
    size_t start = 0;
    for (size_t i = 0; i <= read; i++)
    {
        if (i == read)
        {
            if (start < read)
            {
                size_t end = read;
                if (end > start && buffer[end - 1] == '\r') end--;
                db_array_add(lines, db_strndup(buffer + start, end - start));
            }
            break;
        }

        if (buffer[i] == '\n')
        {
            size_t end = i;
            if (end > start && buffer[end - 1] == '\r') end--;
            db_array_add(lines, db_strndup(buffer + start, end - start));
            start = i + 1;
        }
    }

    free(buffer);
    return true;
}

static void parse_line(parser_t *ps, const char *line, can_database_t *db)
{
    db_array_t tokens;
    tokenize(line, &tokens);

    bool parsed = false;

    if (tokens.n > 0 && ps->state == PARSE_NONE)
    {
        const char *t0 = (const char *)tokens.a[0];

        if (token_eq(t0, "VERSION"))
        {
            parse_version(ps, &tokens, db);
            parsed = true;
        }
        else if (token_eq(t0, "NS_") || token_eq(t0, "NS_:"))
        {
            parse_namespaces(ps, &tokens, db);
            parsed = true;
        }
        else if (token_eq(t0, "BS_") || token_eq(t0, "BS_:"))
        {
            parse_speed(ps, &tokens, db);
            parsed = true;
        }
        else if (token_eq(t0, "BU_") || token_eq(t0, "BU_:"))
        {
            parse_nodes(ps, &tokens, db);
            parsed = true;
        }
        else if (token_eq(t0, "BO_"))
        {
            parse_message_signal(ps, line, &tokens, db);
            parsed = true;
        }
        else if (token_eq(t0, "CM_"))
        {
            parse_comment(ps, line, db);
            parsed = true;
        }
        else if (token_eq(t0, "BA_DEF_"))
        {
            parse_attribute_definition(ps, line, db);
            parsed = true;
        }
        else if (token_eq(t0, "BA_DEF_DEF_"))
        {
            parse_attribute_default(ps, line, db);
            parsed = true;
        }
        else if (token_eq(t0, "BA_"))
        {
            parse_attribute_assignment(ps, line, db);
            parsed = true;
        }
        else if (token_eq(t0, "VAL_TABLE_"))
        {
            parse_value_table(ps, line, db);
            parsed = true;
        }
        else if (token_eq(t0, "VAL_"))
        {
            parse_value_table_assignment(ps, line, db);
            parsed = true;
        }
        else if (token_eq(t0, "SIG_VALTYPE_"))
        {
            parse_signal_value_type(ps, line, db);
            parsed = true;
        }
        else if (token_eq(t0, "EV_"))
        {
            parse_environment_variable(ps, line, db);
            parsed = true;
        }
    }

    if (!parsed)
    {
        /* Multi-line constructs continue here based on the active state. */
        switch (ps->state)
        {
            case PARSE_NAMESPACES:
                parse_namespaces(ps, &tokens, db);
                break;

            case PARSE_NODES:
                parse_nodes(ps, &tokens, db);
                break;

            case PARSE_MESSAGE:
                parse_message_signal(ps, line, &tokens, db);
                break;

            case PARSE_GLOBAL_COMMENT:
            case PARSE_NODE_COMMENT:
            case PARSE_MESSAGE_COMMENT:
            case PARSE_SIGNAL_COMMENT:
                parse_comment(ps, line, db);
                break;

            default:
                break;
        }
    }

    db_array_free(&tokens, free);
}

/***************************************************************
** MARK: STATIC FUNCTIONS — section parsers
***************************************************************/

static void parse_version(parser_t *ps, db_array_t *tokens, can_database_t *db)
{
    if (tokens->n > 1)
    {
        const char *v = (const char *)tokens->a[1];
        size_t len = strlen(v);

        if (len >= 2 && v[0] == '"' && v[len - 1] == '"')
        {
            db_str_setn(&db->version, v + 1, len - 2);
        }
    }

    ps->state = PARSE_NONE;
}

static void parse_namespaces(parser_t *ps, db_array_t *tokens, can_database_t *db)
{
    if (tokens->n == 0)
    {
        ps->state = PARSE_NONE;
        return;
    }

    const char *t0 = (const char *)tokens->a[0];
    size_t pos = 0;

    if (tokens->n >= 2 && token_eq(t0, "NS_") && token_eq((const char *)tokens->a[1], ":"))
    {
        pos = 2;
        ps->state = PARSE_NAMESPACES;
    }

    if (tokens->n >= 1 && token_eq(t0, "NS_:"))
    {
        pos = 1;
        ps->state = PARSE_NAMESPACES;
    }

    if (ps->state == PARSE_NAMESPACES)
    {
        for (size_t i = pos; i < tokens->n; i++)
        {
            const char *tok = (const char *)tokens->a[i];
            if (!is_blank(tok))
            {
                db_array_add(&db->namespace_symbols, db_strdup(tok));
            }
        }
    }

    if (is_blank(t0))
    {
        ps->state = PARSE_NONE;
    }
}

static void parse_speed(parser_t *ps, db_array_t *tokens, can_database_t *db)
{
    (void)ps;

    const char *t0 = (const char *)tokens->a[0];
    size_t pos = 0;

    if (tokens->n >= 2 && token_eq(t0, "BS_") && token_eq((const char *)tokens->a[1], ":"))
    {
        pos = 2;
    }

    if (tokens->n >= 1 && token_eq(t0, "BS_:"))
    {
        pos = 1;
    }

    if (pos > 0)
    {
        size_t total = 1;
        for (size_t i = pos; i < tokens->n; i++)
        {
            total += strlen((const char *)tokens->a[i]) + 1;
        }

        char *combined = (char *)malloc(total);
        if (combined)
        {
            combined[0] = '\0';
            for (size_t i = pos; i < tokens->n; i++)
            {
                if (i > pos) strcat(combined, " ");
                strcat(combined, (const char *)tokens->a[i]);
            }
            db_str_set(&db->bus_speed, combined);
            free(combined);
        }
    }
}

static void parse_nodes(parser_t *ps, db_array_t *tokens, can_database_t *db)
{
    if (tokens->n == 0)
    {
        ps->state = PARSE_NONE;
        return;
    }

    const char *t0 = (const char *)tokens->a[0];
    size_t pos = 0;

    if (tokens->n >= 2 && token_eq(t0, "BU_") && token_eq((const char *)tokens->a[1], ":"))
    {
        pos = 2;
        ps->state = PARSE_NODES;
    }

    if (tokens->n >= 1 && token_eq(t0, "BU_:"))
    {
        pos = 1;
        ps->state = PARSE_NODES;
    }

    if (ps->state == PARSE_NODES)
    {
        for (size_t i = pos; i < tokens->n; i++)
        {
            const char *tok = (const char *)tokens->a[i];
            if (!is_blank(tok))
            {
                can_database_node_t *node = can_database_node_create();
                db_str_set(&node->name, tok);
                db_array_add(&db->nodes, node);
            }
        }
    }

    if (is_blank(t0))
    {
        ps->state = PARSE_NONE;
    }
}

static void parse_message_signal(parser_t *ps, const char *line, db_array_t *tokens, can_database_t *db)
{
    const char *t0 = tokens->n > 0 ? (const char *)tokens->a[0] : "";

    if (token_eq(t0, "BO_"))
    {
        parse_message(ps, line, db);
        ps->state = PARSE_MESSAGE;
    }
    else if (token_eq(t0, "SG_") && ps->state == PARSE_MESSAGE)
    {
        parse_signal(ps, line, db);
    }
    else
    {
        ps->state = PARSE_NONE;
    }
}

static void parse_message(parser_t *ps, const char *line, can_database_t *db)
{
    can_database_message_t *message = can_database_message_create();

    const char *p = line;
    skip_ws(&p);

    bool ok = consume_literal(&p, "BO_");
    skip_ws(&p);

    char *id_str  = ok ? read_digits(&p) : NULL;
    char *name    = NULL;
    char *dlc_str = NULL;
    char *sender  = NULL;

    /* (\d+)\s+ — the id must be followed by whitespace; a stray non-digit such
       as "12z96" makes the whole line unparseable, matching the C# regex. */
    if (ok && id_str && *p != ' ' && *p != '\t') ok = false;

    if (ok && id_str)
    {
        skip_ws(&p);
        name = read_until(&p, ':');
        if (consume_char(&p, ':'))
        {
            skip_ws(&p);
            dlc_str = read_digits(&p);
            /* (\d+)\s+(\S+) — the DLC likewise must be followed by whitespace. */
            if (dlc_str && *p != ' ' && *p != '\t') ok = false;
            skip_ws(&p);
            sender = read_nonspace(&p);
        }
    }

    if (!ok || !id_str || !name || name[0] == '\0' || !dlc_str || !sender)
    {
        set_error(ps, "Unable to parse message line: %s", line);
    }
    else
    {
        uint32_t id;
        if (!parse_uint32(id_str, &id))
        {
            set_error(ps, "Invalid message ID '%s' in line: %s", id_str, line);
        }
        else
        {
            message->id = id;
        }

        trim_inplace(name);
        db_str_set(&message->name, name);

        uint8_t dlc;
        if (!parse_byte(dlc_str, &dlc))
        {
            set_error(ps, "Invalid DLC '%s' in line: %s", dlc_str, line);
        }
        else
        {
            message->length = dlc;
        }

        if (strcmp(sender, "Vector__XXX") == 0)
        {
            db_str_set(&message->sender_node, "");
        }
        else
        {
            db_str_set(&message->sender_node, sender);
        }
    }

    free(id_str);
    free(name);
    free(dlc_str);
    free(sender);

    db_array_add(&db->messages, message);
}

static void parse_signal(parser_t *ps, const char *line, can_database_t *db)
{
    can_database_signal_t *signal = can_database_signal_create();

    const char *p = line;
    skip_ws(&p);

    bool ok = consume_literal(&p, "SG_");
    skip_ws(&p);

    char *name = ok ? read_word(&p) : NULL;
    ok = ok && name != NULL;

    skip_ws(&p);

    /* Optional multiplexing indicator between the name and the ':' —
       'M' marks the multiplexor (switch), 'm<value>' a multiplexed signal, and
       the rare 'm<value>M' is both. */
    if (ok && *p == 'M' && !is_word_char(p[1]))
    {
        signal->is_multiplexor = true;
        p++;
        skip_ws(&p);
    }
    else if (ok && *p == 'm' && isdigit((unsigned char)p[1]))
    {
        p++;
        char *mux_value = read_digits(&p);
        signal->is_multiplexed = true;
        db_str_set(&signal->multiplex_value, mux_value);
        free(mux_value);

        if (*p == 'M') { signal->is_multiplexor = true; p++; }
        skip_ws(&p);
    }

    ok = ok && consume_char(&p, ':');
    skip_ws(&p);

    char *start_str = ok ? read_digits(&p) : NULL;
    ok = ok && start_str != NULL && consume_char(&p, '|');

    char *len_str = ok ? read_digits(&p) : NULL;
    ok = ok && len_str != NULL && consume_char(&p, '@');

    char byte_order_c = (ok && isdigit((unsigned char)*p)) ? *p : '\0';
    if (byte_order_c) p++;
    ok = ok && byte_order_c != '\0';

    char sign_c = (ok && (*p == '+' || *p == '-')) ? *p : '\0';
    if (sign_c) p++;
    ok = ok && sign_c != '\0';

    skip_ws(&p);
    ok = ok && consume_char(&p, '(');
    char *scale  = ok ? read_until(&p, ',') : NULL;
    ok = ok && scale != NULL && scale[0] != '\0' && consume_char(&p, ',');
    char *offset = ok ? read_until(&p, ')') : NULL;
    ok = ok && offset != NULL && offset[0] != '\0' && consume_char(&p, ')');

    skip_ws(&p);
    ok = ok && consume_char(&p, '[');
    char *min_str = ok ? read_until(&p, '|') : NULL;
    ok = ok && min_str != NULL && min_str[0] != '\0' && consume_char(&p, '|');
    char *max_str = ok ? read_until(&p, ']') : NULL;
    ok = ok && max_str != NULL && max_str[0] != '\0' && consume_char(&p, ']');

    skip_ws(&p);
    ok = ok && consume_char(&p, '"');
    char *unit = ok ? read_until(&p, '"') : NULL;
    ok = ok && unit != NULL && consume_char(&p, '"');

    skip_ws(&p);
    char *receiver = ok ? read_nonspace(&p) : NULL;
    ok = ok && receiver != NULL;

    if (!ok)
    {
        set_error(ps, "Unable to parse signal line: %s", line);
    }
    else
    {
        db_str_set(&signal->name, name);

        uint32_t start_bit;
        if (!parse_uint32(start_str, &start_bit))
            set_error(ps, "Invalid start bit '%s' in line: %s", start_str, line);
        else
            signal->start_bit = start_bit;

        uint32_t bit_length;
        if (!parse_uint32(len_str, &bit_length))
            set_error(ps, "Invalid signal length '%s' in line: %s", len_str, line);
        else
            signal->bit_length = bit_length;

        if (byte_order_c == '0')
            signal->byte_order = CAN_DB_BYTE_ORDER_BIG_ENDIAN;
        else if (byte_order_c == '1')
            signal->byte_order = CAN_DB_BYTE_ORDER_LITTLE_ENDIAN;
        else
            set_error(ps, "Invalid byte order '%c' in line: %s", byte_order_c, line);

        signal->signal_type = (sign_c == '+') ? CAN_DB_SIGNAL_UNSIGNED : CAN_DB_SIGNAL_SIGNED;

        db_str_set(&signal->scale, scale);
        db_str_set(&signal->offset, offset);
        db_str_set(&signal->minimum, min_str);
        db_str_set(&signal->maximum, max_str);
        db_str_set(&signal->unit, unit);

        if (strcmp(receiver, "Vector__XXX") == 0)
            db_str_set(&signal->receiver_node, "");
        else
            db_str_set(&signal->receiver_node, receiver);
    }

    free(name);
    free(start_str);
    free(len_str);
    free(scale);
    free(offset);
    free(min_str);
    free(max_str);
    free(unit);
    free(receiver);

    if (db->messages.n > 0)
    {
        can_database_message_t *last =
            DB_ARRAY_AT(db->messages, can_database_message_t, db->messages.n - 1);
        db_array_add(&last->signals, signal);
    }
    else
    {
        set_error(ps, "Signal defined before any message in line: %s", line);
        can_database_signal_destroy(signal);   /* nothing owns it */
    }
}

static void parse_comment(parser_t *ps, const char *line, can_database_t *db)
{
    bool is_end = trimmed_ends_with_semicolon(line);

    if (ps->state == PARSE_NONE)
    {
        const char *p = line;
        skip_ws(&p);

        if (!consume_literal(&p, "CM_"))
        {
            set_error(ps, "Unable to parse CM_ line: %s", line);
            return;
        }
        skip_ws(&p);

        int   target  = 0;  /* 0 none, 1 BU_, 2 BO_, 3 SG_ */
        char *id_str  = NULL;
        char *name    = NULL;

        if (*p != '"')
        {
            if      (consume_keyword(&p, "BU_")) target = 1;
            else if (consume_keyword(&p, "BO_")) target = 2;
            else if (consume_keyword(&p, "SG_")) target = 3;
            else
            {
                set_error(ps, "Unable to parse CM_ line: %s", line);
                return;
            }

            skip_ws(&p);

            if (isdigit((unsigned char)*p))
            {
                id_str = read_digits(&p);
                skip_ws(&p);
            }

            if (*p != '"' && is_word_char(*p))
            {
                name = read_word(&p);
                skip_ws(&p);
            }
        }

        if (!consume_char(&p, '"'))
        {
            set_error(ps, "Unable to parse CM_ line: %s", line);
            free(id_str);
            free(name);
            return;
        }

        char *comment = read_until(&p, '"');   /* to closing quote or end */

        if (target == 0)
        {
            db_array_add(&db->global_comments, db_strdup(comment));
            if (!is_end) ps->state = PARSE_GLOBAL_COMMENT;
        }
        else if (target == 1)
        {
            can_database_node_t *node = name ? find_node(db, name) : NULL;
            if (node)
            {
                db_str_set(&node->comment, comment);
                if (!is_end)
                {
                    ps->current_node = node;
                    ps->state = PARSE_NODE_COMMENT;
                }
            }
            else
            {
                set_error(ps, "Could not find node '%s' for CM_ line: %s", db_str(name), line);
            }
        }
        else if (target == 2)
        {
            uint32_t id = 0;
            if (!parse_uint32(id_str, &id))
            {
                set_error(ps, "Invalid message ID '%s' in line: %s", db_str(id_str), line);
            }
            else
            {
                can_database_message_t *message = find_message(db, id);
                if (message)
                {
                    db_str_set(&message->comment, comment);
                    if (!is_end)
                    {
                        ps->current_message = message;
                        ps->state = PARSE_MESSAGE_COMMENT;
                    }
                }
                else
                {
                    set_error(ps, "Could not find message ID %u for CM_ line: %s", id, line);
                }
            }
        }
        else /* target == 3 */
        {
            uint32_t id = 0;
            if (!parse_uint32(id_str, &id))
            {
                set_error(ps, "Invalid message ID '%s' in line: %s", db_str(id_str), line);
            }
            else
            {
                can_database_message_t *message = find_message(db, id);
                can_database_signal_t  *signal  = message ? find_signal(message, db_str(name)) : NULL;
                if (signal)
                {
                    db_str_set(&signal->comment, comment);
                    if (!is_end)
                    {
                        ps->current_signal = signal;
                        ps->state = PARSE_SIGNAL_COMMENT;
                    }
                }
                else
                {
                    set_error(ps, "Could not find signal '%s' in message ID %u for CM_ line: %s",
                              db_str(name), id, line);
                }
            }
        }

        free(id_str);
        free(name);
        free(comment);
    }
    else
    {
        /* Continuation line: append everything up to the first quote. */
        const char *p = line;
        skip_ws(&p);
        char *part = read_until(&p, '"');

        char **dst = NULL;
        if (ps->state == PARSE_NODE_COMMENT && ps->current_node)
            dst = &ps->current_node->comment;
        else if (ps->state == PARSE_MESSAGE_COMMENT && ps->current_message)
            dst = &ps->current_message->comment;
        else if (ps->state == PARSE_SIGNAL_COMMENT && ps->current_signal)
            dst = &ps->current_signal->comment;
        else if (ps->state == PARSE_GLOBAL_COMMENT && db->global_comments.n > 0)
            dst = (char **)&db->global_comments.a[db->global_comments.n - 1];

        if (dst)
        {
            db_str_append(dst, "\n");
            db_str_append(dst, part);
        }

        free(part);

        if (is_end)
        {
            ps->state = PARSE_NONE;
            ps->current_node    = NULL;
            ps->current_message = NULL;
            ps->current_signal  = NULL;
        }
    }
}

static void parse_attribute_definition(parser_t *ps, const char *line, can_database_t *db)
{
    can_database_attribute_t *attribute = can_database_attribute_create();
    attribute->target = CAN_DB_TARGET_NONE;

    const char *p = line;
    skip_ws(&p);

    bool ok = consume_literal(&p, "BA_DEF_");
    skip_ws(&p);

    if (ok && *p != '"')
    {
        if      (consume_keyword(&p, "BU_")) attribute->target = CAN_DB_TARGET_NODE;
        else if (consume_keyword(&p, "BO_")) attribute->target = CAN_DB_TARGET_MESSAGE;
        else if (consume_keyword(&p, "SG_")) attribute->target = CAN_DB_TARGET_SIGNAL;
        else if (consume_keyword(&p, "EV_")) attribute->target = CAN_DB_TARGET_NONE;
        else ok = false;

        skip_ws(&p);
    }

    ok = ok && consume_char(&p, '"');
    char *name = ok ? read_until(&p, '"') : NULL;
    ok = ok && consume_char(&p, '"');
    skip_ws(&p);

    char *rest = ok ? db_strdup(p) : NULL;
    if (rest)
    {
        rstrip(rest);
        size_t n = strlen(rest);
        if (n > 0 && rest[n - 1] == ';') { rest[n - 1] = '\0'; rstrip(rest); }
    }

    if (!ok || !name || name[0] == '\0' || !rest || rest[0] == '\0')
    {
        set_error(ps, "Unable to parse BA_DEF_ line: %s", line);
    }
    else
    {
        db_str_set(&attribute->name, name);

        const char *rp = rest;
        char *type_str = read_word(&rp);

        if (!type_str)
        {
            set_error(ps, "Unable to parse BA_DEF_ line: %s", line);
        }
        else if (token_eq(type_str, "INT") || token_eq(type_str, "FLOAT") || token_eq(type_str, "HEX"))
        {
            attribute->type = token_eq(type_str, "INT")   ? CAN_DB_ATTR_INT
                            : token_eq(type_str, "FLOAT") ? CAN_DB_ATTR_FLOAT
                            :                               CAN_DB_ATTR_HEX;

            skip_ws(&rp);
            char *min_tok = read_nonspace(&rp);
            skip_ws(&rp);
            char *max_tok = read_nonspace(&rp);

            if (min_tok && max_tok)
            {
                db_str_set(&attribute->min, min_tok);
                db_str_set(&attribute->max, max_tok);
            }

            free(min_tok);
            free(max_tok);
        }
        else if (token_eq(type_str, "STRING"))
        {
            attribute->type = CAN_DB_ATTR_STRING;
        }
        else if (token_eq(type_str, "ENUM"))
        {
            attribute->type = CAN_DB_ATTR_ENUM;

            const char *q = rest;
            while ((q = strchr(q, '"')) != NULL)
            {
                const char *end = strchr(q + 1, '"');
                if (!end) break;
                db_array_add(&attribute->enum_values, db_strndup(q + 1, (size_t)(end - q - 1)));
                q = end + 1;
            }
        }
        else
        {
            set_error(ps, "Invalid attribute type '%s' in line: %s", type_str, line);
        }

        free(type_str);
    }

    free(name);
    free(rest);

    db_array_add(&db->attributes, attribute);
}

static void parse_attribute_default(parser_t *ps, const char *line, can_database_t *db)
{
    const char *p = line;
    skip_ws(&p);

    bool ok = consume_literal(&p, "BA_DEF_DEF_");
    skip_ws(&p);

    ok = ok && consume_char(&p, '"');
    char *name = ok ? read_until(&p, '"') : NULL;
    ok = ok && consume_char(&p, '"');
    skip_ws(&p);

    char *value = NULL;
    if (ok)
    {
        if (*p == '"')
        {
            p++;
            value = read_until(&p, '"');
            ok = consume_char(&p, '"');
        }
        else
        {
            value = read_value_token(&p);
            ok = value != NULL;
        }
    }

    if (!ok || !name)
    {
        set_error(ps, "Unable to parse BA_DEF_DEF_ line: %s", line);
    }
    else
    {
        can_database_attribute_t *attribute = find_attribute(db, name);
        if (attribute)
        {
            db_str_set(&attribute->default_value, db_str(value));
        }
        else
        {
            set_error(ps, "Could not find attribute '%s' for BA_DEF_DEF_ line: %s", name, line);
        }
    }

    free(name);
    free(value);
}

static void parse_attribute_assignment(parser_t *ps, const char *line, can_database_t *db)
{
    const char *p = line;
    skip_ws(&p);

    bool ok = consume_literal(&p, "BA_");
    skip_ws(&p);

    ok = ok && consume_char(&p, '"');
    char *name = ok ? read_until(&p, '"') : NULL;
    ok = ok && consume_char(&p, '"');

    int   target = 0;
    char *arg1   = NULL;
    char *arg2   = NULL;

    if (ok)
    {
        const char *save = p;
        skip_ws(&p);

        if      (consume_keyword(&p, "BU_")) target = 1;
        else if (consume_keyword(&p, "BO_")) target = 2;
        else if (consume_keyword(&p, "SG_")) target = 3;
        else p = save;

        if (target)
        {
            skip_ws(&p);
            arg1 = read_nonspace(&p);
            skip_ws(&p);
            if (target == 3)
            {
                arg2 = read_nonspace(&p);
                skip_ws(&p);
            }
        }
    }

    char *value = NULL;
    if (ok)
    {
        skip_ws(&p);
        if (*p == '"')
        {
            p++;
            value = read_until(&p, '"');
            ok = consume_char(&p, '"');
        }
        else
        {
            value = read_value_token(&p);
            ok = value != NULL;
        }
    }

    if (!ok || !name || (target && !arg1) || (target == 3 && !arg2))
    {
        set_error(ps, "Unable to parse BA_ line: %s", line);
        free(name);
        free(arg1);
        free(arg2);
        free(value);
        return;
    }

    if (target == 0)
    {
        can_database_attribute_value_t *av = can_database_attribute_value_create();
        db_str_set(&av->attribute, name);
        db_str_set(&av->value, value);
        db_array_add(&db->global_attribute_values, av);
    }
    else if (target == 1)
    {
        can_database_node_t *node = find_node(db, arg1);
        if (node)
        {
            can_database_attribute_value_t *av = can_database_attribute_value_create();
            db_str_set(&av->attribute, name);
            db_str_set(&av->value, value);
            db_array_add(&node->attribute_values, av);
        }
        else
        {
            set_error(ps, "Could not find node '%s' for BA_ line: %s", arg1, line);
        }
    }
    else if (target == 2)
    {
        uint32_t id = 0;
        if (!parse_uint32(arg1, &id))
        {
            set_error(ps, "Invalid message ID '%s' in line: %s", arg1, line);
        }
        else
        {
            can_database_message_t *message = find_message(db, id);
            if (message)
            {
                can_database_attribute_value_t *av = can_database_attribute_value_create();
                db_str_set(&av->attribute, name);
                db_str_set(&av->value, value);
                db_array_add(&message->attribute_values, av);
            }
            else
            {
                set_error(ps, "Could not find message ID %u for BA_ line: %s", id, line);
            }
        }
    }
    else /* target == 3 */
    {
        uint32_t id = 0;
        if (!parse_uint32(arg1, &id))
        {
            set_error(ps, "Invalid message ID '%s' in line: %s", arg1, line);
        }
        else
        {
            can_database_message_t *message = find_message(db, id);
            can_database_signal_t  *signal  = message ? find_signal(message, arg2) : NULL;
            if (signal)
            {
                can_database_attribute_value_t *av = can_database_attribute_value_create();
                db_str_set(&av->attribute, name);
                db_str_set(&av->value, value);
                db_array_add(&signal->attribute_values, av);
            }
            else
            {
                set_error(ps, "Could not find signal '%s' in message ID %u for BA_ line: %s",
                          arg2, id, line);
            }
        }
    }

    free(name);
    free(arg1);
    free(arg2);
    free(value);
}

/* Parses zero or more `<int> "<string>"` pairs into the table, stopping at ';',
   end-of-line, or the first token that isn't a pair. */
static void parse_value_pairs(const char **p, can_database_value_table_t *table)
{
    for (;;)
    {
        skip_ws(p);
        if (**p == ';' || **p == '\0') break;

        char *key_str = read_int_literal(p);
        if (!key_str) break;

        skip_ws(p);
        if (**p != '"') { free(key_str); break; }
        (*p)++;

        char *value = read_until(p, '"');
        if (!consume_char(p, '"')) { free(key_str); free(value); break; }

        long key;
        if (parse_long(key_str, &key))
        {
            can_database_value_table_set(table, key, value);
        }

        free(key_str);
        free(value);
    }
}

static void parse_value_table(parser_t *ps, const char *line, can_database_t *db)
{
    const char *p = line;
    skip_ws(&p);

    bool ok = consume_literal(&p, "VAL_TABLE_");
    skip_ws(&p);

    char *name = ok ? read_word(&p) : NULL;

    if (!ok || !name)
    {
        set_error(ps, "Unable to parse VAL_TABLE_ line: %s", line);
        free(name);
        return;
    }

    can_database_value_table_t *table = can_database_value_table_create();
    db_str_set(&table->name, name);
    parse_value_pairs(&p, table);
    db_array_add(&db->global_value_tables, table);

    free(name);
}

static void parse_value_table_assignment(parser_t *ps, const char *line, can_database_t *db)
{
    const char *p = line;
    skip_ws(&p);

    bool ok = consume_literal(&p, "VAL_");
    skip_ws(&p);

    char *id_str = ok ? read_digits(&p) : NULL;
    skip_ws(&p);
    char *signal_name = ok ? read_word(&p) : NULL;

    if (!ok || !id_str || !signal_name)
    {
        set_error(ps, "Unable to parse VAL_ line: %s", line);
        free(id_str);
        free(signal_name);
        return;
    }

    uint32_t id = 0;
    if (!parse_uint32(id_str, &id))
    {
        set_error(ps, "Invalid message ID '%s' in line: %s", id_str, line);
        free(id_str);
        free(signal_name);
        return;
    }

    can_database_message_t *message = find_message(db, id);
    can_database_signal_t  *signal  = message ? find_signal(message, signal_name) : NULL;

    if (!signal)
    {
        set_error(ps, "Could not find signal '%s' in message ID %u for VAL_ line: %s",
                  signal_name, id, line);
        free(id_str);
        free(signal_name);
        return;
    }

    skip_ws(&p);

    /* A local table is one or more `<int> "<string>"` pairs; otherwise a single
       word names a global value table. Distinguish by peeking for a key+quote. */
    const char *peek = p;
    char       *probe = read_int_literal(&peek);
    bool        is_pairs = false;
    if (probe)
    {
        skip_ws(&peek);
        is_pairs = (*peek == '"');
    }
    free(probe);

    if (is_pairs)
    {
        can_database_value_table_t *table = can_database_value_table_create();
        parse_value_pairs(&p, table);
        db_array_add(&signal->local_value_tables, table);
    }
    else
    {
        char *table_name = read_word(&p);
        if (table_name)
        {
            db_array_add(&signal->global_value_tables, db_strdup(table_name));
        }
        free(table_name);
    }

    free(id_str);
    free(signal_name);
}

static void parse_signal_value_type(parser_t *ps, const char *line, can_database_t *db)
{
    const char *p = line;
    skip_ws(&p);

    bool ok = consume_literal(&p, "SIG_VALTYPE_");
    skip_ws(&p);

    char *id_str = ok ? read_digits(&p) : NULL;
    skip_ws(&p);
    char *signal_name = ok ? read_word(&p) : NULL;
    skip_ws(&p);

    ok = ok && id_str && signal_name && consume_char(&p, ':');
    skip_ws(&p);

    char *type_str = ok ? read_digits(&p) : NULL;
    skip_ws(&p);
    ok = ok && type_str && consume_char(&p, ';');

    if (!ok)
    {
        set_error(ps, "Unable to parse SIG_VALTYPE_ line: %s", line);
        free(id_str);
        free(signal_name);
        free(type_str);
        return;
    }

    uint32_t id = 0;
    if (!parse_uint32(id_str, &id))
    {
        set_error(ps, "Invalid message ID '%s' in line: %s", id_str, line);
        free(id_str);
        free(signal_name);
        free(type_str);
        return;
    }

    can_database_signal_type_t signal_type = CAN_DB_SIGNAL_UNSIGNED;
    bool override_found = false;

    if (token_eq(type_str, "0"))
    {
        /* default signal type — nothing to override */
    }
    else if (token_eq(type_str, "1"))
    {
        signal_type = CAN_DB_SIGNAL_FLOAT;
        override_found = true;
    }
    else if (token_eq(type_str, "2"))
    {
        signal_type = CAN_DB_SIGNAL_DOUBLE;
        override_found = true;
    }
    else
    {
        set_error(ps, "Invalid value type '%s' in line: %s", type_str, line);
    }

    if (override_found && ps->error[0] == '\0')
    {
        can_database_message_t *message = find_message(db, id);
        can_database_signal_t  *signal  = message ? find_signal(message, signal_name) : NULL;

        if (signal)
        {
            signal->signal_type = signal_type;
        }
        else
        {
            set_error(ps, "Could not find signal '%s' in message ID %u for SIG_VALTYPE_ line: %s",
                      signal_name, id, line);
        }
    }

    free(id_str);
    free(signal_name);
    free(type_str);
}

static void parse_environment_variable(parser_t *ps, const char *line, can_database_t *db)
{
    /* EV_ <Name> : <Type> [<Min>|<Max>] "<Unit>" <Initial> <EvId> <AccessType>
       <AccessNodes> ;   (Type: 0 = integer, 1 = float, 2 = string) */
    can_database_environment_variable_t *env = can_database_environment_variable_create();

    const char *p = line;
    skip_ws(&p);

    bool ok = consume_literal(&p, "EV_");
    skip_ws(&p);

    char *name = ok ? read_word(&p) : NULL;
    ok = ok && name != NULL;
    skip_ws(&p);
    ok = ok && consume_char(&p, ':');
    skip_ws(&p);

    char *type_str = ok ? read_digits(&p) : NULL;
    ok = ok && type_str != NULL;
    skip_ws(&p);

    ok = ok && consume_char(&p, '[');
    char *min_str = ok ? read_until(&p, '|') : NULL;
    ok = ok && min_str != NULL && consume_char(&p, '|');
    char *max_str = ok ? read_until(&p, ']') : NULL;
    ok = ok && max_str != NULL && consume_char(&p, ']');
    skip_ws(&p);

    ok = ok && consume_char(&p, '"');
    char *unit = ok ? read_until(&p, '"') : NULL;
    ok = ok && unit != NULL && consume_char(&p, '"');
    skip_ws(&p);

    char *initial     = ok ? read_nonspace(&p) : NULL;
    ok = ok && initial != NULL;
    skip_ws(&p);

    char *ev_id       = ok ? read_nonspace(&p) : NULL;
    ok = ok && ev_id != NULL;
    skip_ws(&p);

    char *access_type = ok ? read_nonspace(&p) : NULL;
    ok = ok && access_type != NULL;
    skip_ws(&p);

    char *nodes = ok ? read_until(&p, ';') : NULL;   /* access nodes, up to ';' */
    if (nodes) trim_inplace(nodes);
    ok = ok && nodes != NULL && nodes[0] != '\0';

    if (!ok || !name)
    {
        set_error(ps, "Unable to parse EV_ line: %s", line);
    }
    else
    {
        db_str_set(&env->name, name);

        if      (token_eq(type_str, "1")) env->type = CAN_DB_ATTR_FLOAT;
        else if (token_eq(type_str, "2")) env->type = CAN_DB_ATTR_STRING;
        else                              env->type = CAN_DB_ATTR_INT;

        db_str_set(&env->min, min_str);
        db_str_set(&env->max, max_str);
        db_str_set(&env->unit, unit);
        db_str_set(&env->initial_value, initial);
        db_str_set(&env->ev_id, ev_id);
        db_str_set(&env->access_type, access_type);
        db_str_set(&env->node, nodes);
    }

    free(name);
    free(type_str);
    free(min_str);
    free(max_str);
    free(unit);
    free(initial);
    free(ev_id);
    free(access_type);
    free(nodes);

    db_array_add(&db->environment_variables, env);
}

/***************************************************************
** MARK: STATIC FUNCTIONS — lookups
***************************************************************/

static can_database_node_t *find_node(can_database_t *db, const char *name)
{
    for (size_t i = 0; i < db->nodes.n; i++)
    {
        can_database_node_t *node = DB_ARRAY_AT(db->nodes, can_database_node_t, i);
        if (node->name && strcmp(node->name, name) == 0) return node;
    }
    return NULL;
}

static can_database_message_t *find_message(can_database_t *db, uint32_t id)
{
    for (size_t i = 0; i < db->messages.n; i++)
    {
        can_database_message_t *message = DB_ARRAY_AT(db->messages, can_database_message_t, i);
        if (message->id == id) return message;
    }
    return NULL;
}

static can_database_signal_t *find_signal(can_database_message_t *message, const char *name)
{
    for (size_t i = 0; i < message->signals.n; i++)
    {
        can_database_signal_t *signal = DB_ARRAY_AT(message->signals, can_database_signal_t, i);
        if (signal->name && strcmp(signal->name, name) == 0) return signal;
    }
    return NULL;
}

static can_database_attribute_t *find_attribute(can_database_t *db, const char *name)
{
    for (size_t i = 0; i < db->attributes.n; i++)
    {
        can_database_attribute_t *attribute = DB_ARRAY_AT(db->attributes, can_database_attribute_t, i);
        if (attribute->name && strcmp(attribute->name, name) == 0) return attribute;
    }
    return NULL;
}
