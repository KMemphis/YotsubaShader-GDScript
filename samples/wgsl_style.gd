# WGSL-style vocabulary: the output backend does not depend on the authoring style.
class_name WgslStyle

class VertexOutput:
    @builtin_position var position: Vector4

@vertex
func vs(@builtin_vertex_index index: uint) -> VertexOutput:
    var output := VertexOutput.new()
    output.position = Vector4(0.0, 0.0, 0.0, 1.0)
    return output

@compute
@workgroup_size(8, 8, 1)
func main(@builtin_global_invocation_id id: Vector3u) -> void:
    pass
