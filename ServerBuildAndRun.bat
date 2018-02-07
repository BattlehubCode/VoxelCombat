
REM set msbuild.exe=
REM for /D %%D in (%SYSTEMROOT%\Microsoft.NET\Framework\v4*) do set msbuild.exe=%%D\MSBuild.exe

REM if not defined msbuild.exe echo error: can't find MSBuild.exe & goto :eof
REM if not exist "%msbuild.exe%" echo error: %msbuild.exe%: not found & goto :eof

REM msbuild.exe "E:\Development\GitHub\VoxelCombat\Server\VoxelCombatServerApp\VoxelCombatServerApp.sln" 
REM iisexpress /path:E:\Development\GitHub\VoxelCombat\Server\VoxelCombatServerApp\VoxelCombatServerApp /port:7777
REM start "" http://localhost:7777

call "%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"
msbuild.exe "%~dp0\Server\VoxelCombatServerApp\VoxelCombatServerApp.sln"

"%ProgramFiles(x86)%\IIS Express\iisexpress"  /path:"%~dp0Server\VoxelCombatServerApp\VoxelCombatServerApp" /port:7777
