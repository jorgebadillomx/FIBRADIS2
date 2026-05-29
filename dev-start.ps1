Get-Process -Name *dotnet*, *node*, *WindowsTerminal* -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

$root = $PSScriptRoot

$wtArgs = "new-tab --title API --startingDirectory `"$root`" pwsh -NoExit -Command `"dotnet run --project src/Server/Api/Api.csproj`" " +
        "; split-pane --title Main --startingDirectory `"$root`" pwsh -NoExit -Command `"npm run dev:main`" " +
        "; split-pane --title Ops --startingDirectory `"$root`" pwsh -NoExit -Command `"npm run dev:ops`""

$beforePids = Get-Process -Name "WindowsTerminal" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id
Start-Process wt -ArgumentList $wtArgs
Start-Sleep -Seconds 2

$newPid = (Get-Process -Name "WindowsTerminal" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id) |
          Where-Object { $_ -notin $beforePids } |
          Select-Object -First 1

if ($newPid) { $newPid | Set-Content "$root\.dev-wt-pid" }
