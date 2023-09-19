
Gitxtras
---------------------

gitxtras are git extensions coded in dotnet

### What it does

The CLI executable will help do special operations in a local git repository.


### `fixeol` - fix mixed line endings

Sometimes you want to keep mixed line endings in a file. This allows preserving the history. But your editor/IDE – typically Rider – might have a tendency to fully convert your text files. 

This tool detects undesired changes and fixes the files. This way you can check and fix before commiting your changes.

```bash
GitExtras fixeol [-e]          # operates on pending changes
GitExtras fixeol [-e] [path]+  # operates on specified files
```

```
USAGE:
    GitExtras fixeol [path] [OPTIONS]

ARGUMENTS:
    [path]

OPTIONS:
    -e, --execute
```


### Install on Windows

- pull  
  `git pull`
- compile me  
  `dotnet build src/GitExtras/GitExtras.csproj -c Release`
- verify me  
  `src/GitExtras/bin/Release/net6.0/GitExtras.exe`
- open .bashrc  
  `vi ~/.bashrc`
- and insert an alias  
  `alias gitextras=~/dev/github/sandrock/gitxtras/tools/src/GitExtras/bin/Release/net6.0/GitExtras.exe`
- reload your shell configuration


### Install on Linux

- pull  
  `git pull`
- compile me  
  `dotnet build src/GitExtras/GitExtras.csproj -c Release`
- verify me  
  `src/GitExtras/bin/Release/net6.0/GitExtras`
- open .bashrc  or .zsh_aliases
  `vi ~/.bashrc`
- and insert an alias  
  `alias gitextras=~/dev/github/sandrock/gitxtras/tools/src/GitExtras/bin/Release/net6.0/GitExtras`
- reload your shell configuration



### Notes

Why not build using [.NET Native](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=net7)? Nop, we cannot create platform-independant builds with AOT :(




