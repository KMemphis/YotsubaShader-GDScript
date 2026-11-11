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
using YotsubaShader.Godot;

namespace YotsubaShader.GDScript.Project;

public enum GdAddStatus
{
    /// <summary>The script was written with a new or refreshed generated block.</summary>
    Updated,
    /// <summary>The block was already up to date.</summary>
    Unchanged,
    /// <summary>The script has errors; it was not modified.</summary>
    Failed
}

/// <param name="Path">The script on disk.</param>
/// <param name="ResourcePath">The same script as <c>res://</c>.</param>
/// <param name="ShaderName">The shader name (<c>class_name</c> or file name).</param>
/// <param name="Status">What happened.</param>
/// <param name="Diagnostics">Compile errors when <paramref name="Status"/> is Failed.</param>
/// <param name="GodotError">Why Godot shading language could not be produced, if so.</param>
/// <param name="GodotErrorIsExpected">True for compute shaders, which Godot shaders cannot express by design.</param>
public sealed record GdAddResult(
    string Path,
    string ResourcePath,
    string ShaderName,
    GdAddStatus Status,
    IReadOnlyList<GdDiagnostic> Diagnostics,
    string? GodotError,
    bool GodotErrorIsExpected);

/// <summary>Implements <c>yotsuba-gd add</c>: compile every shader of a Godot project and embed the result.</summary>
public static class GdProjectAdder
{
    public static IReadOnlyList<GdAddResult> Add(GdProject project, bool dryRun = false)
    {
        var results = new List<GdAddResult>();
        foreach (string path in project.EnumerateScripts())
        {
            string source = File.ReadAllText(path);
            if (!GdProject.IsShaderScript(source)) continue;

            string resource = project.ToResourcePath(path);
            string fallback = System.IO.Path.GetFileNameWithoutExtension(path);
            GdCompiledShader compiled;
            try
            {
                compiled = GdShaderCompiler.Compile(source, fallback, resource);
            }
            catch (GdCompilationException exception)
            {
                results.Add(new GdAddResult(path, resource, fallback, GdAddStatus.Failed, exception.Diagnostics, null, false));
                continue;
            }

            string? code = null;
            string? error = null;
            bool expected = false;
            try
            {
                code = compiled.ToGodotShaderLanguage();
            }
            catch (GodotBackendException exception)
            {
                error = exception.Message;
                expected = compiled.IntermediateLanguage.Functions.Any(
                    f => f.Stage == YotsubaShader.IR.Intermediate.Stage.Compute);
            }

            string block = GodotGeneratedBlock.Render(
                compiled.Name, code, error, compiled.SpirV, GodotGeneratedBlock.DetectIndent(source));
            string updated = GodotGeneratedBlock.Apply(source, block);

            var status = GdAddStatus.Unchanged;
            if (!string.Equals(updated, source, StringComparison.Ordinal))
            {
                status = GdAddStatus.Updated;
                if (!dryRun) File.WriteAllText(path, updated);
            }

            results.Add(new GdAddResult(path, resource, compiled.Name, status, [], error, expected));
        }

        return results;
    }
}
