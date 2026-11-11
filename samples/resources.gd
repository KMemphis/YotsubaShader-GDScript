# A uniform block read by a fragment shader.
class_name ResourcesShader

class MaterialData:
	var tint: Vector4 # @semantic("COLOR")
	var exposure: float

# @group(0) @binding(0) @uniform
var material: MaterialData

class VertexOutput:
	var position: Vector4 # @builtin(position)

# @vertex
func vs() -> VertexOutput:
	var output := VertexOutput.new()
	output.position = Vector4(0.0, 0.0, 0.0, 1.0)
	return output

# @fragment
# @location(0)
func fs(input: VertexOutput) -> Vector4:
	return Vector4(material.exposure, material.exposure, material.exposure, 1.0)
