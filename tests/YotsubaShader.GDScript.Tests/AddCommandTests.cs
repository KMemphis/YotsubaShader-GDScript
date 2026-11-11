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
using YotsubaShader.GDScript.Project;

namespace YotsubaShader.GDScript.Tests;

public class AddCommandTests
{
    private const string Player = "extends Node3D\nsignal hit\n@export var speed := 5.0\nfunc _process(delta):\n\tposition.x += speed * delta\n";

    [Fact]
    public void Add_UpdatesShaderScriptsOnly()
    {
        using var project = new TempProject();
        string shader = project.Sample("native_triangle.gd");
        string player = project.Write("scripts/player.gd", Player);

        var results = GdProjectAdder.Add(GdProject.Find(project.Root)!);

        var result = Assert.Single(results);
        Assert.Equal(GdAddStatus.Updated, result.Status);
        Assert.Equal("res://shaders/native_triangle.gd", result.ResourcePath);
        Assert.Equal("NativeTriangle", result.ShaderName);
        Assert.Contains("static func to_godot_shader_material() -> ShaderMaterial:", File.ReadAllText(shader));
        Assert.Equal(Player, File.ReadAllText(player));
    }

    [Fact]
    public void Add_IsIdempotent()
    {
        using var project = new TempProject();
        string shader = project.Sample("helper_functions.gd");
        GdProjectAdder.Add(GdProject.Find(project.Root)!);
        string first = File.ReadAllText(shader);

        var results = GdProjectAdder.Add(GdProject.Find(project.Root)!);

        Assert.Equal(GdAddStatus.Unchanged, Assert.Single(results).Status);
        Assert.Equal(first, File.ReadAllText(shader));
        Assert.Equal(1, CountOccurrences(first, GodotGeneratedBlock.BeginMarker));
    }

    [Fact]
    public void Add_DryRunWritesNothing()
    {
        using var project = new TempProject();
        string shader = project.Sample("native_triangle.gd");
        string before = File.ReadAllText(shader);

        var results = GdProjectAdder.Add(GdProject.Find(project.Root)!, dryRun: true);

        Assert.Equal(GdAddStatus.Updated, Assert.Single(results).Status);
        Assert.Equal(before, File.ReadAllText(shader));
    }

    [Fact]
    public void Add_ReplacesAStaleBlock()
    {
        using var project = new TempProject();
        string shader = project.Sample("native_triangle.gd");
        GdProjectAdder.Add(GdProject.Find(project.Root)!);

        // Change the shader, keep the old block.
        File.WriteAllText(shader, File.ReadAllText(shader).Replace("Vector4(1.0, 0.5, 0.25, 1.0)", "Vector4(0.0, 1.0, 0.0, 1.0)"));
        var results = GdProjectAdder.Add(GdProject.Find(project.Root)!);

        string text = File.ReadAllText(shader);
        Assert.Equal(GdAddStatus.Updated, Assert.Single(results).Status);
        Assert.Equal(1, CountOccurrences(text, GodotGeneratedBlock.BeginMarker));
        Assert.Contains("vec4(0.0, 1.0, 0.0, 1.0)", text);
        Assert.DoesNotContain("vec4(1.0, 0.5, 0.25, 1.0)", text);
    }

    [Fact]
    public void AScriptWithAGeneratedBlock_StillCompilesToTheSameShader()
    {
        using var project = new TempProject();
        string shader = project.Sample("helper_functions.gd");
        var expected = GdShaderCompiler.CompileFile(shader);
        GdProjectAdder.Add(GdProject.Find(project.Root)!);

        var again = GdShaderCompiler.CompileFile(shader);

        Assert.Equal(expected.SpirV, again.SpirV);
        Assert.Equal(expected.ToGodotShaderLanguage(), again.ToGodotShaderLanguage());
    }

    [Fact]
    public void Add_EmbedsTheGodotCodeAndTheSpirV()
    {
        using var project = new TempProject();
        string shader = project.Sample("native_triangle.gd");
        var compiled = GdShaderCompiler.CompileFile(shader);

        GdProjectAdder.Add(GdProject.Find(project.Root)!);
        string text = File.ReadAllText(shader);

        Assert.Contains(compiled.ToGodotShaderLanguage().TrimEnd(), text);
        string base64 = string.Concat(System.Text.RegularExpressions.Regex
            .Matches(text.Split("const _YOTSUBA_SPIRV_BASE64 := (")[1].Split("# </yotsuba:generated>")[0], "\"([A-Za-z0-9+/=]+)\"")
            .Select(m => m.Groups[1].Value));
        Assert.Equal(compiled.SpirV, Convert.FromBase64String(base64));
    }

    [Fact]
    public void AComputeShader_GetsStubsThatExplainWhy_AndStillHasSpirV()
    {
        using var project = new TempProject();
        string shader = project.Write("compute.gd", "class_name Noise\n\n# @compute\n# @workgroup_size(8, 8, 1)\nfunc main() -> void:\n\tpass\n");

        var result = Assert.Single(GdProjectAdder.Add(GdProject.Find(project.Root)!));

        Assert.Equal(GdAddStatus.Updated, result.Status);
        Assert.True(result.GodotErrorIsExpected);
        string text = File.ReadAllText(shader);
        Assert.Contains("push_error(", text);
        Assert.Contains("static func to_spirv() -> PackedByteArray:", text);
        Assert.DoesNotContain("_YOTSUBA_GODOT_SHADER_CODE", text);
    }

    [Fact]
    public void AScriptWithErrors_IsReportedAndLeftUntouched()
    {
        using var project = new TempProject();
        const string broken = "# @vertex\nfunc vs() -> Vector4:\n\treturn sin(1.0)\n";
        string shader = project.Write("broken.gd", broken);

        var result = Assert.Single(GdProjectAdder.Add(GdProject.Find(project.Root)!));

        Assert.Equal(GdAddStatus.Failed, result.Status);
        Assert.NotEmpty(result.Diagnostics);
        Assert.Equal(broken, File.ReadAllText(shader));
    }

    [Fact]
    public void Add_KeepsTheIndentationOfTheScript()
    {
        using var project = new TempProject();
        string spaces = File.ReadAllText(TestSupport.SamplePath("native_triangle.gd")).Replace("\t", "    ");
        string shader = project.Write("spaces.gd", spaces);

        GdProjectAdder.Add(GdProject.Find(project.Root)!);
        string text = File.ReadAllText(shader);

        Assert.Contains("    return _YOTSUBA_GODOT_SHADER_CODE", text);
        Assert.DoesNotContain("\treturn _YOTSUBA_GODOT_SHADER_CODE", text);
    }

    [Fact]
    public void Add_PreservesWindowsLineEndings()
    {
        using var project = new TempProject();
        string shader = project.Write("crlf.gd", File.ReadAllText(TestSupport.SamplePath("native_triangle.gd")).Replace("\n", "\r\n"));

        GdProjectAdder.Add(GdProject.Find(project.Root)!);
        string text = File.ReadAllText(shader);

        Assert.DoesNotContain("\n", text.Replace("\r\n", ""));
        Assert.Contains("# <yotsuba:generated>", text);
    }

    [Fact]
    public void Strip_KeepsLineNumbers()
    {
        string source = "a\n# <yotsuba:generated>\nx\ny\n# </yotsuba:generated>\nb\n";

        string stripped = GodotGeneratedBlock.Strip(source);

        Assert.Equal(source.Count(c => c == '\n'), stripped.Count(c => c == '\n'));
        Assert.DoesNotContain("x", stripped);
        Assert.StartsWith("a\n", stripped);
        Assert.EndsWith("b\n", stripped);
    }

    [Theory]
    [InlineData("func f():\n\tpass\n", "\t")]
    [InlineData("func f():\n    pass\n", "    ")]
    [InlineData("func f():\n  pass\n", "  ")]
    [InlineData("var x = 1\n", "\t")]
    public void DetectIndent_ReadsTheScriptsStyle(string source, string expected) =>
        Assert.Equal(expected, GodotGeneratedBlock.DetectIndent(source));

    private static int CountOccurrences(string text, string value) =>
        (text.Length - text.Replace(value, "").Length) / value.Length;
}
