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

using YotsubaShader.GDScript.Diagnostics;
using static YotsubaShader.GDScript.Tests.TestSupport;

namespace YotsubaShader.GDScript.Tests;

public class EndToEndTests
{
    public static IEnumerable<object[]> Samples() =>
        Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "samples"), "*.gd")
            .Select(p => new object[] { Path.GetFileName(p) });

    [Theory]
    [MemberData(nameof(Samples))]
    public void EverySample_CompilesToSpirV(string sample)
    {
        var compiled = GdShaderCompiler.CompileFile(SamplePath(sample));

        Assert.True(compiled.SpirV.Length > 20 && compiled.SpirV.Length % 4 == 0);
        Assert.Equal(0x07230203u, BitConverter.ToUInt32(compiled.SpirV, 0));
        Assert.Contains("OpEntryPoint", compiled.SpirVAs);
        Assert.NotEmpty(compiled.IntermediateLanguage.Functions);
    }

    [Fact]
    public void ShaderName_ComesFromClassNameOrFallback()
    {
        Assert.Equal("BasicColorShader", GdShaderCompiler.CompileFile(SamplePath("basic_color.gd")).Name);
        var anonymous = GdShaderCompiler.Compile(VertexWith("pass"), "FromFileName");
        Assert.Equal("FromFileName", anonymous.Name);
    }

    [Fact]
    public void CompilationIsRepeatableAndHasNoSharedState()
    {
        var source = File.ReadAllText(SamplePath("helper_functions.gd"));

        var first = GdShaderCompiler.Compile(source);
        var second = GdShaderCompiler.Compile(source);

        Assert.Equal(first.SpirV, second.SpirV);
    }

    [Fact]
    public void ErrorsCarryTheFileNameInTheirMessage()
    {
        var exception = Assert.Throws<GdCompilationException>(() =>
            GdShaderCompiler.Compile("func f():\n    return 1 +\n", fileName: "broken.gd"));

        Assert.Contains("broken.gd(", exception.Message);
        Assert.Contains("error", exception.Message);
    }
}
