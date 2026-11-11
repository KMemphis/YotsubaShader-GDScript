# WGSL-style vocabulary: the output backend does not depend on the authoring style.
class_name WgslStyle

class VertexOutput:
	var position: Vector4 # @builtin_position

# @vertex
func vs(
		index: int # @builtin_vertex_index
) -> VertexOutput:
	var output := VertexOutput.new()
	output.position = Vector4(0.0, 0.0, 0.0, 1.0)
	return output

# @compute
# @workgroup_size(8, 8, 1)
func main(
		id: Vector3i # @builtin_global_invocation_id
) -> void:
	pass
