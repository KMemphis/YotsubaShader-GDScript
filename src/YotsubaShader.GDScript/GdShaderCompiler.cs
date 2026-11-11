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

using YotsubaShader.Compiler.SpirV;
using YotsubaShader.GDScript.Annotations;
using YotsubaShader.GDScript.Diagnostics;
using YotsubaShader.GDScript.Lowering;
using YotsubaShader.GDScript.Parsing;
using YotsubaShader.IR.Intermediate;

namespace YotsubaShader.GDScript;

/// <summary>
/// The GDScript frontend of Yotsuba Shader: GDScript source → Yotsuba IR → SPIR-V.
/// The IR to SPIR-V step is the same one the C# frontend uses.
/// </summary>
public static class GdShaderCompiler
{
    /// <summary>Parses and lowers a script to IR. Throws <see cref="GdCompilationException"/> on errors.</summary>
    public static (string Name, Shader Shader) ToIntermediateLanguage(
        string source, string fallbackName = "Shader", string? fileName = null)
    {
        var diagnostics = new GdDiagnosticBag();
        var file = GdParser.Parse(source, diagnostics);

        Shader? shader = diagnostics.HasErrors ? null : new GdLowerer(file, diagnostics).Lower();
        if (shader is null || diagnostics.HasErrors)
        {
            throw new GdCompilationException(fileName, diagnostics.Items);
        }

        return (file.ClassName ?? fallbackName, shader.Value);
    }

    /// <summary>Compiles a script to IR and SPIR-V.</summary>
    public static GdCompiledShader Compile(string source, string fallbackName = "Shader", string? fileName = null)
    {
        var (name, shader) = ToIntermediateLanguage(source, fallbackName, fileName);
        var module = new IRToSpirvModule().Compile(shader);
        byte[] spirV = new SpirVCompiler().Compile(module);
        return new GdCompiledShader(name, shader, spirV, module.ToSpirVAs);
    }

    /// <summary>Compiles a <c>.gd</c> file; the file name is the fallback shader name.</summary>
    public static GdCompiledShader CompileFile(string path) =>
        Compile(File.ReadAllText(path), Path.GetFileNameWithoutExtension(path), path);

    /// <summary>The annotation vocabulary used by the frontend.</summary>
    public static AnnotationVocabulary Vocabulary => AnnotationVocabulary.Default;
}
