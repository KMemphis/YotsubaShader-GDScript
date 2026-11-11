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
