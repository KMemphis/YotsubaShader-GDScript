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

namespace YotsubaShader.GDScript.Tests;

public class AnnotationCommentTests
{
    private static Syntax.GdFile Parse(string source, out GdDiagnosticBag diagnostics)
    {
        diagnostics = new GdDiagnosticBag();
        return GdParser.Parse(source, diagnostics);
    }

    [Fact]
    public void LeadingComments_AttachToTheNextDeclaration()
    {
        var file = Parse("# note\n# @vertex\n# @location(0)\nfunc vs() -> int:\n\treturn 1\n", out var diagnostics);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal(["vertex", "location"], file.Functions[0].Annotations.Select(a => a.Name));
    }

    [Fact]
    public void TrailingComments_AttachToFieldsAndParameters()
    {
        var file = Parse("""
            class S:
            	var a: Vector4 # @builtin(position)
            	var b: float # @location(1)
            	var c: float

            func f(
            		x: int # @builtin(vertex_index)
            		, y: int
            ) -> void:
            	pass
            """.Replace("\t", "\t"), out var diagnostics);

        Assert.False(diagnostics.HasErrors, string.Join("\n", diagnostics.Items));
        var fields = file.Structs[0].Fields;
        Assert.Equal("builtin", Assert.Single(fields[0].Annotations).Name);
        Assert.Equal("location", Assert.Single(fields[1].Annotations).Name);
        Assert.Empty(fields[2].Annotations);
        Assert.Equal("builtin", Assert.Single(file.Functions[0].Parameters[0].Annotations).Name);
        Assert.Empty(file.Functions[0].Parameters[1].Annotations);
    }

    [Fact]
    public void ATrailingCommentDoesNotLeakToTheNextDeclaration()
    {
        var file = Parse("class S:\n\tvar a: float # @location(0)\n\tvar b: float\n", out _);

        Assert.Empty(file.Structs[0].Fields[1].Annotations);
    }

    [Fact]
    public void ABlankLineSeparatesALeadingBlockFromTheDeclaration()
    {
        var file = Parse("# @vertex\n\nfunc f() -> void:\n\tpass\n", out _);

        Assert.Empty(file.Functions[0].Annotations);
    }

    [Fact]
    public void MultipleAnnotationsInOneComment_AreAllRead()
    {
        var file = Parse("# @group(0) @binding(2) @uniform\nvar x: float\n", out var diagnostics);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal(["group", "binding", "uniform"], file.Globals[0].Annotations.Select(a => a.Name));
    }

    [Fact]
    public void PlainComments_AreNotAnnotations()
    {
        var file = Parse("# a normal comment about @vertex shaders\n# email me @ home\nfunc f() -> void:\n\tpass\n", out var diagnostics);

        Assert.False(diagnostics.HasErrors);
        Assert.Empty(file.Functions[0].Annotations);
    }

    [Fact]
    public void BadAnnotationComments_ReportThePositionInsideTheComment()
    {
        Parse("func f() -> void:\n\tpass\n# @vertex(\nfunc g() -> void:\n\tpass\n", out var diagnostics);

        Assert.True(diagnostics.Items.Count == 0 || diagnostics.Items.All(d => d.Position.Line >= 1));
    }

    [Fact]
    public void DoubleHashDocComments_AreAccepted()
    {
        var file = Parse("## @vertex\nfunc f() -> int:\n\treturn 1\n", out _);

        Assert.Equal("vertex", Assert.Single(file.Functions[0].Annotations).Name);
    }
}
