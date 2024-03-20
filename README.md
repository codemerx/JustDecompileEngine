# JustDecompile Engine

Since the beginning of 2024, the original repository of JustDecompile’s decompilation engine is no longer publicly available. Progress Software owns the original JustDecompile and at the time of writing this, there is no official statement from Progress Software as to the fate of that original. Since there have been no changes whatsoever to the original JustDecompile ever since 2018 it is safe to assume it is effectively gone, though.

CodeMerx is the home of the team that created the original JustDecompile. So CodeMerx now picks up the development of JustDecompile introducing [CodemerxDecompile](https://github.com/codemerx/CodemerxDecompile). Same engine, same people, same promise - free, forever.

## About

This is the engine of the popular .NET decompiler [CodemerxDecompile](https://github.com/codemerx/CodemerxDecompile). C# is the only programming language used.

## Getting Started

- Clone the repository and open `src/JustDecompileEngine.sln` in an IDE of choice
- Set your startup project to ConsoleRunner (.NET Framework 4.7.2) or ConsoleRunnerDotNet6 (.NET 6)
- Enjoy

## Working with JustDecompile Engine

There are 2 main options to test changes made to the decompilation engine.

The first option is to use the rich console functionality the ConsoleRunner provides. The ConsoleRunner project is a console app that exposes the project generation feature and makes testing easy. When started it prints out all the available commands and switches. One can use that feature to see the results of the changes made to the engine.

> Note: There are 2 versions of the ConsoleRunner project - ConsoleRunner targeting .NET Framework 4.7.2 and ConsoleRunnerDotNet6 targeting .NET 6. Both versions work identically.

The second option is to use [CodemerxDecompile](https://github.com/codemerx/CodemerxDecompile). This repo is used as a submodule in [CodemerxDecompile](https://github.com/codemerx/CodemerxDecompile) and any changes made to the engine code can be seen in the UI in a matter of seconds.

## Contributions

We encourage and support an active, healthy community that accepts contributions from the public. We'd like you to be a part of that community.

## License

This project is [AGPL](https://github.com/codemerx/JustDecompileEngine/blob/master/COPYING) licensed. It includes [Mono.Cecil](https://github.com/jbevain/cecil) library which is MIT licensed.
