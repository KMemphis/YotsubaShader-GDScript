# A vertex and a fragment shader that pass a per-vertex color through.
# Equivalent to BasicColorShader in the C# frontend, written in the HLSL-style vocabulary.
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
