
echo ###             restoring all NUGET Packages.                 ###
echo:
call external\json-index\.nuget\nuget.exe restore external\json-index\DotJEM.Json.Index.sln
call external\json-storage\.nuget\nuget.exe restore external\json-storage\DotJEM.Json.Storage.sln
call .nuget\nuget.exe restore NSW.sln