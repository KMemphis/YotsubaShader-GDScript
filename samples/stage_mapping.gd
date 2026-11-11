# Less common stage inputs and outputs: UV2, TANGENT, BINORMAL, CUSTOM0, integer varyings,
# FRAGCOORD, FRONT_FACING, DEPTH and POINT_SIZE once written as Godot shading language.
class_name MappingShader

class VertexInput:
	var position: Vector3 # @POSITION
	var uv2: Vector2 # @TEXCOORD(1)
	var tangent: Vector3 # @TANGENT
	var binormal: Vector3 # @BINORMAL
	var custom: Vector4 # @location(0)

class VertexOutput:
	var position: Vector4 # @builtin(position)
	var tag: int # @location(5)
	var weight: float # @location(6)
	var point: float # @builtin(point_size)

# @vertex
func vs(
		input: VertexInput
) -> VertexOutput:
	var output := VertexOutput.new()
	output.position = Vector4(input.position.x, input.uv2.x, input.tangent.y, input.binormal.z)
	output.tag = int(input.custom.x)
	output.weight = input.custom.w
	output.point = 4.0
	return output

class FragmentInput:
	var tag: int # @location(5)
	var weight: float # @location(6)

class FragmentOutput:
	var color: Vector4 # @location(0)
	var depth: float # @builtin(frag_depth)

# @fragment
func fs(
		input: FragmentInput,
		coord: Vector4, # @builtin(frag_coord)
		front: bool # @builtin(front_facing)
) -> FragmentOutput:
	var output := FragmentOutput.new()
	var shade := input.weight
	if front and input.tag % 2 == 0:
		shade = coord.x * 0.001
	output.color = Vector4(shade, shade, shade, 1.0)
	output.depth = 0.5
	return output
