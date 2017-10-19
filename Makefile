all:
	nuget restore
	msbuild /verbosity:diag *.sln
