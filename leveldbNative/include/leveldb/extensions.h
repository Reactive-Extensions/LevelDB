
#include "c.h"

#ifndef STORAGE_LEVELDB_INCLUDE_EXTENSIONS_H_
#define STORAGE_LEVELDB_INCLUDE_EXTENSIONS_H_

#ifdef __cplusplus
extern "C" {
#endif

typedef void (*leveldb_log_string_fn)(const char* message);

// frees memory allocated by leveldb_get() and others
void leveldb_free(char* value);


leveldb_logger_t* leveldb_logger_create(leveldb_log_string_fn logger);
void leveldb_logger_destroy(leveldb_logger_t* logger);


#ifdef __cplusplus
}  /* end extern "C" */
#endif

#endif  /* STORAGE_LEVELDB_INCLUDE_EXTENSIONS_H_ */
