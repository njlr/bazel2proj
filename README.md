# bazel2proj

Tool for generating `fsproj` and `csproj` files from Bazel build definitions.

## How it works

 1. Use Bazel query to identify all binary and library targets
 2. Use Bazel aspects to figure out the project structure of each target
 3. Write appropriate .NET project files
