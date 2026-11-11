#region MS-PL License - Copyright (c) 2026 Memphis Yomael Núñez Cruz - Dominican Republic
/*
╔════════════════════════════════════════════════════════════════════════════════════╗
║                                                                                    ║
║  ███╗   ███╗███████╗      ██████╗ ██╗                                              ║
║  ████╗ ████║██╔════╝      ██╔══██╗██║                                              ║
║  ██╔████╔██║███████╗█████╗██████╔╝██║                                              ║
║  ██║╚██╔╝██║╚════██║╚════╝██╔═══╝ ██║                                              ║
║  ██║ ╚═╝ ██║███████║      ██║     ███████╗                                         ║
║  ╚═╝     ╚═╝╚══════╝      ╚═╝     ╚══════╝                                         ║
║                                                                                    ║
╠════════════════════════════════════════════════════════════════════════════════════╣
║  Copyright (c) 2026 Memphis Yomael Núñez Cruz - Dominican Republic                 ║
╠════════════════════════════════════════════════════════════════════════════════════╣
║                                                                                    ║
║  Microsoft Public License (MS-PL)                                                  ║
║                                                                                    ║
║  This license governs use of the accompanying software. If you use the software,   ║
║  you accept this license. If you do not accept the license, do not use the         ║
║  software.                                                                         ║
║                                                                                    ║
║  1. Definitions                                                                    ║
║  The terms "reproduce," "reproduction," "derivative works," and "distribution"     ║
║  have the same meaning here as under U.S. copyright law.                           ║
║  A "contribution" is the original software, to which copyright notices are         ║
║  attached, and any additions or changes to the software.                           ║
║  A "contributor" is any person that distributes its contribution under this        ║
║  license.                                                                          ║
║  "Licensed patents" are a contributor's patent claims that read directly on its    ║
║  contribution.                                                                     ║
║                                                                                    ║
║  2. Grant of Rights                                                                ║
║  (A) Copyright Grant- Subject to the terms of this license, including the license  ║
║  conditions and limitations in section 3, each contributor grants you a non-       ║
║  exclusive, worldwide, royalty-free copyright license to reproduce its             ║
║  contribution, prepare derivative works of its contribution, and distribute its    ║
║  contribution or any derivative works that you create.                             ║
║  (B) Patent Grant- Subject to the terms of this license, including the license     ║
║  conditions and limitations in section 3, each contributor grants you a non-       ║
║  exclusive, worldwide, royalty-free license under its licensed patents to make,    ║
║  have made, use, sell, offer for sale, import, and/or otherwise dispose of its     ║
║  contribution in the software or derivative works of the contribution in the       ║
║  software.                                                                         ║
║                                                                                    ║
║  3. Conditions and Limitations                                                     ║
║  (A) No Trademark License- This license does not grant you rights to use any       ║
║  contributors' name, logo, or trademarks.                                          ║
║  (B) If you bring a patent claim against any contributor over patents that you     ║
║  claim are infringed by the software, your patent license from such contributor    ║
║  to the software ends automatically.                                               ║
║  (C) If you distribute any portion of the software, you must retain all copyright, ║
║  patent, trademark, and attribution notices that are present in the software.      ║
║  (D) If you distribute any portion of the software in source code form, you may do ║
║  so only under this license by including a complete copy of this license with      ║
║  your distribution. If you distribute any portion of the software in compiled or   ║
║  object code form, you may only do so under a license that complies with this      ║
║  license.                                                                          ║
║  (E) The software is licensed "as-is." You bear the risk of using it. The          ║
║  contributors give no express warranties, guarantees or conditions. You may have   ║
║  additional consumer rights under your local laws which this license cannot        ║
║  change. To the extent permitted under your local laws, the contributors exclude   ║
║  the implied warranties of merchantability, fitness for a particular purpose and   ║
║  non-infringement.                                                                 ║
║                                                                                    ║
╚════════════════════════════════════════════════════════════════════════════════════╝
*/
#endregion

using YotsubaShader.GDScript.Annotations;
using YotsubaShader.IR.Intermediate;
using YotsubaShader.IR.Intermediate.Slot;
using YotsubaShader.IR.Intermediate.Slot.Types;
using YotsubaShader.IR.Intermediate.Types;
using static YotsubaShader.GDScript.Tests.TestSupport;

namespace YotsubaShader.GDScript.Tests;

public class VocabularyTests
{
    private static InterfaceSlot? VertexOutputSlot(string annotation)
    {
        var shader = Lower($$"""
            class Out:
                var value: Vector4 # {{annotation}}

            # @vertex
            func vs() -> Out:
                var o := Out.new()
                o.value = Vector4(0.0, 0.0, 0.0, 1.0)
                return o
            """);
        return ((CustomStruct)shader.Functions[0].Outputs[0]).Properties[0].Slot;
    }

    private static InterfaceSlot? ComputeInputSlot(string annotation)
    {
        var shader = Lower($"""
            # @compute
            # @workgroup_size(1, 1, 1)
            func main(
                    id: Vector3i # {annotation}
            ) -> void:
                pass
            """);
        return shader.Functions[0].Inputs[0].Slot;
    }

    [Theory]
    [InlineData("@builtin(position)")]
    [InlineData("@SV_Position")]
    [InlineData("@gl_Position")]
    [InlineData("@position")]
    [InlineData("@BuiltIn_Position")]
    [InlineData("@builtin_position")]
    public void PositionSpellings_LowerToTheSameSlot(string annotation) =>
        Assert.Equal(new BuiltInSlot(BuiltIn.Position), VertexOutputSlot(annotation));

    [Theory]
    [InlineData("@builtin(global_invocation_id)")]
    [InlineData("@SV_DispatchThreadID")]
    [InlineData("@gl_GlobalInvocationID")]
    [InlineData("@thread_position_in_grid")]
    [InlineData("@BuiltIn_GlobalInvocationId")]
    [InlineData("@builtin_global_invocation_id")]
    public void GlobalInvocationIdSpellings_LowerToTheSameSlot(string annotation) =>
        Assert.Equal(new BuiltInSlot(BuiltIn.GlobalInvocationId), ComputeInputSlot(annotation));

    [Theory]
    [InlineData("@location(3)")]
    [InlineData("@Location(3)")]
    [InlineData("@SV_Target(3)")]
    [InlineData("@color(3)")]
    [InlineData("@attribute(3)")]
    public void LocationSpellings_LowerToTheSameSlot(string annotation) =>
        Assert.Equal(new LocationSlot(3), VertexOutputSlot(annotation));

    [Theory]
    [InlineData("@POSITION", VertexUsage.Position, 0)]
    [InlineData("@COLOR", VertexUsage.Color, 0)]
    [InlineData("@TEXCOORD(2)", VertexUsage.TextureCoordinate, 2)]
    [InlineData("@NORMAL", VertexUsage.Normal, 0)]
    [InlineData("@semantic(\"TANGENT\", 1)", VertexUsage.Tangent, 1)]
    public void VertexSemantics_LowerToVertexSlots(string annotation, VertexUsage usage, int index) =>
        Assert.Equal(new VertexSlot(new VertexSemantic(usage, index)), VertexOutputSlot(annotation));

    [Theory]
    [InlineData("@numthreads(4, 2, 1)")]
    [InlineData("@workgroup_size(4, 2, 1)")]
    [InlineData("@layout_local_size(4, 2, 1)")]
    public void WorkgroupSizeSpellings_LowerToComputeParams(string annotation)
    {
        var shader = Lower($"# {annotation}\n# @compute\nfunc main() -> void:\n    pass\n");

        Assert.Equal(new ComputeParams(4, 2, 1), shader.Functions[0].ExtraParams);
    }

    [Theory]
    [InlineData("@group(1) @binding(2)", 1, 2)]
    [InlineData("@register(\"b\", 5)", 0, 5)]
    [InlineData("@binding(4)", 0, 4)]
    public void DescriptorAnnotations_LowerToDescriptorSlots(string annotations, int set, int binding)
    {
        var shader = Lower($$"""
            # {{annotations}} @uniform
            var exposure: float

            class Out:
                var position: Vector4 # @builtin(position)

            # @vertex
            func vs() -> Out:
                var o := Out.new()
                o.position = Vector4(exposure, 0.0, 0.0, 1.0)
                return o
            """);

        Assert.Equal(new DescriptorSlot(set, binding), Assert.Single(shader.GlobalResources).Slot);
    }

    [Fact]
    public void EverySupportedBuiltInIsReachableFromEveryDialect()
    {
        var byBuiltIn = AnnotationVocabulary.Default.All
            .Where(d => d.Kind == AnnotationKind.BuiltIn)
            .GroupBy(d => d.BuiltIn!.Value)
            .ToDictionary(g => g.Key, g => g.Select(d => d.Dialect).Distinct().ToList());

        // The clip-space position and the vertex index have an equivalent in each style that has one.
        Assert.Contains("hlsl", byBuiltIn[BuiltIn.Position]);
        Assert.Contains("glsl", byBuiltIn[BuiltIn.Position]);
        Assert.Contains("msl", byBuiltIn[BuiltIn.Position]);
        Assert.Contains("spirv", byBuiltIn[BuiltIn.Position]);
        Assert.Contains("wgsl", byBuiltIn[BuiltIn.Position]);
        Assert.Equal(5, byBuiltIn[BuiltIn.VertexIndex].Count);
        Assert.Equal(5, byBuiltIn[BuiltIn.GlobalInvocationId].Count);
    }

    [Fact]
    public void ConflictingDefinitionsAcrossDialects_AreRejected()
    {
        var conflicting = new[]
        {
            new AnnotationDefinition("same", AnnotationKind.BuiltIn, "a", BuiltIn: BuiltIn.Position),
            new AnnotationDefinition("same", AnnotationKind.BuiltIn, "b", BuiltIn: BuiltIn.FragCoord)
        };

        Assert.Throws<InvalidOperationException>(() => new AnnotationVocabulary(conflicting));
    }
}
