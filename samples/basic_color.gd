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
