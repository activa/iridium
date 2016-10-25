LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

LOCAL_MODULE    := sqlite3_emb
LOCAL_MODULE_FILENAME    := sqlite3_emb
LOCAL_CFLAGS := -O -DNDEBUG 
LOCAL_CFLAGS += -DSQLITE_DEFAULT_FOREIGN_KEYS=1 
LOCAL_CFLAGS += -DSQLITE_ENABLE_FTS3_PARENTHESIS 
LOCAL_CFLAGS += -DSQLITE_ENABLE_FTS4 
LOCAL_CFLAGS += -DSQLITE_ENABLE_COLUMN_METADATA
LOCAL_CFLAGS += -DSQLITE_ENABLE_JSON1
LOCAL_CFLAGS += -DSQLITE_TEMP_STORE=3

LOCAL_SRC_FILES = sqlite3.c


#ifeq ($(TARGET_ARCH), arm)
#	LOCAL_CFLAGS += -DPACKED="__attribute__ ((packed))"
#else
#	LOCAL_CFLAGS += -DPACKED=""
#endif

LOCAL_C_INCLUDES += $(LOCAL_PATH)

include $(BUILD_SHARED_LIBRARY)
