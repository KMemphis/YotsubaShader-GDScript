using YotsubaShader.Godot;
using YotsubaShader.GDScript;
using YotsubaShader.GDScript.Diagnostics;
using YotsubaShader.GDScript.Project;

const string Usage = """
    yotsuba-gd - compile GDScript shaders with Yotsuba Shader

    Commands:
      compile <file.gd | folder | project.godot> [options]
          Compile shaders and write the results to a folder.
      add <project.godot | folder> [--dry-run]
          Compile every shader of a Godot project and embed the result in each shader's own script, so it can
          be used from GDScript: MyShader.to_godot_shader_language(), MyShader.to_godot_new_shader(),
          MyShader.to_godot_shader_material() and MyShader.to_spirv().

    Options of compile:
      --output, -o   Folder for the generated files (default: ./artifacts).
      --target, -t   spirv (default), godot (Godot shading language, .gdshader), hlsl, glsl, gles
                     (GLSL ES), msl or cpp. Repeat to write several. hlsl, glsl, gles, msl and cpp
                     write one file per stage (<Name>.<Stage>.<ext>), from the SPIR-V through
                     YotsubaShaderCross, the same emission the C# frontend uses.
      --emit-spvasm  Also write the SPIR-V assembly text next to each .spv file.

    Options of add:
      --dry-run      Report what would change without writing any file.

    A folder or project is searched recursively; only scripts with a shader entry point (# @vertex,
    # @fragment or # @compute) are compiled, and Godot's own rules apply (.godot, hidden folders and folders with
    # a .gdignore file or another project.godot are skipped).
    """;

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    Console.WriteLine(Usage);
    return args.Length == 0 ? 1 : 0;
}

return args[0] switch
{
    "compile" => RunCompile(args),
    "add" => RunAdd(args),
    _ => Fail($"Unknown command '{args[0]}'.")
};

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(Usage);
    return 1;
}

static int RunCompile(string[] args)
{
    if (args.Length < 2) return Fail("compile needs a file, a folder or a project.godot.");

    string input = args[1];
    string output = "artifacts";
    bool emitAssembly = false;
    var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    for (int i = 2; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--output" or "-o" when i + 1 < args.Length:
                output = args[++i];
                break;
            case "--target" or "-t" when i + 1 < args.Length:
                string target = args[++i];
                if (!Targets.Known.Contains(target))
                {
                    return Fail($"Target '{target}' is not available; use one of: {string.Join(", ", Targets.Known)}.");
                }

                targets.Add(target.ToLowerInvariant());
                break;
            case "--emit-spvasm":
                emitAssembly = true;
                break;
            default:
                return Fail($"Unknown or incomplete option '{args[i]}'.");
        }
    }

    if (targets.Count == 0) targets.Add("spirv");

    bool explicitFile = File.Exists(input) && Path.GetExtension(input) == ".gd";
    IEnumerable<string> files;
    if (explicitFile)
    {
        files = [input];
    }
    else if (GdProject.Find(input) is { } project && (Directory.Exists(input) || Path.GetFileName(input) == GdProject.ProjectFileName)
             && SameFolder(project.Root, input))
    {
        files = project.EnumerateScripts().Where(IsShader);
    }
    else if (Directory.Exists(input))
    {
        files = GdProject.EnumerateScripts(input, isRoot: true).Where(IsShader);
    }
    else
    {
        files = [];
    }

    if (!files.Any())
    {
        Console.Error.WriteLine($"No shader scripts found at '{input}'.");
        return 1;
    }

    Directory.CreateDirectory(output);
    int failures = 0;
    foreach (string file in files)
    {
        try
        {
            var compiled = GdShaderCompiler.CompileFile(file);
            if (targets.Contains("spirv"))
            {
                string path = Path.Combine(output, compiled.Name + ".spv");
                File.WriteAllBytes(path, compiled.SpirV);
                if (emitAssembly) File.WriteAllText(Path.Combine(output, compiled.Name + ".spvasm"), compiled.SpirVAs);
                Console.WriteLine($"{file} -> {path} ({compiled.SpirV.Length} bytes)");
            }

            foreach (string target in Targets.Stage.Where(targets.Contains))
            {
                foreach (var stage in compiled.Stages)
                {
                    string path = Path.Combine(output, $"{compiled.Name}.{stage}{StageExtension(target)}");
                    try
                    {
                        string text = target switch
                        {
                            "hlsl" => compiled.ToHlsl(stage),
                            "glsl" => compiled.ToGlsl(stage),
                            "gles" => compiled.ToGlslEs(stage),
                            "msl" => compiled.ToMsl(stage),
                            _ => compiled.ToCpp(stage),
                        };
                        File.WriteAllText(path, text);
                        Console.WriteLine($"{file} -> {path}");
                    }
                    catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
                    {
                        failures++;
                        Console.Error.WriteLine($"{file}: error: cannot write {target} for the {stage} stage: {exception.Message}");
                    }
                }
            }

            if (targets.Contains("godot"))
            {
                try
                {
                    string path = Path.Combine(output, compiled.Name + ".gdshader");
                    File.WriteAllText(path, compiled.ToGodotShaderLanguage());
                    Console.WriteLine($"{file} -> {path}");
                }
                catch (GodotBackendException exception)
                {
                    failures++;
                    Console.Error.WriteLine($"{file}: error: cannot write Godot shading language: {exception.Message}");
                }
            }
        }
        catch (GdCompilationException exception)
        {
            failures++;
            foreach (var diagnostic in exception.Diagnostics) Console.Error.WriteLine(diagnostic.Format(file));
        }
    }

    return failures == 0 ? 0 : 1;
}

static string StageExtension(string target) => target switch
{
    "hlsl" => ".hlsl",
    "glsl" => ".glsl",
    "gles" => ".gles",
    "msl" => ".msl",
    _ => ".cpp",
};

static bool SameFolder(string root, string input)
{
    string full = Path.GetFullPath(input);
    if (File.Exists(full)) full = Path.GetDirectoryName(full)!;
    return string.Equals(Path.TrimEndingDirectorySeparator(full), Path.TrimEndingDirectorySeparator(root),
        StringComparison.Ordinal);
}

static bool IsShader(string path) => GdProject.IsShaderScript(File.ReadAllText(path));

static int RunAdd(string[] args)
{
    if (args.Length < 2) return Fail("add needs a project.godot (or the folder that contains it).");

    bool dryRun = false;
    for (int i = 2; i < args.Length; i++)
    {
        if (args[i] == "--dry-run") dryRun = true;
        else return Fail($"Unknown option '{args[i]}'.");
    }

    var project = GdProject.Find(args[1]);
    if (project is null || !(Directory.Exists(args[1]) || Path.GetFileName(args[1]) == GdProject.ProjectFileName))
    {
        Console.Error.WriteLine($"'{args[1]}' is not a Godot project: no {GdProject.ProjectFileName} found.");
        return 1;
    }

    var results = GdProjectAdder.Add(project, dryRun);
    if (results.Count == 0)
    {
        Console.Error.WriteLine($"No shader scripts found in {project.Root}.");
        return 1;
    }

    int failures = 0;
    foreach (var result in results)
    {
        if (result.Status == GdAddStatus.Failed)
        {
            failures++;
            foreach (var diagnostic in result.Diagnostics) Console.Error.WriteLine(diagnostic.Format(result.ResourcePath));
            continue;
        }

        string verb = result.Status switch
        {
            GdAddStatus.Updated => dryRun ? "would update" : "updated",
            _ => "up to date"
        };
        Console.WriteLine($"{result.ResourcePath}: {result.ShaderName} ({verb})");
        if (result.GodotError is not null)
        {
            if (result.GodotErrorIsExpected)
            {
                Console.WriteLine($"  note: no Godot shading language: {result.GodotError}");
            }
            else
            {
                failures++;
                Console.Error.WriteLine($"{result.ResourcePath}: error: cannot write Godot shading language: {result.GodotError}");
            }
        }
    }

    return failures == 0 ? 0 : 1;
}

internal static class Targets
{
    /// <summary>Targets written per stage, from the SPIR-V through YotsubaShaderCross.</summary>
    public static readonly string[] Stage = ["hlsl", "glsl", "gles", "msl", "cpp"];

    public static readonly string[] Known = ["spirv", "godot", .. Stage];
}
