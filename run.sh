#!/bin/bash

dotnet run --project ./TangoFlexCompiler/TangoFlexCompiler.csproj -- ./TangoFlexSrc/test.tf -o ./outputs/test1.asm --cc SysV --backend asm --bits 64