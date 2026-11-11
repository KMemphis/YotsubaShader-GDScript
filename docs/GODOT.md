# Godot shading language backend

`YotsubaShader.Godot` writes **Godot shading language** (a `spatial` shader) from the Yotsuba IR. It reads the IR; it never changes it. The result is what `to_godot_shader_language()`, `to_godot_new_shader()` and `to_godot_shader_material()` return, and what `compile --target godot` writes to `<Name>.gdshader`.

Godot does not accept SPIR-V in a `ShaderMaterial`, and its shaders have no free-form stage interface, so this is a translation of the IR, not a wrapper around the SPIR-V. Everything here is current behavior, checked by compiling the output with Godot's real renderer (Godot 4.7).

Every shader starts with:

```glsl
shader_type spatial;
render_mode unshaded;
```

`unshaded` makes `ALBEDO` the final color, which is what a shader that returns a color expects.

## Entry points

| IR | Godot |
| --- | --- |
| Vertex entry point | `void vertex()` |
| Fragment entry point | `void fragment()` |
| Compute entry point | Not expressible. A script with only compute entry points is rejected. If it also has vertex or fragment entry points, the compute part is ignored (the SPIR-V still contains it). |
| Helper functions | Plain functions, written before their callers. Recursion is rejected. |

Godot does not allow `return` inside `vertex()` and `fragment()`. A `return` at the end of the entry point becomes plain assignments; an earlier `return` makes the body run inside a single-pass loop (`for (int yb_once = 0; yb_once < 1; yb_once++)`) and leaves it with a flag and `break`.

## Vertex inputs

| IR slot | Godot built-in | Type seen by the shader |
| --- | --- | --- |
| Vertex semantic `POSITION` | `VERTEX` | `vec3` (a `Vector2` field gets `.xy`, a `Vector4` gets `vec4(VERTEX, 1.0)`) |
| `COLOR` | `COLOR` | `vec4` |
| `TEXCOORD(0)` / `TEXCOORD(1)` | `UV` / `UV2` | `vec2` |
| `NORMAL`, `TANGENT`, `BINORMAL` | `NORMAL`, `TANGENT`, `BINORMAL` | `vec3` |
| Built-in `VertexIndex` / `InstanceIndex` | `VERTEX_ID` / `INSTANCE_ID` | `int` |
| `@location(0)` to `@location(3)` | `CUSTOM0` to `CUSTOM3` | `vec4` |

Any other slot is rejected, with the name of the field.

## Vertex outputs, varyings and fragment inputs

- A vertex output with the `Position` built-in writes `POSITION` (a `Vector4`); `PointSize` writes `POINT_SIZE`.
- A vertex output with a **location or a vertex semantic** becomes a `varying` (`v_loc0`, `v_color0`, ...). Integer varyings are `flat`.
- A fragment input is matched to the vertex output with the **same slot**; if none exists the shader is rejected. The types must agree.
- Fragment built-ins: `FragCoord` is `FRAGCOORD`, `FrontFacing` is `FRONT_FACING`.

## Fragment outputs

- `@location(0)` with a `Vector4` writes `ALBEDO` (rgb) and `ALPHA`; with a `Vector3` it writes `ALBEDO`.
- The `FragDepth` built-in writes `DEPTH`.
- Other locations are rejected: Godot shaders have one color output.

## Uniforms

| IR resource | Godot |
| --- | --- |
| Uniform or push constant of a scalar or vector type | `uniform <type> <name>;` |
| Uniform struct | One uniform per field, named `<resource>_<field>` (nested structs add their field names) |
| Storage buffer, texture, sampler | Rejected |

Godot has no uniform blocks and no push constants, so push constants become ordinary uniforms. Set values from GDScript with `material.set_shader_parameter("material_exposure", 1.5)`.

## Types and operators

`float`, `int`, `bool`, `vec2..4`, `ivec2..4` and structs map to the Godot types of the same shape (`uint` and `uvec` exist for the IR's unsigned values). Double precision, matrices and arrays are rejected. Operators map one to one, with two details: the remainder of floats is written as `a - b * trunc(a / b)` (GLSL has no `%` for floats, and the IR's remainder truncates), and structs become `struct` declarations written before their first use.

## Names

Identifiers that clash with Godot names are renamed with a trailing underscore (`mix` becomes `mix_`, `UV` becomes `UV_`). The temporaries of the frontend (`__select`, ...) are written as `yb_select`.

## Limits

- One `vertex()` and one `fragment()` per shader.
- `spatial` only. `canvas_item` and other shader types are not generated.
- Compute shaders run through Godot's `RenderingDevice`; use `to_spirv()` or `compile --target spirv` for them.
- Expressions that need statements of their own (a conditional expression or a short-circuit with side effects) write those statements right before the statement that uses them. This is exact for what the GDScript frontend builds, and for the C# frontend's conditional expressions; an expression that reads a variable changed by such a statement in the same expression could be reordered.
