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

/// <summary>Vulkan/SPIR-V-style annotations (<c>@BuiltIn_Position</c>, <c>@Location</c>, ...).</summary>
internal static class SpirvVocabulary
{
    private const string Dialect = "spirv";

    public static IEnumerable<AnnotationDefinition> Definitions =>
    [
        new("BuiltIn_Position", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.Position),
        new("BuiltIn_FragCoord", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.FragCoord),
        new("BuiltIn_FragDepth", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.FragDepth),
        new("BuiltIn_FrontFacing", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.FrontFacing),
        new("BuiltIn_VertexIndex", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.VertexIndex),
        new("BuiltIn_InstanceIndex", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.InstanceIndex),
        new("BuiltIn_GlobalInvocationId", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.GlobalInvocationId),
        new("BuiltIn_LocalInvocationId", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.LocalInvocationId),
        new("BuiltIn_LocalInvocationIndex", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.LocalInvocationIndex),
        new("BuiltIn_WorkgroupId", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.WorkgroupId),
        new("BuiltIn_NumWorkgroups", AnnotationKind.BuiltIn, Dialect, BuiltIn: BuiltIn.NumWorkgroups),
        new("Location", AnnotationKind.Location, Dialect),
        new("BuiltIn_BaryCoord", AnnotationKind.NotSupportedYet, Dialect, Note: "barycentric coordinates"),
        new("BuiltIn_ClipDistance", AnnotationKind.NotSupportedYet, Dialect, Note: "clip distances"),
        new("BuiltIn_PrimitiveId", AnnotationKind.NotSupportedYet, Dialect, Note: "the primitive index"),
        new("BuiltIn_SampleId", AnnotationKind.NotSupportedYet, Dialect, Note: "the sample index"),
        new("BuiltIn_SubgroupSize", AnnotationKind.NotSupportedYet, Dialect, Note: "subgroup built-ins"),
        new("BuiltIn_WorkgroupSize", AnnotationKind.NotSupportedYet, Dialect, Note: "the workgroup size built-in")
    ];
}
