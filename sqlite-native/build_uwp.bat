SET SQLITE_OPTIONS=/D SQLITE_ENABLE_COLUMN_METADATA /D SQLITE_ENABLE_FTS4 /D "SQLITE_API=__declspec(dllexport)" /D SQLITE_WIN32_FILEMAPPING_API=1
SET COMPILER_OPTIONS=/MT /Zi /W1 /WX- /sdl- /O2 /Oi /Oy- /EHsc /D NDEBUG /D _USRDLL /D _WINDLL /Gm- /GS /Gy /fp:precise /Zc:wchar_t /Zc:forScope /Gd /TC /analyze-
SET LINKER_OPTIONS=/APPCONTAINER /SUBSYSTEM:CONSOLE /OPT:REF /OPT:ICF /TLBID:1 /WINMD:NO /DYNAMICBASE /NXCOMPAT /DLL /MANIFEST /MANIFESTUAC:"level='asInvoker' uiAccess='false'" /manifest:embed

SET OLD_PATH=%PATH%

md .\bin
md .\bin\uwp
md .\bin\win
md .\bin\uwp\win10-arm
md .\bin\uwp\win10-x86
md .\bin\uwp\win10-x64
md .\bin\win\x86
md .\bin\win\x64

REM SET VCVARSBAT="C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\VC\Auxiliary\Build\vcvarsall.bat"

SET VCVARSBAT="C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\vcvarsall.bat"

set LIB=
set LIBPATH=
set INCLUDE=
set PATH=%OLD_PATH%

call %VCVARSBAT% x86_arm

cl src\sqlite3.c /c %SQLITE_OPTIONS% %COMPILER_OPTIONS% /D _ARM_WINAPI_PARTITION_DESKTOP_SDK_AVAILABLE=1 /Fosqlite_emb.obj
link /out:bin\uwp\win10-arm\sqlite_emb.dll sqlite_emb.obj /IMPLIB:bin\uwp\win10-arm\sqlite_emb.lib /MACHINE:ARM %LINKER_OPTIONS%
del *.obj

set LIB=
set LIBPATH=
set INCLUDE=
set PATH=%OLD_PATH%

call %VCVARSBAT% x86

cl src\sqlite3.c /c %SQLITE_OPTIONS% %COMPILER_OPTIONS% /Fosqlite_emb.obj
link /out:bin\uwp\win10-x86\sqlite_emb.dll sqlite_emb.obj /IMPLIB:bin\uwp\win10-x86\sqlite_emb.lib /MACHINE:X86 %LINKER_OPTIONS%

set LIB=
set LIBPATH=
set INCLUDE=
set PATH=%OLD_PATH%

call %VCVARSBAT% x86_amd64

del *.obj

cl src\sqlite3.c /c %SQLITE_OPTIONS% %COMPILER_OPTIONS% /Fosqlite_emb.obj
link /out:bin\uwp\win10-x64\sqlite_emb.dll sqlite_emb.obj /IMPLIB:bin\uwp\win10-x64\sqlite_emb.lib /MACHINE:X64 %LINKER_OPTIONS%

SET COMPILER_OPTIONS=/MT /Zi /W1 /WX- /sdl- /O2 /Oi /Oy- /EHsc /D NDEBUG /D _USRDLL /D _WINDLL /Gm- /GS /Gy /fp:precise /Zc:wchar_t /Zc:forScope /Gd /TC /analyze- /D _USING_V110_SDK71_
SET LINKER_OPTIONS=/SUBSYSTEM:CONSOLE /OPT:REF /OPT:ICF /TLBID:1 /WINMD:NO /DYNAMICBASE /NXCOMPAT /DLL /MANIFEST /MANIFESTUAC:"level='asInvoker' uiAccess='false'" /manifest:embed


set LIB=
set LIBPATH=
set INCLUDE=
set PATH=%OLD_PATH%

call %VCVARSBAT% x86

cl src\sqlite3.c /c %SQLITE_OPTIONS% %COMPILER_OPTIONS% /Fosqlite3.obj
link /out:bin\win\x86\sqlite3.dll sqlite3.obj /IMPLIB:bin\win\x86\sqlite3.lib /MACHINE:X86 %LINKER_OPTIONS%

del *.obj


set LIB=
set LIBPATH=
set INCLUDE=
set PATH=%OLD_PATH%

call %VCVARSBAT% x86_amd64

cl src\sqlite3.c /c %SQLITE_OPTIONS% %COMPILER_OPTIONS% /Fosqlite3.obj
link /out:bin\win\x64\sqlite3.dll sqlite3.obj /IMPLIB:bin\win\x64\sqlite3.lib /MACHINE:X64 %LINKER_OPTIONS%

del *.obj




set LIB=
set LIBPATH=
set INCLUDE=
set PATH=%OLD_PATH%
set OLD_PATH=

SET SQLITE_OPTIONS=
SET COMPILER_OPTIONS=
SET LINKER_OPTIONS=

del *.obj

del /s bin\*.exp
del /s bin\*.lib