@echo off
gcc -m32 program_2.c -o out.exe
out.exe
echo %errorlevel%