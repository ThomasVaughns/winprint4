# Minimum CMake version
cmake_minimum_required(VERSION 3.15)

# Project name
project(WinPrintDotNetWPF)

# Set the path to the .csproj file
set(WINPRINT_CSPROJ "${CMAKE_CURRENT_SOURCE_DIR}/WinPrint.csproj")

# Custom target to build the .NET project
add_custom_target(
    build_winprint ALL
    COMMAND dotnet build ${WINPRINT_CSPROJ} --configuration Release
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    COMMENT "Building WinPrint WPF app with dotnet build"
)

# Custom target to run the .NET project
add_custom_target(
    run_winprint
    COMMAND dotnet run --project ${WINPRINT_CSPROJ} --configuration Release
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    COMMENT "Running WinPrint WPF app with dotnet run"
    DEPENDS build_winprint
)
