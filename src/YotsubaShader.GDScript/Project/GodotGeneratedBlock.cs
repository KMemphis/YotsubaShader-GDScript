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

using System.Text;
using System.Text.RegularExpressions;

namespace YotsubaShader.GDScript.Project;

/// <summary>
/// The code that <c>yotsuba-gd add</c> writes at the end of a shader script, between two marker comments:
/// static functions (<c>to_godot_shader_language()</c>, <c>to_godot_new_shader()</c>,
/// <c>to_godot_shader_material()</c> and <c>to_spirv()</c>) with the compiled shader embedded. GDScript has no
/// extension methods, so the functions live in the shader's own script and are called on its
/// <c>class_name</c>. The block is valid GDScript and is ignored when the script is compiled.
/// </summary>
public static class GodotGeneratedBlock
{
    public const string BeginMarker = "# <yotsuba:generated>";
    public const string EndMarker = "# </yotsuba:generated>";

    /// <summary>Replaces the generated block with blank lines, so line numbers of the source stay the same.</summary>
    public static string Strip(string source)
    {
        if (!source.Contains(BeginMarker, StringComparison.Ordinal)) return source;

        string newline = source.Contains("\r\n") ? "\r\n" : "\n";
        var lines = source.Split(newline);
        bool inside = false;
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].Trim();
            if (!inside && trimmed == BeginMarker) inside = true;
            if (inside)
            {
                bool last = trimmed == EndMarker;
                lines[i] = "";
                if (last) inside = false;
            }
        }

        return string.Join(newline, lines);
    }

    /// <summary>The indentation unit the script already uses (a tab, or N spaces); GDScript requires consistency.</summary>
    public static string DetectIndent(string source)
    {
        foreach (string line in source.Split('\n'))
        {
            if (line.Length == 0 || (line[0] != ' ' && line[0] != '\t')) continue;
            if (line.Trim().Length == 0) continue;
            if (line[0] == '\t') return "\t";
            int width = line.Length - line.TrimStart(' ').Length;
            return new string(' ', width);
        }

        return "\t";
    }

    /// <summary>Returns the source with the generated block added or replaced.</summary>
    public static string Apply(string source, string block)
    {
        string newline = source.Contains("\r\n") ? "\r\n" : "\n";
        block = block.Replace("\n", newline);

        int begin = source.IndexOf(BeginMarker, StringComparison.Ordinal);
        int end = source.IndexOf(EndMarker, StringComparison.Ordinal);
        if (begin >= 0 && end > begin)
        {
            int lineStart = source.LastIndexOf('\n', begin) + 1;
            int after = end + EndMarker.Length;
            string tail = source[after..];
            return source[..lineStart] + block.TrimEnd('\r', '\n') + tail;
        }

        string body = source.TrimEnd('\r', '\n', ' ', '\t');
        return body + newline + newline + block.TrimEnd('\r', '\n') + newline;
    }

    /// <summary>
    /// Renders the block for a shader. <paramref name="godotCode"/> is null when the shader cannot be expressed
    /// in Godot shading language; <paramref name="godotError"/> then explains why, and the Godot functions report it.
    /// </summary>
    public static string Render(
        string shaderName,
        string? godotCode,
        string? godotError,
        byte[] spirV,
        string indent)
    {
        var sb = new StringBuilder();
        sb.Append(BeginMarker).Append('\n');
        sb.Append("# Generated by `yotsuba-gd add`. Everything between these markers is replaced the next time it runs.\n");
        sb.Append("# Call these on the class name of this script, for example ").Append(shaderName).Append(".to_godot_shader_material().\n\n");

        if (godotCode is not null)
        {
            sb.Append("## The shader as Godot shading language source.\n");
            sb.Append("static func to_godot_shader_language() -> String:\n");
            sb.Append(indent).Append("return _YOTSUBA_GODOT_SHADER_CODE\n\n");

            sb.Append("## A new Shader resource with the code above.\n");
            sb.Append("static func to_godot_new_shader() -> Shader:\n");
            sb.Append(indent).Append("var shader := Shader.new()\n");
            sb.Append(indent).Append("shader.code = _YOTSUBA_GODOT_SHADER_CODE\n");
            sb.Append(indent).Append("return shader\n\n");

            sb.Append("## A new ShaderMaterial that uses a new Shader resource.\n");
            sb.Append("static func to_godot_shader_material() -> ShaderMaterial:\n");
            sb.Append(indent).Append("var material := ShaderMaterial.new()\n");
            sb.Append(indent).Append("material.shader = to_godot_new_shader()\n");
            sb.Append(indent).Append("return material\n\n");
        }
        else
        {
            string message = Quote($"{shaderName}: {godotError}");
            sb.Append("## This shader cannot be expressed in Godot shading language (see the message below).\n");
            sb.Append("static func to_godot_shader_language() -> String:\n");
            sb.Append(indent).Append("push_error(").Append(message).Append(")\n");
            sb.Append(indent).Append("return \"\"\n\n");

            sb.Append("static func to_godot_new_shader() -> Shader:\n");
            sb.Append(indent).Append("push_error(").Append(message).Append(")\n");
            sb.Append(indent).Append("return null\n\n");

            sb.Append("static func to_godot_shader_material() -> ShaderMaterial:\n");
            sb.Append(indent).Append("push_error(").Append(message).Append(")\n");
            sb.Append(indent).Append("return null\n\n");
        }

        sb.Append("## The compiled SPIR-V binary.\n");
        sb.Append("static func to_spirv() -> PackedByteArray:\n");
        sb.Append(indent).Append("return Marshalls.base64_to_raw(_YOTSUBA_SPIRV_BASE64)\n\n");

        if (godotCode is not null)
        {
            // The code never contains a backslash or a double quote, but escape anyway.
            string safe = godotCode.Replace("\\", "\\\\").Replace("\"\"\"", "\\\"\\\"\\\"");
            sb.Append("const _YOTSUBA_GODOT_SHADER_CODE := \"\"\"").Append(safe).Append("\"\"\"\n\n");
        }

        string base64 = Convert.ToBase64String(spirV);
        sb.Append("const _YOTSUBA_SPIRV_BASE64 := (\n");
        const int chunk = 100;
        for (int i = 0; i < base64.Length; i += chunk)
        {
            string piece = base64.Substring(i, Math.Min(chunk, base64.Length - i));
            sb.Append(indent).Append('"').Append(piece).Append('"');
            sb.Append(i + chunk < base64.Length ? " +\n" : "\n");
        }

        sb.Append(")\n");
        sb.Append(EndMarker);
        return sb.ToString();
    }

    private static string Quote(string text)
    {
        string oneLine = Regex.Replace(text, @"\s+", " ").Trim();
        return "\"" + oneLine.Replace("\\", "\\\\").Replace("\"", "'") + "\"";
    }
}
