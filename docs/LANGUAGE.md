# GDScript shader language reference

This is the subset of GDScript that the Yotsuba GDScript frontend reads. A script is a `.gd` file; one file is one shader.

**A shader is a legal GDScript file.** Godot loads it without errors. The metadata that Godot has no concept of (stages, interface slots, bindings) is written as `# @...` comments, which Godot ignores. The shader name is the `class_name`, or the file name when there is none.

Everything here is **current behavior** unless a section says "not supported". The frontend supports what the Yotsuba IR and SPIR-V backend support; it never extends them.

## Script structure

```gdscript
class_name MyShader          # optional: the shader name

const SCALE = 2.0            # global constants are literals

class Data:                  # a struct
	var value: float # @location(0)

# @group(0) @binding(0) @uniform
var exposure: float          # a resource

func helper(a: float) -> float:       # a helper function
	return a * SCALE

# @vertex
func vs() -> Data:                    # an entry point
	var data := Data.new()
	data.value = helper(exposure)
	return data
```

Indentation is significant (spaces **or** tabs, not both; Godot's editor uses tabs). `#` starts a comment.

## Annotation comments

An annotation is `@name` or `@name(arguments)` inside a comment: `# @vertex`, `## @vertex` and `#@vertex` all work. A comment is an annotation comment when its text, after the `#` characters and spaces, starts with `@` and a letter; any other comment is ignored.

Where an annotation comment attaches:

| Position | Attaches to |
| --- | --- |
| The comment-only lines **directly above** a declaration (no blank line in between) | That declaration: a function, a global `var`, a struct field |
| A comment at the **end of a line** | The declaration on that line: a struct field or a parameter |

Several annotations can share one comment: `# @group(0) @binding(0) @uniform`.

Parameters take their annotation from a trailing comment, so a parameter that needs one goes on its own line:

```gdscript
# @vertex
func vs(
		vertex_index: int # @builtin(vertex_index)
) -> VertexOutput:
```

A trailing comment on the `func` line belongs to the function, not to its first parameter.

Real Godot annotations (`@export`, `@onready`, ...) or `@name` written as code are rejected with an explanation: Godot would not accept the file with unknown annotations.

## Types

| GDScript type | IR type | Notes |
| --- | --- | --- |
| `float` | `Float` (32 bit) | |
| `int` | `Int` (32 bit, signed) | |
| `bool` | `Bool` | |
| `Vector2`, `Vector3`, `Vector4` | `Vector` of floats | |
| `Vector2i`, `Vector3i`, `Vector4i` | `Vector` of ints | |
| `class Name:` | `CustomStruct` | Fields only: `var name: Type`. |

Not supported yet: matrices (`Transform2D`, `Basis`, `Projection`, ...), textures, samplers, arrays and dictionaries.

### Conversions

- `int` converts to `float` implicitly (`var f: float = 3` and `float_value + int_value`).
- Everything else is explicit: `float(x)`, `int(x)`.

## Statements

| Statement | Notes |
| --- | --- |
| `var x := expr`, `var x: T = expr`, `var x: T` | Local variable. `const` locals cannot be assigned. |
| `x = expr`, `x += expr`, ... | Compound operators: `+= -= *= /= %= &= \|= ^= <<= >>=`. Targets: variables and struct fields. |
| `if` / `elif` / `else` | The condition must be `bool`. |
| `while cond:` | |
| `for i in range(n)`, `range(a, b)`, `range(a, b, step)` | `i` is `int`. `step` must be a non-zero integer literal. |
| `break`, `continue` | Only inside a loop. |
| `return`, `return value` | |
| `pass` | |
| `helper(args)` | A call to a helper function used as a statement. |

Every non-void function must return on all paths. Recursion is rejected (SPIR-V does not allow it).

## Expressions

- Literals: `1`, `0x1F`, `1.5`, `1e3`, `true`, `false`.
- Arithmetic `+ - * / %`, comparison `== != < <= > >=`, logical `and or not` (and `&& || !`), bitwise `& | ^ ~`, shifts `<< >>`, on scalars.
- Conditional expression: `a if condition else b`. Only the chosen branch is evaluated.
- Vector constructors with one scalar per component: `Vector4(x, y, 0.0, 1.0)`.
- Component reads: `v.x`, `v.y`, `v.z`, `v.w`.
- Struct fields: `s.field`. Struct creation: `Name.new()` (fields are assigned afterwards).
- Calls to helper functions, with arguments converted to the parameter types.
- `float(x)` and `int(x)`.

Not supported yet (reported as errors): intrinsic functions (`sin`, `dot`, `mix`, `normalize`, ...), operators on vectors, swizzles such as `v.xy`, writing a vector component, indexing, and strings.

## Entry points

An entry point is a function with exactly one stage annotation.

| Annotation | Stage |
| --- | --- |
| `@vertex` | Vertex |
| `@fragment` | Fragment |
| `@compute` | Compute (needs a workgroup size, returns `void`) |

Parameters and the return value describe the stage interface:

- A parameter or return value of **struct type** is flattened: each field is its own interface variable and carries its own annotation.
- A **scalar or vector** parameter takes its annotation from a trailing comment on its own line (see above). Built-in indices and ids are `int`, `Vector2i`, `Vector3i`.
- The annotation of the **return value** of a non-struct return is written above the function: `# @fragment` and `# @location(0)`.
- Vertex and fragment entry points must return a value. Helper functions have no stage and no interface annotations.
- In a fragment shader, the clip-space position built-in is the fragment coordinate (`FragCoord`), as in the C# frontend.

## Annotation vocabularies

All spellings of one row lower to the same IR slot. Use any of them, mixed or not. Each is written as a comment, for example `# @SV_Position`.

### Stages and sizes

| Meaning | Native | HLSL | GLSL | WGSL |
| --- | --- | --- | --- | --- |
| Entry point | `# @vertex` `# @fragment` `# @compute` | (native) | (native) | (native) |
| Workgroup size | `@workgroup_size(x, y, z)` | `@numthreads(x, y, z)` | `@layout_local_size(x, y, z)` | `@workgroup_size(x, y, z)` |

### Built-in values (`BuiltInSlot`)

| IR built-in | Native `@builtin(...)` | HLSL | GLSL | Metal | SPIR-V | WGSL |
| --- | --- | --- | --- | --- | --- | --- |
| `Position` | `position` | `@SV_Position` | `@gl_Position` | `@position` | `@BuiltIn_Position` | `@builtin_position` |
| `PointSize` | `point_size` | | | | | |
| `VertexIndex` | `vertex_index` | `@SV_VertexID` | `@gl_VertexIndex` | `@vertex_id` | `@BuiltIn_VertexIndex` | `@builtin_vertex_index` |
| `InstanceIndex` | `instance_index` | `@SV_InstanceID` | `@gl_InstanceIndex` | `@instance_id` | `@BuiltIn_InstanceIndex` | `@builtin_instance_index` |
| `FragCoord` | `frag_coord` | | `@gl_FragCoord` | | `@BuiltIn_FragCoord` | |
| `FrontFacing` | `front_facing` | `@SV_IsFrontFace` | `@gl_FrontFacing` | `@front_facing` | `@BuiltIn_FrontFacing` | `@builtin_front_facing` |
| `FragDepth` | `frag_depth` | `@SV_Depth` | `@gl_FragDepth` | `@depth` | `@BuiltIn_FragDepth` | `@builtin_frag_depth` |
| `GlobalInvocationId` | `global_invocation_id` | `@SV_DispatchThreadID` | `@gl_GlobalInvocationID` | `@thread_position_in_grid` | `@BuiltIn_GlobalInvocationId` | `@builtin_global_invocation_id` |
| `LocalInvocationId` | `local_invocation_id` | `@SV_GroupThreadID` | `@gl_LocalInvocationID` | `@thread_position_in_threadgroup` | `@BuiltIn_LocalInvocationId` | `@builtin_local_invocation_id` |
| `LocalInvocationIndex` | `local_invocation_index` | `@SV_GroupIndex` | `@gl_LocalInvocationIndex` | `@thread_index_in_threadgroup` | `@BuiltIn_LocalInvocationIndex` | `@builtin_local_invocation_index` |
| `WorkgroupId` | `workgroup_id` | `@SV_GroupID` | `@gl_WorkGroupID` | `@threadgroup_position_in_grid` | `@BuiltIn_WorkgroupId` | `@builtin_workgroup_id` |
| `NumWorkgroups` | `num_workgroups` | | `@gl_NumWorkGroups` | `@threadgroups_per_grid` | `@BuiltIn_NumWorkgroups` | `@builtin_num_workgroups` |

A built-in is checked against the entry-point stage: vertex built-ins in `@vertex`, fragment built-ins in `@fragment`, invocation and workgroup values in `@compute`.

### Locations (`LocationSlot`)

`@location(n)`, `@Location(n)` (SPIR-V), `@SV_Target(n)` (HLSL render target), `@color(n)` and `@attribute(n)` (Metal).

### Vertex semantics (`VertexSlot`)

HLSL-style: `@POSITION`, `@COLOR`, `@TEXCOORD(n)`, `@NORMAL`, `@TANGENT`, `@BINORMAL`, with an optional index. Native form: `@semantic("TEXCOORD", 1)`. The backend resolves semantics to locations.

### Resources (`DescriptorSlot`)

| Meaning | Native | HLSL |
| --- | --- | --- |
| Resource kind | `@uniform`, `@storage_buffer`, `@push_constant` | `@cbuffer` (uniform) |
| Descriptor | `@group(set)` `@binding(n)` | `@register("b", n)` |

`@group` defaults to `0` when only `@binding` is given. Uniform and storage-buffer resources require a binding. Uniforms and push constants are read-only.

### Known to the vocabularies but not supported yet

The Yotsuba vocabularies name built-ins that the IR has no equivalent for: barycentric coordinates, clip distances, primitive and sample indices, sample coverage and masks, stencil reference, subgroup values and the workgroup-size built-in. Using one is an error that says so.

## Diagnostics

Errors are reported as `file(line,column): error: message`; positions inside an annotation comment point at the annotation. The frontend reports every error it can find in one pass and never throws on bad input except through `GdCompilationException`, which carries the full list.

## Extensions that are not valid GDScript

The compiler still understands `uint`, `Vector2u`/`Vector3u`/`Vector4u` and the `u` literal suffix (`3u`) because the IR has unsigned integers. **Godot rejects them**, so shaders that must stay legal GDScript should not use them; built-in indices and ids work with `int` and `Vector3i`.
