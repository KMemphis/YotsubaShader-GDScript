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

using YotsubaShader.Godot;
using static YotsubaShader.GDScript.Tests.TestSupport;

namespace YotsubaShader.GDScript.Tests;

public class GodotWriterTests
{
    private static string Godot(string source)
    {
        var (name, shader) = GdShaderCompiler.ToIntermediateLanguage(source);
        return GodotShaderWriter.Write(shader, name);
    }

    private static string GodotSample(string sample) => Godot(File.ReadAllText(SamplePath(sample)));

    private static string Body(string code, string function)
    {
        int start = code.IndexOf($"void {function}() {{", StringComparison.Ordinal);
        Assert.True(start >= 0, $"{function}() not found in:\n{code}");
        return code[start..];
    }

    [Fact]
    public void Header_DeclaresASpatialUnshadedShader()
    {
        string code = GodotSample("native_triangle.gd");

        Assert.Contains("shader_type spatial;", code);
        Assert.Contains("render_mode unshaded;", code);
    }

    [Fact]
    public void VertexInputsAndOutputs_MapToGodotBuiltins()
    {
        string code = GodotSample("basic_color.gd");

        Assert.Contains("VERTEX.xy.x", code);          // POSITION semantic, a Vector2 field
        Assert.Contains("= COLOR;", code);              // COLOR semantic
        Assert.Contains("POSITION = ", code);           // the position output
        Assert.Contains("varying vec4 v_color0;", code);
        Assert.Contains("ALBEDO = ", code);
        Assert.Contains("ALPHA = ", code);
    }

    [Fact]
    public void BuiltInIndices_MapToVertexId()
    {
        string code = GodotSample("native_triangle.gd");

        Assert.Contains("VERTEX_ID == 1", code);
        Assert.Contains("varying vec4 v_loc0;", code);
    }

    [Fact]
    public void ProcessorFunctions_NeverContainReturn()
    {
        foreach (string sample in new[] { "basic_color.gd", "native_triangle.gd", "helper_functions.gd" })
        {
            string code = GodotSample(sample);
            string processors = code[code.IndexOf("void vertex()", StringComparison.Ordinal)..];

            Assert.DoesNotContain("return;", processors);
            Assert.DoesNotContain("return ", processors);
        }
    }

    [Fact]
    public void EarlyReturn_RunsInASinglePassLoop()
    {
        string code = Godot("""
            class Out:
            	var position: Vector4 # @builtin(position)

            # @vertex
            func vs() -> Out:
            	var o := Out.new()
            	for i in range(3):
            		if i == 1:
            			return o
            	o.position = Vector4(0.0, 0.0, 0.0, 1.0)
            	return o
            """);
        string vertex = Body(code, "vertex");

        Assert.Contains("for (int yb_once", vertex);
        Assert.Contains("= true;", vertex);
        Assert.Contains("break;", vertex);
        Assert.DoesNotContain("return", vertex);
    }

    [Fact]
    public void HelperFunctions_KeepTheirReturns_AndComeBeforeTheirCallers()
    {
        string code = GodotSample("helper_functions.gd");

        Assert.Contains("return total;", code);
        Assert.True(code.IndexOf("float accumulate(", StringComparison.Ordinal) <
                    code.IndexOf("void vertex()", StringComparison.Ordinal));
        Assert.True(code.IndexOf("float choose(", StringComparison.Ordinal) <
                    code.IndexOf("void vertex()", StringComparison.Ordinal));
    }

    [Fact]
    public void Structs_AreDeclaredBeforeTheyAreUsed()
    {
        string code = GodotSample("basic_color.gd");

        Assert.Contains("struct VertexOutput {", code);
        Assert.True(code.IndexOf("struct VertexOutput", StringComparison.Ordinal) <
                    code.IndexOf("void vertex()", StringComparison.Ordinal));
    }

    [Fact]
    public void UniformStructs_AreFlattenedIntoOneUniformPerField()
    {
        string code = GodotSample("resources.gd");

        Assert.Contains("uniform vec4 material_tint;", code);
        Assert.Contains("uniform float material_exposure;", code);
        Assert.Contains("material_exposure", Body(code, "fragment"));
    }

    [Fact]
    public void ScalarUniforms_KeepTheirName()
    {
        string code = Godot("""
            # @group(0) @binding(0) @uniform
            var strength: float

            class Out:
            	var position: Vector4 # @builtin(position)

            # @vertex
            func vs() -> Out:
            	var o := Out.new()
            	o.position = Vector4(strength, 0.0, 0.0, 1.0)
            	return o
            """);

        Assert.Contains("uniform float strength;", code);
    }

    [Fact]
    public void FloatRemainder_UsesTruncation()
    {
        string code = Godot(VertexWith("var a := 5.5\nvar b := a % 2.0"));

        Assert.Contains("trunc(", code);
        Assert.DoesNotContain(" % 2.0", code);
    }

    [Fact]
    public void IntegerRemainder_UsesTheOperator()
    {
        string code = Godot(VertexWith("var a := 5\nvar b := a % 2"));

        Assert.Contains("int b = a % 2;", code);
    }

    [Fact]
    public void Identifiers_AreRenamedWhenTheyClashWithGodotNames()
    {
        string code = Godot(VertexWith("var mix := 1.0\nvar length := 2.0\nvar UV := 3.0"));

        Assert.Contains("float mix_ = 1.0;", code);
        Assert.Contains("float length_ = 2.0;", code);
        Assert.Contains("float UV_ = 3.0;", code);
    }

    [Fact]
    public void LessCommonStageValues_MapToTheirGodotBuiltins()
    {
        string code = GodotSample("stage_mapping.gd");

        foreach (string expected in new[]
                 {
                     "UV2.x", "TANGENT.y", "BINORMAL.z", "CUSTOM0.x", "POINT_SIZE = ", "DEPTH = ",
                     "FRONT_FACING", "FRAGCOORD.x", "varying flat int v_loc5;"
                 })
        {
            Assert.Contains(expected, code);
        }
    }

    [Fact]
    public void ComponentOfAVectorStageInput_IsReadByLetter()
    {
        string code = Godot("""
            # @fragment
            # @location(0)
            func fs(
                    coord: Vector4 # @builtin(frag_coord)
            ) -> Vector4:
                return Vector4(coord.x, coord.y, 0.0, 1.0)
            """);

        Assert.Contains("FRAGCOORD.x", code);
        Assert.Contains("FRAGCOORD.y", code);
    }

    [Fact]
    public void ComputeOnly_IsRejectedWithAnExplanation()
    {
        var exception = Assert.Throws<GodotBackendException>(() =>
            GodotSample("wgsl_style.gd").Length > 0 ? Godot("# @compute\n# @workgroup_size(1, 1, 1)\nfunc main() -> void:\n\tpass\n") : "");

        Assert.Contains("no compute stage", exception.Message);
    }

    [Fact]
    public void ComputeAlongsideVertex_IsIgnored()
    {
        string code = GodotSample("basic_color.gd");

        Assert.DoesNotContain("compute", code);
        Assert.Contains("void vertex()", code);
        Assert.Contains("void fragment()", code);
    }

    [Fact]
    public void FragmentInputWithoutAMatchingVertexOutput_IsRejected()
    {
        var exception = Assert.Throws<GodotBackendException>(() => Godot("""
            class In:
            	var color: Vector4 # @location(3)

            # @fragment
            # @location(0)
            func fs(input: In) -> Vector4:
            	return input.color
            """));

        Assert.Contains("no vertex output provides", exception.Message);
    }

    [Fact]
    public void StorageBuffers_AreRejected()
    {
        var exception = Assert.Throws<GodotBackendException>(() => Godot(VertexWith("pass",
            "class Data:\n    var value: float\n\n# @group(0) @binding(0) @storage_buffer\nvar data: Data")));

        Assert.Contains("StorageBuffer", exception.Message);
    }

    [Fact]
    public void TwoFragmentOutputs_AreRejected()
    {
        var exception = Assert.Throws<GodotBackendException>(() => Godot("""
            # @fragment
            # @location(1)
            func fs() -> Vector4:
            	return Vector4(0.0, 0.0, 0.0, 1.0)
            """));

        Assert.Contains("location 1", exception.Message);
    }

    [Fact]
    public void FloatLiterals_AlwaysLookLikeFloats()
    {
        string code = Godot(VertexWith("var a := 2.0\nvar b := 0.5\nvar c := 1e-5\nvar d := -3.0"));

        Assert.Contains("float a = 2.0;", code);
        Assert.Contains("float b = 0.5;", code);
        Assert.Contains("float c = 1.0e-05;", code);
        Assert.Contains("float d = -3.0;", code);
    }

    [Fact]
    public void GeneratedShadersStayDeterministic()
    {
        Assert.Equal(GodotSample("helper_functions.gd"), GodotSample("helper_functions.gd"));
    }

    [Fact]
    public void TheShaderExposesToGodotShaderLanguageOnTheCompiledResult()
    {
        var compiled = GdShaderCompiler.CompileFile(SamplePath("native_triangle.gd"));

        Assert.Equal(GodotSample("native_triangle.gd"), compiled.ToGodotShaderLanguage());
    }
}
