#include <stdlib.h>
#include <stdio.h>
#include <stdarg.h>
#include <limits>

#include "leveldb/extensions.h"
#include "leveldb/env.h"

extern "C" 
{
  void leveldb_free(char* value)
  {
    if (value)
      free(value);
  }
}


namespace extensions {
  class DelegateLogger : public leveldb::Logger {
  public:
    explicit DelegateLogger(leveldb_log_string_fn logger)
    {
      this->logger = logger;
    }

    ~DelegateLogger() {}

  public:
    void Logv(const char* format, va_list ap)
    {
      // avoid dynamic allocation in most cases
      char buf[128];
      
      // TODO: how should we treat OOM? Ignore, or bail?

      // measure the required buffer size.
      //  msdn reference for printf variants on VC: http://msdn.microsoft.com/en-us/library/0zf95wk0%28v=VS.100%29.aspx
      size_t sz = _vscprintf(format, ap)+1;
      char* bufp = buf;
      if (sz > _countof(buf)) {
        bufp = (char*)malloc(sz);
        if (!bufp) abort();
      }
      int n = 
#if defined(_MSC_VER)
        vsprintf_s(bufp, sz, format, ap);
#else
        vsprintf(bufp, format, ap);
#endif
      if (n < 0) abort();

      logger(bufp);

      if (bufp != buf) free(bufp);
    }

  private:
    leveldb_log_string_fn logger;
  };
}

extern "C" 
{
  // note: duplicated definition
  struct leveldb_logger_t {
    leveldb::Logger* rep;
  };

  leveldb_logger_t* leveldb_logger_create(leveldb_log_string_fn logger)
  {
    leveldb_logger_t* result = new leveldb_logger_t;
    result->rep =  new extensions::DelegateLogger(logger);
    return result;
  }

  void leveldb_logger_destroy(leveldb_logger_t* logger)
  {
    delete logger->rep;
    delete logger;
  }
}
