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

using YotsubaShader.GDScript.Annotations;
using YotsubaShader.GDScript.Diagnostics;
using YotsubaShader.GDScript.Lexing;

namespace YotsubaShader.GDScript.Project;

/// <summary>A Godot project on disk: the folder that contains <c>project.godot</c>, which is <c>res://</c>.</summary>
public sealed class GdProject
{
    public const string ProjectFileName = "project.godot";
    public const string IgnoreFileName = ".gdignore";

    private GdProject(string root)
    {
        Root = root;
    }

    /// <summary>The absolute path of the folder that contains <c>project.godot</c>.</summary>
    public string Root { get; }

    /// <summary>
    /// Finds the project for a path: a <c>project.godot</c> file, a folder that contains one, or a path inside a
    /// project (the folders above it are searched). Returns null when there is no project.
    /// </summary>
    public static GdProject? Find(string path)
    {
        string full = Path.GetFullPath(path);
        if (File.Exists(full))
        {
            if (Path.GetFileName(full) == ProjectFileName) return new GdProject(Path.GetDirectoryName(full)!);
            full = Path.GetDirectoryName(full)!;
        }

        for (string? directory = full; directory is not null; directory = Path.GetDirectoryName(directory))
        {
            if (File.Exists(Path.Combine(directory, ProjectFileName))) return new GdProject(directory);
        }

        return null;
    }

    /// <summary>Converts a disk path inside the project to a <c>res://</c> path.</summary>
    public string ToResourcePath(string path) =>
        "res://" + Path.GetRelativePath(Root, Path.GetFullPath(path)).Replace(Path.DirectorySeparatorChar, '/');

    /// <summary>
    /// Every <c>.gd</c> file of the project, following Godot's rules: the <c>.godot</c> cache and hidden folders
    /// are skipped, as are folders that hold a <c>.gdignore</c> file or another <c>project.godot</c>.
    /// </summary>
    public IEnumerable<string> EnumerateScripts() => EnumerateScripts(Root, isRoot: true);

    /// <summary>The same enumeration for any folder (used when no project file exists).</summary>
    public static IEnumerable<string> EnumerateScripts(string directory, bool isRoot)
    {
        if (!isRoot)
        {
            string name = Path.GetFileName(directory);
            if (name.StartsWith('.') ||
                File.Exists(Path.Combine(directory, IgnoreFileName)) ||
                File.Exists(Path.Combine(directory, ProjectFileName)))
            {
                yield break;
            }
        }

        foreach (string file in Directory.EnumerateFiles(directory, "*.gd").OrderBy(f => f, StringComparer.Ordinal))
        {
            yield return file;
        }

        foreach (string child in Directory.EnumerateDirectories(directory).OrderBy(d => d, StringComparer.Ordinal))
        {
            foreach (string file in EnumerateScripts(child, isRoot: false)) yield return file;
        }
    }

    /// <summary>
    /// True when the script declares a shader entry point (<c># @vertex</c>, <c># @fragment</c> or
    /// <c># @compute</c>). A Godot project is full of ordinary scripts; those are left alone.
    /// </summary>
    public static bool IsShaderScript(string source)
    {
        var lexer = new GdLexer(GodotGeneratedBlock.Strip(source), new GdDiagnosticBag());
        lexer.Tokenize();
        foreach (var comment in lexer.Comments)
        {
            if (comment.AnnotationText is not { } text) continue;
            int end = 1;
            while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_')) end++;
            if (AnnotationVocabulary.Default.TryGet(text[1..end], out var definition) &&
                definition.Kind == AnnotationKind.Stage)
            {
                return true;
            }
        }

        return false;
    }
}
