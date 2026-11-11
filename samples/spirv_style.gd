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
