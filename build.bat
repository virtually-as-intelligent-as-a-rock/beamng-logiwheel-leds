@echo off
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (echo csc.exe not found & pause & exit /b 1)
if not exist LogitechSteeringWheelEnginesWrapper.dll (
  echo Missing LogitechSteeringWheelEnginesWrapper.dll
  echo Download the Logitech Steering Wheel SDK and copy Lib\GameEnginesWrapper\x64\LogitechSteeringWheelEnginesWrapper.dll next to this file.
  pause & exit /b 1
)
"%CSC%" /nologo /platform:x64 /target:winexe /out:BeamLeds.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll BeamLeds.cs
if errorlevel 1 (echo BUILD FAILED & pause & exit /b 1)
echo Done: BeamLeds.exe
pause
