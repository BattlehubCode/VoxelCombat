call "%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"
msbuild.exe "%~dp0\Server\VoxelCombatServerApp\VoxelCombatServerApp.sln"

"%ProgramFiles(x86)%\IIS Express\iisexpress"  /path:"%~dp0Server\VoxelCombatServerApp\VoxelCombatServerApp" /port:7777
