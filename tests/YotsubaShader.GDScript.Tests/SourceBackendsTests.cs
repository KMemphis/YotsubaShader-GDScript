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

using YotsubaShader.IR.Intermediate;
using static YotsubaShader.GDScript.Tests.TestSupport;

namespace YotsubaShader.GDScript.Tests;

/// <summary>
/// The backends of the main repository, reached from a GDScript shader: HLSL, GLSL, GLSL ES, Metal and C++
/// through YotsubaShaderCross, from the SPIR-V this frontend produces.
/// </summary>
public class SourceBackendsTests
{
    private static GdCompiledShader BasicColor() => GdShaderCompiler.CompileFile(SamplePath("basic_color.gd"));

    [Fact]
    public void Stages_ListsTheEntryPointStagesInDeclarationOrder() =>
        Assert.Equal([Stage.Vertex, Stage.Fragment, Stage.Compute], BasicColor().Stages);

    [Fact]
    public void Hlsl_ComesOutForEveryStage()
    {
        var shader = BasicColor();

        foreach (Stage stage in shader.Stages)
        {
            string hlsl = shader.ToHlsl(stage);
            Assert.False(string.IsNullOrWhiteSpace(hlsl), $"{stage} produced no HLSL");
        }

        Assert.Contains("float4", shader.ToHlsl(Stage.Vertex));
    }

    [Fact]
    public void Glsl_IsVersioned_AndTheVersionIsConfigurable()
    {
        var shader = BasicColor();

        Assert.StartsWith("#version 450", shader.ToGlsl(Stage.Vertex).TrimStart());
        Assert.StartsWith("#version 430", shader.ToGlsl(Stage.Vertex, 430).TrimStart());
        Assert.Contains("gl_Position", shader.ToGlsl(Stage.Vertex));
    }

    [Fact]
    public void GlslEs_UsesAnEsVersion()
    {
        string es = BasicColor().ToGlslEs(Stage.Fragment);

        Assert.Contains("#version 310 es", es);
    }

    [Fact]
    public void Msl_And_Cpp_ComeOutForEveryStage()
    {
        var shader = BasicColor();

        foreach (Stage stage in shader.Stages)
        {
            Assert.Contains("#include", shader.ToMsl(stage));
            Assert.False(string.IsNullOrWhiteSpace(shader.ToCpp(stage)));
        }
    }

    [Fact]
    public void AStageWithoutAnEntryPoint_IsReportedByTheBackend()
    {
        var shader = GdShaderCompiler.CompileFile(SamplePath("native_triangle.gd"));

        Assert.DoesNotContain(Stage.Compute, shader.Stages);
        Assert.Throws<InvalidOperationException>(() => shader.ToHlsl(Stage.Compute));
    }

    [Fact]
    public void TheGodotAndTheSourceBackendsShareTheSameShader()
    {
        var shader = GdShaderCompiler.CompileFile(SamplePath("helper_functions.gd"));

        Assert.False(string.IsNullOrWhiteSpace(shader.ToGodotShaderLanguage()));
        Assert.False(string.IsNullOrWhiteSpace(shader.ToGlsl(Stage.Fragment)));
        Assert.False(string.IsNullOrWhiteSpace(shader.ToHlsl(Stage.Vertex)));
    }
}
