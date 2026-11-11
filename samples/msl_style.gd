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
