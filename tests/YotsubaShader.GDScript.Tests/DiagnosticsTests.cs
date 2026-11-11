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

using static YotsubaShader.GDScript.Tests.TestSupport;

namespace YotsubaShader.GDScript.Tests;

/// <summary>
/// Anything the IR cannot represent yet must be rejected by the frontend with a clear message; the IR is never
/// extended from this repository.
/// </summary>
public class DiagnosticsTests
{
    [Theory]
    [InlineData("var x := sin(1.0)", "'sin' is an intrinsic")]
    [InlineData("var x := dot(Vector2(1.0, 2.0), Vector2(1.0, 2.0))", "'dot' is an intrinsic")]
    public void Intrinsics_AreRejected(string body, string expected) =>
        Assert.Contains(Errors(VertexWith(body)), m => m.Contains(expected));

    [Fact]
    public void VectorArithmetic_IsRejected() =>
        Assert.Contains(Errors(VertexWith("var a := Vector2(1.0, 2.0)\nvar b := a + a")),
            m => m.Contains("not supported by the IR yet"));

    [Fact]
    public void VectorComponentWrite_IsRejected() =>
        Assert.Contains(Errors(VertexWith("var a := Vector2(1.0, 2.0)\na.x = 3.0")),
            m => m.Contains("Writing to a vector component"));

    [Fact]
    public void Swizzle_IsRejected() =>
        Assert.Contains(Errors(VertexWith("var a := Vector4(1.0, 2.0, 3.0, 4.0)\nvar b := a.xy")),
            m => m.Contains("Swizzles"));

    [Fact]
    public void ComputeWithoutWorkgroupSize_IsRejected() =>
        Assert.Contains(Errors("@compute\nfunc main() -> void:\n    pass\n"), m => m.Contains("needs @workgroup_size"));

    [Fact]
    public void UnknownAnnotation_IsRejected() =>
        Assert.Contains(Errors("""
            class Out:
                @nonsense var position: Vector4

            @vertex
            func vs() -> Out:
                return Out.new()
            """), m => m.Contains("Unknown annotation '@nonsense'"));

    [Fact]
    public void BuiltInsWithoutIrEquivalent_AreRejected() =>
        Assert.Contains(Errors("""
            class Out:
                @gl_ClipDistance var position: Vector4

            @vertex
            func vs() -> Out:
                return Out.new()
            """), m => m.Contains("not supported by the IR yet"));

    [Fact]
    public void BuiltInInTheWrongStage_IsRejected() =>
        Assert.Contains(Errors("""
            @vertex
            func vs(@builtin(global_invocation_id) id: Vector3u) -> Vector4:
                return Vector4(0.0, 0.0, 0.0, 1.0)
            """), m => m.Contains("can only be used in a compute shader"));

    [Fact]
    public void MixingIntAndUint_NeedsAConversion() =>
        Assert.Contains(Errors(VertexWith("var a := 1\nvar b: uint = 2u\nvar c := a + b")),
            m => m.Contains("Mixing 'int' and 'uint'"));

    [Fact]
    public void FloatToIntAssignment_NeedsAnExplicitConversion() =>
        Assert.Contains(Errors(VertexWith("var a: int = 1.5")), m => m.Contains("Cannot convert 'float' to 'int'"));

    [Fact]
    public void UnknownIdentifier_IsReportedWithPosition()
    {
        var exception = Assert.Throws<YotsubaShader.GDScript.Diagnostics.GdCompilationException>(() =>
            YotsubaShader.GDScript.GdShaderCompiler.ToIntermediateLanguage(VertexWith("var a := missing")));

        var diagnostic = Assert.Single(exception.Diagnostics);
        Assert.Contains("Unknown identifier 'missing'", diagnostic.Message);
        Assert.True(diagnostic.Position.Line > 0 && diagnostic.Position.Column > 0);
    }

    [Fact]
    public void MissingReturn_IsReported() =>
        Assert.Contains(Errors("""
            func f(a: float) -> float:
                if a > 0.0:
                    return a

            class Out:
                @builtin(position) var position: Vector4

            @vertex
            func vs() -> Out:
                var o := Out.new()
                o.position = Vector4(f(1.0), 0.0, 0.0, 1.0)
                return o
            """), m => m.Contains("Not all code paths"));

    [Fact]
    public void Recursion_IsRejected() =>
        Assert.Contains(Errors("""
            func f(a: float) -> float:
                return f(a)

            class Out:
                @builtin(position) var position: Vector4

            @vertex
            func vs() -> Out:
                var o := Out.new()
                o.position = Vector4(f(1.0), 0.0, 0.0, 1.0)
                return o
            """), m => m.Contains("Recursion"));

    [Fact]
    public void WritingToAUniform_IsRejected() =>
        Assert.Contains(Errors(VertexWith("exposure = 2.0", "@group(0) @binding(0) @uniform var exposure: float")),
            m => m.Contains("read-only"));

    [Fact]
    public void ResourceWithoutBinding_IsRejected() =>
        Assert.Contains(Errors(VertexWith("var a := exposure", "@uniform var exposure: float")),
            m => m.Contains("needs a binding"));

    [Fact]
    public void TexturesAndMatrices_AreRejectedWithAReason()
    {
        Assert.Contains(Errors(VertexWith("pass", "@group(0) @binding(1) @uniform var albedo: Texture2D")),
            m => m.Contains("sampling operations"));
        Assert.Contains(Errors(VertexWith("var m: Matrix4x4")), m => m.Contains("matrix values"));
    }

    [Fact]
    public void ScriptWithoutEntryPoint_IsRejected() =>
        Assert.Contains(Errors("func f() -> void:\n    pass\n"), m => m.Contains("no entry point"));

    [Fact]
    public void BreakOutsideLoop_IsRejected() =>
        Assert.Contains(Errors(VertexWith("break")), m => m.Contains("only valid inside a loop"));

    [Fact]
    public void Indexing_IsRejected() =>
        Assert.Contains(Errors(VertexWith("var a := Vector2(1.0, 2.0)\nvar b := a[0]")),
            m => m.Contains("Indexing is not supported"));
}
