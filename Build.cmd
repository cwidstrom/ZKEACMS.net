@echo off
WHERE /Q dotnet
IF %ERRORLEVEL% NEQ 0 (
ECHO dotnet core sdk was not find, please install the latest sdk at first.
@pause
start https://www.microsoft.com/net/download/windows
exit
)
@echo Build starting...
dotnet build ZKEACMS.sln