@echo off
title HabitaCont
cd /d "%~dp0"

echo Iniciando HabitaCont...
start "" http://localhost:5260
dotnet run --launch-profile http

echo.
echo El sistema se detuvo.
pause