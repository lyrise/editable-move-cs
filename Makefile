dotnet-run:
	dotnet run --project ./src/VRename/

dotnet-pack:
	dotnet pack ./src/VRename

.PHONY: dotnet-run dotnet-pack
