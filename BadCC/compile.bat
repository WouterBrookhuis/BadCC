@echo off
"bin/debug/badcc" program_2.c
gcc -m32 program_2.s -o out_bcc.exe
out_bcc.exe
echo Got %errorlevel%
gcc -m32 program_2.c -o out_gcc.exe
gcc -m32 -S program_2.c -o program_2_gcc.s
out_gcc.exe
echo Expected %errorlevel%