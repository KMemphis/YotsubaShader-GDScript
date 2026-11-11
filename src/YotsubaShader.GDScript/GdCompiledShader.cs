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

using YotsubaShader.Compiler;
using YotsubaShader.IR.Intermediate;

namespace YotsubaShader.GDScript;

/// <summary>The result of compiling one GDScript shader.</summary>
/// <param name="Name">The shader name (<c>class_name</c> or the file name).</param>
/// <param name="IntermediateLanguage">The Yotsuba IR produced by the frontend.</param>
/// <param name="SpirV">The SPIR-V binary.</param>
/// <param name="SpirVAs">The SPIR-V assembly text.</param>
public sealed record GdCompiledShader(string Name, Shader IntermediateLanguage, byte[] SpirV, string SpirVAs)
{
    /// <summary>
    /// The shader as Godot shading language (a <c>spatial</c> shader). Throws
    /// <see cref="YotsubaShader.Godot.GodotBackendException"/> when the IR cannot be expressed in Godot
    /// (for example a compute shader).
    /// </summary>
    public string ToGodotShaderLanguage() => YotsubaShader.Godot.GodotShaderWriter.Write(IntermediateLanguage, Name);

    /// <summary>
    /// The stages this shader has entry points for (Vertex, Fragment, Compute), in declaration order.
    /// </summary>
    public IReadOnlyList<Stage> Stages => IntermediateLanguage.Functions
        .Select(function => function.Stage)
        .Where(stage => stage != Stage.None)
        .Distinct()
        .ToList();

    // The main repository's own emission path: SPIR-V in, source text out (YotsubaShaderCross). The same
    // CompiledCode the C# frontend returns, built from this frontend's IR and SPIR-V, so every method below
    // behaves exactly as it does there. Each takes one stage and needs exactly one entry point for it.
    private CompiledCode ToCompiledCode() =>
        new(new ShaderReflection(IntermediateLanguage), SpirV, SpirVAs, Name);

    /// <summary>The stage as HLSL source (shader model 5).</summary>
    public string ToHlsl(Stage stage) => ToCompiledCode().ToHlsl(stage);

    /// <summary>The stage as desktop GLSL source.</summary>
    public string ToGlsl(Stage stage, uint version = 450) => ToCompiledCode().ToGlsl(stage, version);

    /// <summary>The stage as GLSL ES source.</summary>
    public string ToGlslEs(Stage stage, uint version = 310) => ToCompiledCode().ToGlslEs(stage, version);

    /// <summary>The stage as Metal Shading Language source.</summary>
    public string ToMsl(Stage stage) => ToCompiledCode().ToMsl(stage);

    /// <summary>The stage as C++ source.</summary>
    public string ToCpp(Stage stage) => ToCompiledCode().ToCpp(stage);
}
