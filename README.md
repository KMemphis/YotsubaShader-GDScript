<h1 align="center">YotsubaShader · GDScript</h1>

<p align="center">
  A GDScript frontend for Yotsuba Shader: write shaders in typed GDScript and compile them to SPIR-V through the shared Yotsuba IR.
</p>

<p align="center">
  <a href="https://yotsubashader.com">Website</a> ·
  <a href="https://github.com/KMemphis/YotsubaShader">Main repository</a> ·
  <a href="docs/LANGUAGE.md">Language reference</a> ·
  <a href="https://github.com/KMemphis/YotsubaShader-GDScript">Repository</a>
</p>

## The product

[Yotsuba Shader](https://github.com/KMemphis/YotsubaShader) lowers shaders to one backend-neutral intermediate representation (IR) and generates output from it. Its first frontend reads C# through Roslyn. This repository adds a second frontend for teams that write their game logic in **GDScript**: it reads a typed subset of GDScript, builds the **same IR**, and hands it to the **same SPIR-V backend**.

```text
GDScript (.gd) with familiar annotations
          ↓
lexing, parsing and semantic analysis
          ↓
Yotsuba IR            ← shared with the C# frontend
          ↓
SPIR-V                ← shared backend, nothing GDScript-specific
```

The frontend is written in C# (.NET 8) because the IR is a C# model. It is a library plus a small command-line compiler; it does not run inside the Godot editor and does not replace Godot's own shading language or `ShaderMaterial`.

> This project is independent. It is not affiliated with or endorsed by the Godot Engine. "GDScript" names the language whose syntax the frontend reads.

## Status

The frontend compiles vertex, fragment and compute entry points with scalar, vector and struct values, helper functions and structured control flow to SPIR-V that passes `spirv-val`, and lowers uniform, storage-buffer and push-constant resources to the IR. It supports **exactly what the IR and the SPIR-V backend support today**: constructs the IR cannot represent yet are rejected with a clear diagnostic. This repository never extends the IR; when the C# frontend gains a feature in the main repository, this frontend follows. See [Current limits](#current-limits).

## One shader, six annotation vocabularies

The annotation style you use does not determine the output target. Native GDScript annotations and the HLSL, GLSL, Metal, Vulkan/SPIR-V and WGSL vocabularies of Yotsuba Shader mean the same thing and lower to the same IR slots. Pick the one your team knows.

### Native GDScript annotations

```gdscript
class_name NativeTriangle

class VertexOutput:
    @builtin(position) var position: Vector4
    @location(0) var color: Vector4

@vertex
func vs(@builtin(vertex_index) vertex_index: uint) -> VertexOutput:
    var output := VertexOutput.new()
    var x := 0.0
    var y := 0.5
    if vertex_index == 1u:
        x = 0.5
        y = -0.5
    elif vertex_index == 2u:
        x = -0.5
        y = -0.5
    output.position = Vector4(x, y, 0.0, 1.0)
    output.color = Vector4(1.0, 0.5, 0.25, 1.0)
    return output

@fragment
@location(0)
func fs(input: VertexOutput) -> Vector4:
    return input.color
```

This small shader shows the essential pieces:

- `class` declares a **struct**; its fields carry the interface annotations.
- `@vertex`, `@fragment` and `@compute` mark entry points.
- `@builtin(position)` is the GPU position; `@location(n)` is a numbered interface location.
- `@location(0)` before a function describes the value it returns (the first render target).
- `uint` and the `u` literal suffix are Yotsuba extensions; GDScript itself has no unsigned type.

### HLSL-like vocabulary: a familiar color shader

```gdscript
class_name BasicColorShader

class VertexInput:
    @POSITION var position: Vector2
    @COLOR var color: Vector4

class VertexOutput:
    @SV_Position var position: Vector4
    @COLOR var color: Vector4

class FragmentInput:
    @COLOR var color: Vector4

@vertex
func vs(input: VertexInput) -> VertexOutput:
    var output := VertexOutput.new()
    output.position = Vector4(input.position.x, input.position.y, 0.0, 1.0)
    output.color = input.color
    return output

@fragment
@SV_Target(0)
func fs(input: FragmentInput) -> Vector4:
    return input.color

@compute
@numthreads(3, 3, 3)
func compute() -> void:
    pass
```

This is the GDScript counterpart of `BasicColorShader` in the C# frontend. `[POSITION]` and `[COLOR]` become `@POSITION` and `@COLOR`; the rest of the vocabulary is spelled the same way, with GDScript's `@` in place of C# brackets.

### GLSL-like vocabulary

```gdscript
class_name GlslStyle

class VertexOutput:
    @gl_Position var position: Vector4

@vertex
func vs(@gl_VertexIndex vertex_index: uint) -> VertexOutput:
    var output := VertexOutput.new()
    output.position = Vector4(0.0, 0.0, 0.0, 1.0)
    return output
```

### Metal-like vocabulary

```gdscript
class_name MslStyle

class VertexOutput:
    @position var position: Vector4

@vertex
func vs(@vertex_id vertex_id: uint) -> VertexOutput:
    var output := VertexOutput.new()
    output.position = Vector4(0.0, 0.0, 0.0, 1.0)
    return output
```

### Vulkan-like vocabulary

```gdscript
class_name SpirvStyle

class VertexOutput:
    @BuiltIn_Position var position: Vector4

@vertex
func vs(@BuiltIn_VertexIndex vertex_index: uint) -> VertexOutput:
    var output := VertexOutput.new()
    output.position = Vector4(0.0, 0.0, 0.0, 1.0)
    return output
```

### WGSL-like vocabulary

```gdscript
class_name WgslStyle

class VertexOutput:
    @builtin_position var position: Vector4

@vertex
func vs(@builtin_vertex_index index: uint) -> VertexOutput:
    var output := VertexOutput.new()
    output.position = Vector4(0.0, 0.0, 0.0, 1.0)
    return output

@compute
@workgroup_size(8, 8, 1)
func main(@builtin_global_invocation_id id: Vector3u) -> void:
    pass
```

Every example above is a file in [`samples/`](samples), and the test suite compiles each one. The equivalence table of all spellings is in the [language reference](docs/LANGUAGE.md#annotation-vocabularies).

## Resources

Uniform and storage resources are global variables with a resource annotation and a binding:

```gdscript
class Material:
    @semantic("COLOR") var tint: Vector4
    var exposure: float

@group(0) @binding(0) @uniform var material: Material
```

`@register("b", 0)` (HLSL style) sets binding `0` in set `0`. Global `const` values are literals, inlined where they are used.

## Compiling to SPIR-V

### Command line

```bash
dotnet run --project src/YotsubaShader.GDScript.CLI -- \
  compile samples/basic_color.gd \
  --output artifacts/
```

`compile` accepts a `.gd` file or a directory (searched recursively), writes one `<ShaderName>.spv` per script, and can write the SPIR-V assembly text with `--emit-spvasm`. Diagnostics use the `file(line,column): error: message` format and the exit code is `1` when any script fails. SPIR-V is the only target of this frontend today.

### Library

```csharp
using YotsubaShader.GDScript;
using YotsubaShader.GDScript.Diagnostics;

try
{
    GdCompiledShader shader = GdShaderCompiler.CompileFile("samples/basic_color.gd");

    File.WriteAllBytes($"{shader.Name}.spv", shader.SpirV);
    Console.WriteLine(shader.SpirVAs);                      // SPIR-V assembly text
    Console.WriteLine(shader.IntermediateLanguage.Functions.Count); // the Yotsuba IR
}
catch (GdCompilationException exception)
{
    foreach (GdDiagnostic diagnostic in exception.Diagnostics)
        Console.Error.WriteLine(diagnostic);
}
```

`GdShaderCompiler.ToIntermediateLanguage` stops at the IR, which is useful when another tool consumes the IR directly. The IR to SPIR-V step is `IRToSpirvModule` and `SpirVCompiler` from the main repository, called unchanged.

## Current limits

Supported today: `float`, `int`, `uint`, `bool`; `Vector2/3/4`, `Vector2i/3i/4i`, `Vector2u/3u/4u`; structs (`class`); `var` and `const`; assignments and compound assignments; `if`/`elif`/`else`, `while`, `for i in range(...)`, `break`, `continue`; `a if c else b`; helper functions; vector constructors, component reads (`v.x`) and numeric conversions; uniform, storage-buffer and push-constant resources (lowered to IR; see the known issue below about their SPIR-V).

Rejected with a diagnostic because the IR or the backend does not support them yet:

- **Intrinsic functions** (`sin`, `dot`, `mix`, `normalize`, texture sampling, ...): the IR has no intrinsic calls.
- **Vector arithmetic and comparisons**, swizzles (`v.xy`) and **writing a vector component**.
- **Matrices, textures, samplers, arrays and indexing.**
- **Built-ins without an IR equivalent** (barycentrics, subgroups, clip distances, ...).
- GDScript features with no shader meaning: `match`, signals, `await`, lambdas, `extends`, dynamic typing.

Known issue of the shared backend: a shader that reads a global resource (uniform, storage buffer or push constant) produces SPIR-V that `spirv-val` rejects, because the backend does not list the resource in the entry point interface (see `samples/resources.gd`). The IR the frontend builds for these resources is correct; the fix belongs in the main repository. Every other sample passes `spirv-val`.

## Working with the repository

Requirements:

- .NET 10 SDK (the projects target `net8.0`; the backend's source generator needs a recent compiler)
- Git with submodules

```bash
git clone --recurse-submodules https://github.com/KMemphis/YotsubaShader-GDScript.git
cd YotsubaShader-GDScript
dotnet build YotsubaShader.GDScript.sln
dotnet test tests/YotsubaShader.GDScript.Tests
```

`YotsubaShader/` is a Git submodule that points at a commit of the main repository. This repository only consumes its IR and SPIR-V projects; it never modifies them. If you cloned without `--recurse-submodules`, run `git submodule update --init YotsubaShader`; the nested `YotsubaShaderCross` submodule is not needed.

## Repository architecture

| Path | Role |
| --- | --- |
| `src/YotsubaShader.GDScript` | The frontend: lexer, parser, annotation vocabularies, semantic analysis and lowering to IR, and the `GdShaderCompiler` facade. |
| `src/YotsubaShader.GDScript.CLI` | `yotsuba-gd`, the command-line compiler. |
| `tests/YotsubaShader.GDScript.Tests` | Lexer, parser, lowering, vocabulary, diagnostic and end-to-end tests. |
| `samples/` | Example shaders in every vocabulary, compiled by the tests. |
| `docs/LANGUAGE.md` | Reference for the supported GDScript subset. |
| `YotsubaShader/` | Submodule: `YotsubaShader.IR` and `YotsubaShader.Compiler.SpirV` from the main repository. |

Inside `src/YotsubaShader.GDScript`: `Lexing/` and `Parsing/` produce the syntax tree in `Syntax/`; `Annotations/` holds the single vocabulary table and one file per dialect; `Semantics/` defines the type system; `Lowering/` turns the tree into IR (`GdLowerer` for the module and interface slots, `GdFunctionLowerer` for function bodies).

## Contributing

Contributions are welcome. For language changes, open an issue describing the problem, your proposal and a motivating `.gd` shader. IR changes belong in the [main repository](https://github.com/KMemphis/YotsubaShader). See [CONTRIBUTING.md](CONTRIBUTING.md).

## Licensing

Yotsuba Shader - GDScript frontend is released under the [Microsoft Public License (MS-PL)](LICENSE). Third-party notices are in [NOTICE](NOTICE).

The name and logo are not covered by the license. See [assets/README.md](assets/README.md).

If Yotsuba Shader is useful to you or your company, consider [sponsoring the project](https://github.com/sponsors/KMemphis).
