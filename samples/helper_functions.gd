# Helper functions, loops, constants and conversions.
class_name HelperFunctions

const SCALE = 2.0
const STEPS = 4

class VertexOutput:
    @builtin(position) var position: Vector4
    @location(0) var intensity: float

func accumulate(base: float, steps: int) -> float:
    var total := base
    for i in range(steps):
        total += float(i) * SCALE
    return total

func choose(a: float, b: float, use_a: bool) -> float:
    return a if use_a else b

@vertex
func vs(@builtin(vertex_index) index: uint) -> VertexOutput:
    var output := VertexOutput.new()
    var value := accumulate(1.0, STEPS)
    var n := 0
    while n < 3:
        n += 1
        if n == 2:
            continue
        value = choose(value, 0.0, n > 1 and value > 0.5)
    output.position = Vector4(value, 0.0, 0.0, 1.0)
    output.intensity = value
    return output

@fragment
@location(0)
func fs(input: VertexOutput) -> Vector4:
    return Vector4(input.intensity, input.intensity, input.intensity, 1.0)
