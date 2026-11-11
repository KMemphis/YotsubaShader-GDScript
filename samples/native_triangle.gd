# The same idea with GDScript-native annotations: built-ins and locations.
class_name NativeTriangle

class VertexOutput:
    @builtin(position) var position: Vector4
    @location(0) var color: Vector4

@vertex
func vs(@builtin(vertex_index) vertex_index: uint) -> VertexOutput:
    var output := VertexOutput.new()
    var x := 0.0
    var y := 0.5
    if vertex_index == 1u:
        x = 0.5
        y = -0.5
    elif vertex_index == 2u:
        x = -0.5
        y = -0.5
    output.position = Vector4(x, y, 0.0, 1.0)
    output.color = Vector4(1.0, 0.5, 0.25, 1.0)
    return output

@fragment
@location(0)
func fs(input: VertexOutput) -> Vector4:
    return input.color
