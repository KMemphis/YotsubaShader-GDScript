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

using YotsubaShader.IR.Intermediate.Slot.Types;

namespace YotsubaShader.GDScript.Annotations.Dialects;

/// <summary>GLSL-style annotations (<c>@gl_Position</c>, <c>@gl_VertexIndex</c>, ...).</summary>
internal static class GlslVocabulary
{
    private const string Dialect = "glsl";

    public static IEnumerable<AnnotationDefinition> Definitions =>
    [
        new("gl_Position", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.Position),
        new("gl_FragCoord", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.FragCoord),
        new("gl_FragDepth", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.FragDepth),
        new("gl_FrontFacing", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.FrontFacing),
        new("gl_VertexIndex", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.VertexIndex),
        new("gl_InstanceIndex", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.InstanceIndex),
        new("gl_GlobalInvocationID", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.GlobalInvocationId),
        new("gl_LocalInvocationID", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.LocalInvocationId),
        new("gl_LocalInvocationIndex", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.LocalInvocationIndex),
        new("gl_WorkGroupID", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.WorkgroupId),
        new("gl_NumWorkGroups", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.NumWorkgroups),
        new("location", AnnotationKind.Location, Dialect),
        new("layout_local_size", AnnotationKind.WorkgroupSize, Dialect),
        new("gl_BaryCoordEXT", AnnotationKind.NotSupportedYet, Dialect, Note: "barycentric coordinates"),
        new("gl_ClipDistance", AnnotationKind.NotSupportedYet, Dialect, Note: "clip distances"),
        new("gl_PrimitiveID", AnnotationKind.NotSupportedYet, Dialect, Note: "the primitive index"),
        new("gl_SampleID", AnnotationKind.NotSupportedYet, Dialect, Note: "the sample index"),
        new("gl_SubgroupSize", AnnotationKind.NotSupportedYet, Dialect, Note: "subgroup built-ins"),
        new("gl_SubgroupInvocationID", AnnotationKind.NotSupportedYet, Dialect, Note: "subgroup built-ins")
    ];
}
