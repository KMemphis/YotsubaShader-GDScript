# GLSL-style vocabulary.
class_name GlslStyle

class VertexOutput:
    @gl_Position var position: Vector4

@vertex
func vs(@gl_VertexIndex vertex_index: uint) -> VertexOutput:
    var output := VertexOutput.new()
    output.position = Vector4(0.0, 0.0, 0.0, 1.0)
    return output
