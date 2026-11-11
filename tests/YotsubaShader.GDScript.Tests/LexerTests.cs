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
using YotsubaShader.GDScript.Lexing;

namespace YotsubaShader.GDScript.Tests;

public class LexerTests
{
    private static List<GdToken> Lex(string source, out GdDiagnosticBag diagnostics)
    {
        diagnostics = new GdDiagnosticBag();
        return new GdLexer(source, diagnostics).Tokenize().ToList();
    }

    [Fact]
    public void Indentation_ProducesIndentAndDedent()
    {
        var tokens = Lex("func f():\n    return 1\nvar x = 2\n", out var diagnostics);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal(1, tokens.Count(t => t.Kind == GdTokenKind.Indent));
        Assert.Equal(1, tokens.Count(t => t.Kind == GdTokenKind.Dedent));
        Assert.Equal(GdTokenKind.EndOfFile, tokens[^1].Kind);
    }

    [Fact]
    public void NewlinesInsideParentheses_AreIgnored()
    {
        var tokens = Lex("var v = Vector2(1.0,\n    2.0)\n", out _);

        Assert.Single(tokens, t => t.Kind == GdTokenKind.Newline);
        Assert.DoesNotContain(tokens, t => t.Kind == GdTokenKind.Indent);
    }

    [Fact]
    public void CommentsAndBlankLines_DoNotAffectIndentation()
    {
        var tokens = Lex("func f():\n\n    # note\n    pass\n", out var diagnostics);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal(1, tokens.Count(t => t.Kind == GdTokenKind.Indent));
    }

    [Fact]
    public void Numbers_AreClassified()
    {
        var tokens = Lex("1 2.5 .5 1e3 0x1F 7u\n", out _);
        var numbers = tokens.Where(t => t.Kind is GdTokenKind.Integer or GdTokenKind.Float).ToList();

        Assert.Equal(
            [GdTokenKind.Integer, GdTokenKind.Float, GdTokenKind.Float, GdTokenKind.Float, GdTokenKind.Integer, GdTokenKind.Integer],
            numbers.Select(t => t.Kind));
        Assert.Equal("7u", numbers[^1].Text);
    }

    [Fact]
    public void MixedTabsAndSpaces_IsReported()
    {
        Lex("func f():\n    pass\n\tpass\n", out var diagnostics);

        Assert.Contains(diagnostics.Items, d => d.Message.Contains("Mixed tabs and spaces"));
    }

    [Fact]
    public void CompoundOperators_AreSingleTokens()
    {
        var tokens = Lex("a <<= 1\nb := 2\nc -> d\n", out _);

        Assert.Contains(tokens, t => t.Is("<<="));
        Assert.Contains(tokens, t => t.Is(":="));
        Assert.Contains(tokens, t => t.Is("->"));
    }
}
