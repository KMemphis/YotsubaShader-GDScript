<h1 align="center">YotsubaShader · GDScript</h1>

<p align="center">
  A GDScript frontend for Yotsuba Shader: write shaders in GDScript that Godot itself accepts, and compile them to SPIR-V through the shared Yotsuba IR.
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
GDScript (.gd): valid in Godot, metadata in "# @..." comments
          ↓
lexing, parsing and semantic analysis
          ↓
Yotsuba IR            ← shared with the C# frontend
          ↓
SPIR-V                ← shared backend, nothing GDScript-specific
          ↓
Godot shading language ← optional backend of this repository, for ShaderMaterial
```

**Every shader is a legal GDScript file.** Godot loads it without errors, so you keep Godot's editor, syntax highlighting and CPU-side checks. The shader-specific information (stages, interface slots, bindings) is written as `# @...` comments, which Godot ignores and Yotsuba reads. There are no custom syntax extensions in the language the samples use.

The frontend is written in C# (.NET 8) because the IR is a C# model. It is a library plus a small command-line compiler; it does not run inside the Godot editor and does not replace Godot's own shading language or `ShaderMaterial`.

> This project is independent. It is not affiliated with or endorsed by the Godot Engine. "GDScript" names the language whose syntax the frontend reads.

## Status

The frontend compiles vertex, fragment and compute entry points with scalar, vector and struct values, helper functions and structured control flow to SPIR-V that passes `spirv-val`, and lowers uniform, storage-buffer and push-constant resources to the IR. A second backend writes **Godot shading language** (`spatial` shaders) from the IR, so the same script becomes a `ShaderMaterial`. It supports **exactly what the IR and the SPIR-V backend support today**: constructs the IR cannot represent yet are rejected with a clear diagnostic. This repository never extends the IR; when the C# frontend gains a feature in the main repository, this frontend follows. See [Current limits](#current-limits).

## One shader, six annotation vocabularies

The annotation style you use does not determine the output target. Native GDScript annotations and the HLSL, GLSL, Metal, Vulkan/SPIR-V and WGSL vocabularies of Yotsuba Shader mean the same thing and lower to the same IR slots. Pick the one your team knows.

### Native GDScript annotations

```gdscript
# The same idea with GDScript-native annotations: built-ins and locations.
class_name NativeTriangle

class VertexOutput:
	var position: Vector4 # @builtin(position)
	var color: Vector4 # @location(0)

# @vertex
func vs(
		vertex_index: int # @builtin(vertex_index)
) -> VertexOutput:
	var output := VertexOutput.new()
	var x := 0.0
	var y := 0.5
	if vertex_index == 1:
		x = 0.5
		y = -0.5
	elif vertex_index == 2:
		x = -0.5
		y = -0.5
	output.position = Vector4(x, y, 0.0, 1.0)
	output.color = Vector4(1.0, 0.5, 0.25, 1.0)
	return output

# @fragment
# @location(0)
func fs(input: VertexOutput) -> Vector4:
	return input.color
```

This small shader shows the essential pieces:

- `class` declares a **struct**; the comment at the end of each field line carries its interface annotation.
- `# @vertex`, `# @fragment` and `# @compute` on the lines above a function mark entry points.
- `# @builtin(position)` is the GPU position; `# @location(n)` is a numbered interface location.
- `# @location(0)` above a function describes the value it returns (the first render target).
- A parameter that needs an annotation goes on its own line, with the comment after it.
- Built-in indices are plain `int` (GDScript has no unsigned type).

### HLSL-like vocabulary: a familiar color shader

```gdscript
# A vertex and a fragment shader that pass a per-vertex color through.
# Equivalent to BasicColorShader in the C# frontend, written in the HLSL-style vocabulary.
#
# This file is valid GDScript: Yotsuba reads its shader metadata from "# @..." comments,
# which Godot ignores.
class_name BasicColorShader

class VertexInput:
	var position: Vector2 # @POSITION
	var color: Vector4 # @COLOR

class VertexOutput:
	var position: Vector4 # @SV_Position
	var color: Vector4 # @COLOR

class FragmentInput:
	var color: Vector4 # @COLOR

# @vertex
func vs(input: VertexInput) -> VertexOutput:
	var output := VertexOutput.new()
	output.position = Vector4(input.position.x, input.position.y, 0.0, 1.0)
	output.color = input.color
	return output

# @fragment
# @SV_Target(0)
func fs(input: FragmentInput) -> Vector4:
	return input.color

# @compute
# @numthreads(3, 3, 3)
func compute() -> void:
	pass
```

This is the GDScript counterpart of `BasicColorShader` in the C# frontend. `[POSITION]` and `[COLOR]` become the comments `# @POSITION` and `# @COLOR`; the rest of the vocabulary is spelled the same way, with a comment in place of C# brackets.

### GLSL-like vocabulary

```gdscript
# GLSL-style vocabulary.
class_name GlslStyle

class VertexOutput:
	var position: Vector4 # @gl_Position

# @vertex
func vs(
		vertex_index: int # @gl_VertexIndex
) -> VertexOutput:
	var output := VertexOutput.new()
	output.position = Vector4(0.0, 0.0, 0.0, 1.0)
	return output
```

### Metal-like vocabulary

```gdscript
# Metal-style vocabulary.
class_name MslStyle

class VertexOutput:
	var position: Vector4 # @position

# @vertex
func vs(
		vertex_id: int # @vertex_id
) -> VertexOutput:
	var output := VertexOutput.new()
	output.position = Vector4(0.0, 0.0, 0.0, 1.0)
	return output
```

### Vulkan-like vocabulary

```gdscript
# Vulkan/SPIR-V-style vocabulary.
class_name SpirvStyle

class VertexOutput:
	var position: Vector4 # @BuiltIn_Position

# @vertex
func vs(
		vertex_index: int # @BuiltIn_VertexIndex
) -> VertexOutput:
	var output := VertexOutput.new()
	output.position = Vector4(0.0, 0.0, 0.0, 1.0)
	return output
```

### WGSL-like vocabulary

```gdscript
# WGSL-style vocabulary: the output backend does not depend on the authoring style.
class_name WgslStyle

class VertexOutput:
	var position: Vector4 # @builtin_position

# @vertex
func vs(
		index: int # @builtin_vertex_index
) -> VertexOutput:
	var output := VertexOutput.new()
	output.position = Vector4(0.0, 0.0, 0.0, 1.0)
	return output

# @compute
# @workgroup_size(8, 8, 1)
func main(
		id: Vector3i # @builtin_global_invocation_id
) -> void:
	pass
```

Every example above is a file in [`samples/`](samples). The test suite compiles each one and, when Godot is installed, checks that Godot loads it without errors. The equivalence table of all spellings is in the [language reference](docs/LANGUAGE.md#annotation-vocabularies).

## Resources

Uniform and storage resources are global variables with a resource annotation and a binding:

```gdscript
class MaterialData:
	var tint: Vector4 # @semantic("COLOR")
	var exposure: float

# @group(0) @binding(0) @uniform
var material: MaterialData
```

`# @register("b", 0)` (HLSL style) sets binding `0` in set `0`. Global `const` values are literals, inlined where they are used.

## Using it: two commands

The command-line compiler `yotsuba-gd` has the same two commands as the C# frontend's CLI: `compile`, which writes the results to a folder, and `add`, which embeds them in your project.

### `compile`: write the results to a folder

```bash
dotnet run --project src/YotsubaShader.GDScript.CLI -- \
  compile samples/basic_color.gd \
  --target spirv --target godot \
  --output artifacts/
```

`compile` accepts a `.gd` file, a folder or a `project.godot`. For a folder or project it searches recursively and compiles only the scripts that declare a shader entry point (`# @vertex`, `# @fragment` or `# @compute`); the ordinary scripts of a Godot project are left alone. Godot's own rules apply: `.godot/`, hidden folders, folders with a `.gdignore` file and nested projects are skipped.

| Option | Effect |
| --- | --- |
| `--output`, `-o` | Folder for the generated files (default `./artifacts`). |
| `--target`, `-t` | `spirv` (default) writes `<Name>.spv`; `godot` writes `<Name>.gdshader`. Repeat it to write both. |
| `--emit-spvasm` | Also writes the SPIR-V assembly text. |

Diagnostics use the `file(line,column): error: message` format. The exit code is `1` when any script fails.

### `add`: embed the shaders in a Godot project

```bash
dotnet run --project src/YotsubaShader.GDScript.CLI -- \
  add MyGame/project.godot
```

`add` takes a `project.godot` (or the folder that contains it), compiles every shader of the project and writes the result **at the end of each shader's own script**, between two marker comments. GDScript has no extension methods, so the functions live in the shader's script and are called on its `class_name`:

```gdscript
class_name MyCustomShader

# ... your shader ...

# <yotsuba:generated>
# ... generated by `yotsuba-gd add`; replaced the next time it runs ...
# </yotsuba:generated>
```

Then, from any script of your game:

```gdscript
var code: String = MyCustomShader.to_godot_shader_language()          # Godot shading language source
var shader: Shader = MyCustomShader.to_godot_new_shader()             # a new Shader resource
var material: ShaderMaterial = MyCustomShader.to_godot_shader_material()  # a ShaderMaterial using it
var spirv: PackedByteArray = MyCustomShader.to_spirv()                # the SPIR-V binary

$MeshInstance3D.material_override = MyCustomShader.to_godot_shader_material()
```

What `add` does and does not do:

- It changes only scripts that declare a shader entry point, and only between its markers. Running it again refreshes the block; if nothing changed the file is not rewritten.
- The script stays valid GDScript, and the compiler ignores the block when it compiles the script again. The indentation (tabs or spaces) and line endings of the script are kept.
- `--dry-run` reports what would change without writing any file.
- A script with compile errors is reported and left untouched.
- A compute shader has no Godot shading language equivalent: its `to_godot_*` functions call `push_error` with the reason, and `to_spirv()` still works. A script with vertex or fragment entry points and a compute entry point gets the Godot shader for the vertex and fragment part.
- The block embeds the Godot source as a string and the SPIR-V as base64, so a Godot project needs nothing else at runtime.

Run it again after you change a shader. Commit the generated block or add it to your build, as you prefer.

The mapping from the IR to Godot (built-ins, varyings, uniforms and limits) is described in [docs/GODOT.md](docs/GODOT.md).

### Library

```csharp
using YotsubaShader.GDScript;
using YotsubaShader.GDScript.Diagnostics;

try
{
    GdCompiledShader shader = GdShaderCompiler.CompileFile("samples/basic_color.gd");

    File.WriteAllBytes($"{shader.Name}.spv", shader.SpirV);
    File.WriteAllText($"{shader.Name}.gdshader", shader.ToGodotShaderLanguage());
    Console.WriteLine(shader.SpirVAs);                              // SPIR-V assembly text
    Console.WriteLine(shader.IntermediateLanguage.Functions.Count); // the Yotsuba IR
}
catch (GdCompilationException exception)
{
    foreach (GdDiagnostic diagnostic in exception.Diagnostics)
        Console.Error.WriteLine(diagnostic);
}
```

`GdShaderCompiler.ToIntermediateLanguage` stops at the IR, which is useful when another tool consumes the IR directly. The IR to SPIR-V step is `IRToSpirvModule` and `SpirVCompiler` from the main repository, called unchanged. `ToGodotShaderLanguage()` throws `GodotBackendException` when the IR cannot be expressed in Godot. The `add` command is `GdProjectAdder.Add(GdProject.Find("project.godot"))`.

## Current limits

Supported today: `float`, `int`, `bool`; `Vector2/3/4`, `Vector2i/3i/4i`; structs (`class`); `var` and `const`; assignments and compound assignments; `if`/`elif`/`else`, `while`, `for i in range(...)`, `break`, `continue`; `a if c else b`; helper functions; vector constructors, component reads (`v.x`) and numeric conversions; uniform, storage-buffer and push-constant resources (lowered to IR; see the known issue below about their SPIR-V).

Rejected with a diagnostic because the IR or the backend does not support them yet:

- **Intrinsic functions** (`sin`, `dot`, `mix`, `normalize`, texture sampling, ...): the IR has no intrinsic calls.
- **Vector arithmetic and comparisons**, swizzles (`v.xy`) and **writing a vector component**.
- **Matrices, textures, samplers, arrays and indexing.**
- **Built-ins without an IR equivalent** (barycentrics, subgroups, clip distances, ...).
- GDScript features with no shader meaning: `match`, signals, `await`, lambdas, `extends`, dynamic typing.
- Real Godot annotations (`@export`, `@onready`, ...) and annotations written as code: write Yotsuba metadata as `# @...` comments instead.

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

The legality tests run the Godot editor binary headlessly when they find it (`GODOT_PATH`, or `/Applications/Godot.app`); without Godot they are skipped silently. One more test compiles every generated Godot shader with Godot's real renderer and calls the methods that `add` embeds; it opens a Godot window for a moment, so it only runs with `YOTSUBA_GODOT_RENDER_TESTS=1`.

`YotsubaShader/` is a Git submodule that points at a commit of the main repository. This repository only consumes its IR and SPIR-V projects; it never modifies them. If you cloned without `--recurse-submodules`, run `git submodule update --init YotsubaShader`; the nested `YotsubaShaderCross` submodule is not needed.

## Repository architecture

| Path | Role |
| --- | --- |
| `src/YotsubaShader.GDScript` | The frontend: lexer, parser, annotation vocabularies, semantic analysis and lowering to IR, and the `GdShaderCompiler` facade. |
| `src/YotsubaShader.Godot` | Backend: the Yotsuba IR to Godot shading language. It only reads the IR. |
| `src/YotsubaShader.GDScript.CLI` | `yotsuba-gd`, the command-line compiler with the `compile` and `add` commands. |
| `tests/YotsubaShader.GDScript.Tests` | Lexer, parser, lowering, vocabulary, diagnostic and end-to-end tests. |
| `samples/` | Example shaders in every vocabulary, compiled by the tests. |
| `docs/LANGUAGE.md` | Reference for the supported GDScript subset. |
| `docs/GODOT.md` | How the IR maps to Godot shading language, and the limits of that mapping. |
| `YotsubaShader/` | Submodule: `YotsubaShader.IR` and `YotsubaShader.Compiler.SpirV` from the main repository. |

Inside `src/YotsubaShader.GDScript`: `Project/` finds the Godot project and its shader scripts and writes the generated block of `add`; `Lexing/` and `Parsing/` produce the syntax tree in `Syntax/`; `Annotations/` holds the single vocabulary table and one file per dialect; `Semantics/` defines the type system; `Lowering/` turns the tree into IR (`GdLowerer` for the module and interface slots, `GdFunctionLowerer` for function bodies).

## Contributing

Contributions are welcome. For language changes, open an issue describing the problem, your proposal and a motivating `.gd` shader. IR changes belong in the [main repository](https://github.com/KMemphis/YotsubaShader). See [CONTRIBUTING.md](CONTRIBUTING.md).

## Licensing

Yotsuba Shader - GDScript frontend is released under the [Microsoft Public License (MS-PL)](LICENSE). Third-party notices are in [NOTICE](NOTICE).

The name and logo are not covered by the license. See [assets/README.md](assets/README.md).

If Yotsuba Shader is useful to you or your company, consider [sponsoring the project](https://github.com/sponsors/KMemphis).
