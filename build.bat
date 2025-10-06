@echo off
echo 🏗️ Building Oil Trading Solution...
echo.

echo Step 1: Cleaning solution thoroughly...
dotnet clean --verbosity quiet
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
for /r %%i in (bin obj) do if exist "%%i" rmdir /s /q "%%i"
echo ✅ Clean completed
echo.

echo Step 2: Clearing NuGet cache...
dotnet nuget locals all --clear
echo ✅ NuGet cache cleared
echo.

echo Step 3: Restoring packages with force...
dotnet restore --force --verbosity normal
echo.

if %ERRORLEVEL% EQU 0 (
    echo Step 4: Building solution...
    dotnet build --configuration Release --no-restore --verbosity normal
    echo.
    
    if %ERRORLEVEL% EQU 0 (
        echo ✅ Build completed successfully!
        echo.
        echo 🚀 You can now run the API:
        echo   cd src\OilTrading.Api
        echo   dotnet run
        echo.
        echo 📖 Open Swagger: https://localhost:5001/swagger
    ) else (
        echo ❌ Build failed with errors.
        echo Check the output above for details.
    )
) else (
    echo ❌ Package restore failed.
    echo Check your internet connection and NuGet configuration.
)

pause