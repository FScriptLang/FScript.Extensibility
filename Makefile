.PHONY: build test pack verify-api update-api verify-changelog release-prepare
version ?= 2.0.0
build:
	dotnet build FScript.Extensibility.sln -c Release

test:
	dotnet test FScript.Extensibility.sln -c Release
	python3 .github/scripts/test_release.py

pack:
	dotnet pack src/FScript.Extensibility/FScript.Extensibility.fsproj -c Release -p:Version=$(version) -o .out/packages

verify-api: build
	dotnet fsi .github/scripts/api.fsx src/FScript.Extensibility/bin/Release/netstandard2.1/FScript.Extensibility.dll api/PublicAPI.txt

update-api: build
	UPDATE_API=true dotnet fsi .github/scripts/api.fsx src/FScript.Extensibility/bin/Release/netstandard2.1/FScript.Extensibility.dll api/PublicAPI.txt

verify-changelog:
	python3 .github/scripts/changelog.py

release-prepare:
	python3 .github/scripts/release.py "$(version)" "$(dryrun)"
