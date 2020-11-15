# emv - Editable file and directory moving operations 

[![Nuget](https://img.shields.io/nuget/v/EditableMove)](https://www.nuget.org/packages/EditableMove)

## Install

```sh
dotnet tool install --global EditableMove
```

## Usage

### Find files or directories

```sh
# file only
emv find "./**/*"

# directory only
emv find -d "./**/*"

# file and directory
emv find -fd "./**/*"
```

### Edit the path or name of finded files or directories

```sh
nano ./.emv/new
```

### Execute files or directories move

```cs
emv move
```

### Undo files or directories move

```cs
emv move -u
```

### Delete working directory

```cs
emv clean
```
