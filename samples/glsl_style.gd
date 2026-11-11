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
