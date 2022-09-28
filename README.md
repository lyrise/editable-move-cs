# emv - Editable file and directory moving operations

[![Nuget](https://img.shields.io/nuget/v/EditableMove)](https://www.nuget.org/packages/EditableMove)

## Install

```sh
dotnet tool install --global EditableMove
```

## Usage

### Find files or directories

```sh
# directory only (depth: 1)
emv find -d -r 1

# file and directory (depth: 2)
emv find -fd -r 2
```

### Edit the path or name of finded files or directories

```sh
vim .emv/new
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
