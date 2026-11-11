# Metal-style vocabulary.
class_name MslStyle

class VertexOutput:
    @position var position: Vector4

@vertex
func vs(@vertex_id vertex_id: uint) -> VertexOutput:
    var output := VertexOutput.new()
    output.position = Vector4(0.0, 0.0, 0.0, 1.0)
    return output
