@echo off
REM Shortcut for "dotnet ef" that always points at the right projects, so
REM you never have to retype --project/--startup-project.
REM
REM Usage examples (run from the backend\ folder):
REM   ef migrations add AddSomethingSchema
REM   ef migrations remove
REM   ef migrations list
REM   ef database update
REM
REM %* forwards every argument you typed after "ef" straight through to
REM "dotnet ef", then the two fixed flags are appended after it.
dotnet ef %* --project src/Ghuri.Infrastructure --startup-project src/Ghuri.Api
