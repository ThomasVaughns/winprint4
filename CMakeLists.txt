cmake_minimum_required(VERSION 3.15)
project(WinPrint)

set(SOLUTION ${CMAKE_SOURCE_DIR}/src/WinPrint.sln)
set(DOTNET_CONFIGURATION Release)
set(DOTNET_PLATFORM x64)

# Restore NuGet packages
add_custom_target(restore ALL
    COMMAND dotnet restore ${SOLUTION}
    WORKING_DIRECTORY ${CMAKE_SOURCE_DIR}
    COMMENT "Restoring NuGet packages")

# Build the solution
add_custom_target(winprint ALL
    COMMAND dotnet build ${SOLUTION} -c ${DOTNET_CONFIGURATION} -p:Platform=${DOTNET_PLATFORM} -p:WarningAsError=false
    WORKING_DIRECTORY ${CMAKE_SOURCE_DIR}
    DEPENDS restore
    COMMENT "Building WinPrint")
