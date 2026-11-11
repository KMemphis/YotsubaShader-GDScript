# Contributing to Yotsuba Shader - GDScript frontend

Thank you for your interest in the GDScript frontend of Yotsuba Shader. Contributions are welcome, from a script that
does not compile to a new language feature.

## Before you start

- **Small fixes** (typos, documentation, an obvious bug): open a pull request directly.
- **Language or annotation changes** (new syntax, new annotation spellings, new types): open an issue first. Describe:
  1. the problem you want to solve,
  2. your proposal, and
  3. a motivating `.gd` shader.

  Shaders must stay **legal GDScript**: Godot has to load every sample without errors, so new syntax cannot rely on real `@annotations` or on types and literals GDScript does not have. Shader metadata goes in `# @...` comments.

  Starting from an issue saves you from writing code that does not fit the design.
- **IR changes do not belong here.** This frontend only produces the IR defined in
  [YotsubaShader](https://github.com/KMemphis/YotsubaShader). If a construct is not representable in the IR yet, the
  frontend must reject it with a clear diagnostic and wait until the C# frontend supports it; it must never extend or
  work around the IR. Propose IR changes in the main repository.

## Set up

You need the **.NET 10 SDK** (`global.json` asks for 10.0 and rolls forward to the latest feature band) and Git. The
projects target `net8.0`. The SPIR-V backend of the main repository uses a source generator that needs a recent
compiler, which is why an 8.0 SDK alone is not enough.

```bash
git clone --recurse-submodules https://github.com/KMemphis/YotsubaShader-GDScript.git
cd YotsubaShader-GDScript
dotnet build YotsubaShader.GDScript.sln
dotnet test tests/YotsubaShader.GDScript.Tests
```

The legality tests use the Godot editor binary when they find it (`GODOT_PATH`, or `/Applications/Godot.app`) and are skipped without it. Install Godot 4 to run them. `GodotRenderingTests` compiles the generated shaders with Godot's real renderer and opens a window for a moment, so it only runs with `YOTSUBA_GODOT_RENDER_TESTS=1`.

If you cloned without `--recurse-submodules`, run `git submodule update --init --recursive`. The nested
`YotsubaShaderCross` submodule is needed: it provides the HLSL, GLSL, GLSL ES, Metal and C++ emission.

## How the repository is organised

| Path | Role |
| --- | --- |
| `src/YotsubaShader.GDScript` | The frontend library: lexer, parser, annotation vocabularies, lowering to IR, SPIR-V and source-backend facade. |
| `src/YotsubaShader.Godot` | Backend that writes Godot shading language from the IR. It only reads the IR. |
| `src/YotsubaShader.GDScript.CLI` | The `yotsuba-gd` command-line compiler (`compile` and `add`). |
| `tests/YotsubaShader.GDScript.Tests` | Lexer, parser, lowering, vocabulary, diagnostic and end-to-end tests. |
| `samples/` | Shaders that the test suite compiles. |
| `docs/LANGUAGE.md` | The supported GDScript subset. |
| `docs/GODOT.md` | The mapping from the IR to Godot shading language. |
| `YotsubaShader/` | Submodule with the IR, the SPIR-V backend and the compiler (nested YotsubaShaderCross). Read-only here. |

## Annotation changes

The annotation vocabularies (native, HLSL, GLSL, Metal, Vulkan/SPIR-V and WGSL styles) must mean the same thing, as they
do in the C# frontend. When you add or change an annotation:

- Add it to the matching file in `src/YotsubaShader.GDScript/Annotations/Dialects/`.
- Explain its **semantic equivalence** with the language that inspires it.
- Make sure equivalent spellings map to the same canonical meaning; `VocabularyTests` checks this.
- Include **tests**.
- Annotations are written as comments (`# @name`), never as real GDScript annotations.

## Pull requests

1. Fork the repository and create a branch from `main`.
2. Keep the change focused: one topic per pull request.
3. Run `dotnet build YotsubaShader.GDScript.sln` and `dotnet test tests/YotsubaShader.GDScript.Tests`.
4. Describe **what** changes and **why**, and link the issue if there is one.

Write commit messages that say what the change does, in the present tense ("Add swizzle diagnostics").

New `.cs` files carry the MS-PL header used by the rest of the repository; copy it from any existing file.

## Documentation

Two rules for the documentation:

- **Explain with `.gd` examples.** Show the concept in code, even when the explanation gets longer.
- **Distinguish current from planned behavior.** Never document a feature as working before it is implemented.

## Licensing of contributions

Yotsuba Shader is released under the [Microsoft Public License (MS-PL)](LICENSE). By submitting a contribution, you agree
to license it under the same Microsoft Public License (MS-PL), and you confirm that you have the right to do so.

## Using AI tools

AI assistants may be used to understand or review the code. See [`AGENTS.md`](AGENTS.md). You remain responsible for
everything you submit.

## Questions

If you are not sure whether an idea fits, open an issue and ask.
