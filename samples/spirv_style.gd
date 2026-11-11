# Vulkan/SPIR-V-style vocabulary.
class_name SpirvStyle

class VertexOutput:
    @BuiltIn_Position var position: Vector4

@vertex
func vs(@BuiltIn_VertexIndex vertex_index: uint) -> VertexOutput:
    var output := VertexOutput.new()
    output.position = Vector4(0.0, 0.0, 0.0, 1.0)
    return output
