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
using YotsubaShader.GDScript.Parsing;
using YotsubaShader.GDScript.Syntax;

namespace YotsubaShader.GDScript.Tests;

public class ParserTests
{
    private static GdFile Parse(string source, out GdDiagnosticBag diagnostics)
    {
        diagnostics = new GdDiagnosticBag();
        return GdParser.Parse(source, diagnostics);
    }

    [Fact]
    public void ParsesStructsFunctionsAndAnnotations()
    {
        var file = Parse("""
            class_name Demo

            class Output:
                @SV_Position var position: Vector4

            @vertex
            func vs(@builtin(vertex_index) i: uint) -> Output:
                var o := Output.new()
                return o
            """, out var diagnostics);

        Assert.False(diagnostics.HasErrors, string.Join("\n", diagnostics.Items));
        Assert.Equal("Demo", file.ClassName);
        var structure = Assert.Single(file.Structs);
        Assert.Equal("position", Assert.Single(structure.Fields).Name);
        var function = Assert.Single(file.Functions);
        Assert.Equal("vertex", Assert.Single(function.Annotations).Name);
        Assert.Equal("builtin", Assert.Single(function.Parameters[0].Annotations).Name);
        Assert.Equal("Output", function.ReturnType);
    }

    [Fact]
    public void ParsesOperatorPrecedence()
    {
        var file = Parse("func f() -> int:\n    return 1 + 2 * 3\n", out _);
        var ret = Assert.IsType<GdReturnStmt>(file.Functions[0].Body[0]);
        var add = Assert.IsType<GdBinaryExpr>(ret.Value);

        Assert.Equal("+", add.Operator);
        Assert.Equal("*", Assert.IsType<GdBinaryExpr>(add.Right).Operator);
    }

    [Fact]
    public void ParsesTernaryAndWordOperators()
    {
        var file = Parse("func f(a: bool, b: bool) -> int:\n    return 1 if a and not b else 2\n", out var diagnostics);

        Assert.False(diagnostics.HasErrors);
        var ret = Assert.IsType<GdReturnStmt>(file.Functions[0].Body[0]);
        var ternary = Assert.IsType<GdTernaryExpr>(ret.Value);
        Assert.Equal("&&", Assert.IsType<GdBinaryExpr>(ternary.Condition).Operator);
    }

    [Fact]
    public void ParsesElifChain()
    {
        var file = Parse("func f(a: int):\n    if a == 1:\n        pass\n    elif a == 2:\n        pass\n    else:\n        pass\n", out var diagnostics);

        Assert.False(diagnostics.HasErrors);
        var branch = Assert.IsType<GdIfStmt>(file.Functions[0].Body[0]);
        var elif = Assert.IsType<GdIfStmt>(Assert.Single(branch.Else!));
        Assert.NotNull(elif.Else);
    }

    [Theory]
    [InlineData("match")]
    [InlineData("await")]
    [InlineData("signal")]
    public void UnsupportedKeywords_AreReported(string keyword)
    {
        Parse($"func f():\n    {keyword} x\n", out var diagnostics);

        Assert.Contains(diagnostics.Items, d => d.Message.Contains($"'{keyword}' is not supported"));
    }

    [Fact]
    public void SyntaxErrors_DoNotThrowAndAreReportedWithPositions()
    {
        Parse("func f(:\n    return\n", out var diagnostics);

        Assert.True(diagnostics.HasErrors);
        Assert.All(diagnostics.Items, d => Assert.True(d.Position.Line >= 1));
    }
}
