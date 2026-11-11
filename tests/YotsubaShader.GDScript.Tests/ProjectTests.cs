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

using YotsubaShader.GDScript.Project;

namespace YotsubaShader.GDScript.Tests;

/// <summary>A throwaway Godot project on disk.</summary>
internal sealed class TempProject : IDisposable
{
    public TempProject()
    {
        Root = Path.Combine(Path.GetTempPath(), "yotsuba-gd-project-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        File.WriteAllText(Path.Combine(Root, "project.godot"), "config_version=5\n");
    }

    public string Root { get; }

    public string Write(string relativePath, string content)
    {
        string path = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    public string Sample(string name, string? relativePath = null) =>
        Write(relativePath ?? $"shaders/{name}", File.ReadAllText(TestSupport.SamplePath(name)));

    public void Dispose() => Directory.Delete(Root, recursive: true);
}

public class ProjectTests
{
    [Fact]
    public void Find_AcceptsTheProjectFileTheFolderAndAPathInside()
    {
        using var project = new TempProject();
        string script = project.Write("a/b/c.gd", "");

        Assert.Equal(project.Root, GdProject.Find(Path.Combine(project.Root, "project.godot"))!.Root);
        Assert.Equal(project.Root, GdProject.Find(project.Root)!.Root);
        Assert.Equal(project.Root, GdProject.Find(script)!.Root);
        Assert.Equal(project.Root, GdProject.Find(Path.Combine(project.Root, "a", "b"))!.Root);
    }

    [Fact]
    public void Find_ReturnsNullOutsideAProject()
    {
        string folder = Path.Combine(Path.GetTempPath(), "yotsuba-gd-none-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            // Only meaningful when no ancestor of the temp folder is a Godot project.
            Assert.Null(GdProject.Find(folder));
        }
        finally
        {
            Directory.Delete(folder);
        }
    }

    [Fact]
    public void ResourcePaths_UseSlashesAndRes()
    {
        using var project = new TempProject();
        string script = project.Write("shaders/deep/x.gd", "");

        Assert.Equal("res://shaders/deep/x.gd", GdProject.Find(project.Root)!.ToResourcePath(script));
    }

    [Fact]
    public void Enumeration_FollowsGodotsRules()
    {
        using var project = new TempProject();
        project.Write("kept/a.gd", "");
        project.Write("root.gd", "");
        project.Write(".godot/cache.gd", "");
        project.Write(".hidden/h.gd", "");
        project.Write("ignored/i.gd", "");
        project.Write("ignored/.gdignore", "");
        project.Write("nested/project.godot", "");
        project.Write("nested/n.gd", "");
        project.Write("kept/sub/b.gd", "");

        var found = GdProject.Find(project.Root)!.EnumerateScripts()
            .Select(p => Path.GetRelativePath(project.Root, p).Replace('\\', '/'))
            .ToList();

        Assert.Equal(["root.gd", "kept/a.gd", "kept/sub/b.gd"], found);
    }

    [Theory]
    [InlineData("# @vertex\nfunc vs() -> void:\n\tpass\n", true)]
    [InlineData("# @fragment\nfunc fs() -> void:\n\tpass\n", true)]
    [InlineData("## @compute\nfunc cs() -> void:\n\tpass\n", true)]
    [InlineData("extends Node\nfunc _ready():\n\tpass\n", false)]
    [InlineData("# a note about @vertex\nfunc f():\n\tpass\n", false)]
    [InlineData("var s = \"# @vertex\"\n", false)]
    [InlineData("# @group(0) @binding(0) @uniform\nvar x: float\n", false)]
    public void IsShaderScript_LooksForEntryPointAnnotations(string source, bool expected) =>
        Assert.Equal(expected, GdProject.IsShaderScript(source));

    [Fact]
    public void IsShaderScript_SurvivesScriptsThatAreNotShaders()
    {
        const string ordinary = "extends Node3D\nsignal hit\n@export var speed := 5.0\nfunc _process(delta):\n\tposition.x += speed * delta\n";

        Assert.False(GdProject.IsShaderScript(ordinary));
    }
}
