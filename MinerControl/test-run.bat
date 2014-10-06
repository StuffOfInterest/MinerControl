@echo off
echo Test batch file %1.
echo Mining to address %2.
echo Started mining %1 to address %2 >> log.txt
time /t >> log.txt
TestMiner
pause
