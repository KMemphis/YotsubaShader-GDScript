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

using YotsubaShader.GDScript;
using YotsubaShader.GDScript.Diagnostics;

const string Usage = """
    yotsuba-gd - compile GDScript shaders to SPIR-V with Yotsuba Shader

    Usage:
      yotsuba-gd compile <file.gd | directory> [--output <directory>] [--target spirv] [--emit-spvasm]

    Options:
      --output, -o   Directory for the generated files (default: ./artifacts).
      --target, -t   Output target. Only 'spirv' is available in this frontend.
      --emit-spvasm  Also write the SPIR-V assembly text next to each .spv file.
    """;

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    Console.WriteLine(Usage);
    return args.Length == 0 ? 1 : 0;
}

if (args[0] != "compile" || args.Length < 2)
{
    Console.Error.WriteLine(Usage);
    return 1;
}

string input = args[1];
string output = "artifacts";
bool emitAssembly = false;
for (int i = 2; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--output" or "-o" when i + 1 < args.Length:
            output = args[++i];
            break;
        case "--target" or "-t" when i + 1 < args.Length:
            string target = args[++i];
            if (!target.Equals("spirv", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"Target '{target}' is not available in the GDScript frontend yet; use 'spirv'.");
                return 1;
            }

            break;
        case "--emit-spvasm":
            emitAssembly = true;
            break;
        default:
            Console.Error.WriteLine($"Unknown or incomplete option '{args[i]}'.{Environment.NewLine}{Usage}");
            return 1;
    }
}

IEnumerable<string> files = Directory.Exists(input)
    ? Directory.EnumerateFiles(input, "*.gd", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal)
    : File.Exists(input)
        ? [input]
        : [];

if (!files.Any())
{
    Console.Error.WriteLine($"No .gd files found at '{input}'.");
    return 1;
}

Directory.CreateDirectory(output);
int failures = 0;
foreach (string file in files)
{
    try
    {
        var compiled = GdShaderCompiler.CompileFile(file);
        string path = Path.Combine(output, compiled.Name + ".spv");
        File.WriteAllBytes(path, compiled.SpirV);
        if (emitAssembly) File.WriteAllText(Path.Combine(output, compiled.Name + ".spvasm"), compiled.SpirVAs);
        Console.WriteLine($"{file} -> {path} ({compiled.SpirV.Length} bytes)");
    }
    catch (GdCompilationException exception)
    {
        failures++;
        foreach (var diagnostic in exception.Diagnostics) Console.Error.WriteLine(diagnostic.Format(file));
    }
}

return failures == 0 ? 0 : 1;
