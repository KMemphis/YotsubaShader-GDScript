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

using System.Diagnostics;
using YotsubaShader.GDScript.Project;

namespace YotsubaShader.GDScript.Tests;

/// <summary>
/// Compiles the generated Godot shaders with Godot's real renderer, using the methods that
/// <c>yotsuba-gd add</c> writes into each script. This opens a Godot window for a moment, so it only runs when
/// the environment variable YOTSUBA_GODOT_RENDER_TESTS=1 is set (and Godot is found: GODOT_PATH or
/// /Applications/Godot.app).
/// </summary>
public class GodotRenderingTests
{
    private const string Runner = """
        extends SceneTree

        func _initialize() -> void:
        	var count := 0
        	for file in DirAccess.open("res://shaders").get_files():
        		if not file.ends_with(".gd"):
        			continue
        		var script = load("res://shaders/" + file)
        		var material: ShaderMaterial = script.to_godot_shader_material()
        		var spirv: PackedByteArray = script.to_spirv()
        		if material == null or spirv.size() < 20 or spirv.decode_u32(0) != 0x07230203:
        			printerr("RUNNER FAILURE ", file)
        			continue
        		var instance := MeshInstance3D.new()
        		instance.mesh = QuadMesh.new()
        		instance.material_override = material
        		root.add_child(instance)
        		count += 1
        	await process_frame
        	await process_frame
        	await process_frame
        	print("DONE ", count)
        	quit()
        """;

    private static string? FindGodot()
    {
        string?[] candidates =
        [
            Environment.GetEnvironmentVariable("GODOT_PATH"),
            "/Applications/Godot.app/Contents/MacOS/Godot",
            "/Applications/Godot_mono.app/Contents/MacOS/Godot"
        ];
        return candidates.FirstOrDefault(c => !string.IsNullOrEmpty(c) && File.Exists(c));
    }

    [Fact]
    public void GeneratedShaders_CompileInGodot_AndTheEmbeddedMethodsWork()
    {
        if (Environment.GetEnvironmentVariable("YOTSUBA_GODOT_RENDER_TESTS") != "1") return;
        string? godot = FindGodot();
        if (godot is null) return;

        using var project = new TempProject();
        File.WriteAllText(Path.Combine(project.Root, "project.godot"),
            "config_version=5\n[application]\nconfig/name=\"t\"\n[rendering]\nrenderer/rendering_method=\"gl_compatibility\"\n");
        string[] samples = Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "samples"), "*.gd")
            .Select(Path.GetFileName).ToArray()!;
        foreach (string sample in samples) project.Sample(sample);
        project.Write("runner.gd", Runner);

        var results = GdProjectAdder.Add(GdProject.Find(project.Root)!);
        Assert.All(results, r => Assert.NotEqual(GdAddStatus.Failed, r.Status));

        var start = new ProcessStartInfo(godot)
        {
            ArgumentList = { "--path", project.Root, "--script", "res://runner.gd", "--quit-after", "10" },
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var process = Process.Start(start)!;
        string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(120_000), "Godot did not finish in time.");

        Assert.DoesNotContain("SHADER ERROR", output);
        Assert.DoesNotContain("SCRIPT ERROR", output);
        Assert.DoesNotContain("RUNNER FAILURE", output);
        Assert.Contains($"DONE {samples.Length}", output);
    }
}
