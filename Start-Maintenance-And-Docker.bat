@echo off
setlocal

set "ROOT=%~dp0"
set "CONTAINER_NAME=campus-sql"
set "IMAGE=mcr.microsoft.com/mssql/server:2022-latest"
set "SA_PASSWORD=MakeItStrong!2026"
set "PORT=1433"

where docker >nul 2>&1
if errorlevel 1 (
    echo Docker is not installed or not on PATH.
    echo Install Docker Desktop for Windows, then run this script again.
    exit /b 1
)

:: Start or recreate the SQL Server container if needed.
docker ps -a --format "{{.Names}}" | findstr /I /C:"%CONTAINER_NAME%" >nul
if errorlevel 1 (
    echo Starting SQL Server container %CONTAINER_NAME%...
    docker run -d --name %CONTAINER_NAME% -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=%SA_PASSWORD%" -e "MSSQL_PID=Developer" -p %PORT%:1433 %IMAGE%
    if errorlevel 1 (
        echo Failed to start SQL Server container.
        exit /b 1
    )
) else (
    docker ps --format "{{.Names}}" | findstr /I /C:"%CONTAINER_NAME%" >nul
    if errorlevel 1 (
        echo Container %CONTAINER_NAME% exists but is stopped. Starting it...
        docker start %CONTAINER_NAME%
        if errorlevel 1 (
            echo Failed to start existing container %CONTAINER_NAME%.
            exit /b 1
        )
    ) else (
        echo Container %CONTAINER_NAME% is already running.
    )
)

:: Give SQL Server a moment to finish startup before opening the maintenance GUI.
for /L %%I in (1,1,20) do (
    timeout /t 1 /nobreak >nul
    docker logs %CONTAINER_NAME% 2>nul | findstr /I /C:"SQL Server is now ready" >nul
    if not errorlevel 1 goto :continue
)

:continue

echo SQL Server is available at localhost:%PORT% (user: sa, password: %SA_PASSWORD%)

echo Starting Campus System Maintenance Dashboard...
call "%ROOT%Maintenance\Run-MaintenanceMonitoring.bat"

endlocal
