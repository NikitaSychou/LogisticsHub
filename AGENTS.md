## Token-saving command rules

Use RTK for noisy shell commands.

Prefer:
- `rtk git status`
- `rtk git diff`
- `rtk grep -r --exclude-dir=bin --exclude-dir=obj "<pattern>" .`
- `rtk read <file>`
- `rtk err dotnet build .\LogisticsHub.sln --no-restore --nologo`
- `rtk test dotnet test .\LogisticsHub.sln --no-build --nologo`

Do not use raw `git diff`, `cat`, `grep`, `dotnet build`, or `dotnet test` unless RTK is unavailable.
Keep final answers short and do not include full diffs or large command outputs.
