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

using YotsubaShader.IR.Intermediate;
using YotsubaShader.IR.Intermediate.FunctionBody;
using YotsubaShader.IR.Intermediate.FunctionBody.Expressions;
using YotsubaShader.IR.Intermediate.FunctionBody.Expressions.AssignmentTargets;
using YotsubaShader.IR.Intermediate.FunctionBody.Expressions.Literals;
using YotsubaShader.IR.Intermediate.FunctionBody.Statements;
using YotsubaShader.IR.Intermediate.Slot;
using YotsubaShader.IR.Intermediate.Slot.Types;
using YotsubaShader.IR.Intermediate.Types;
using YotsubaShader.IR.Intermediate.ValueConvertions;
using static YotsubaShader.GDScript.Tests.TestSupport;

namespace YotsubaShader.GDScript.Tests;

public class LoweringTests
{
    private static Shader BasicColor() =>
        GdShaderCompiler.ToIntermediateLanguage(File.ReadAllText(SamplePath("basic_color.gd"))).Shader;

    [Fact]
    public void BasicColor_ProducesThreeEntryPointsInDeclarationOrder()
    {
        var shader = BasicColor();

        Assert.Equal(["vs", "fs", "compute"], shader.Functions.Select(f => f.HumanName));
        Assert.Equal([Stage.Vertex, Stage.Fragment, Stage.Compute], shader.Functions.Select(f => f.Stage));
        Assert.Equal([0u, 1u, 2u], shader.Functions.Select(f => f.Id.Value));
        Assert.Equal(new ComputeParams(3, 3, 3), shader.Functions[2].ExtraParams);
    }

    [Fact]
    public void BasicColor_VertexInterfaceUsesVertexSemanticsAndBuiltIns()
    {
        var vertex = BasicColor().Functions[0];

        var input = Assert.IsType<CustomStruct>(Assert.Single(vertex.Inputs));
        Assert.Equal(StorageKind.Input, input.Kind);
        Assert.Equal(new VertexSlot(new VertexSemantic(VertexUsage.Position)), input.Properties[0].Slot);
        Assert.Equal(new VertexSlot(new VertexSemantic(VertexUsage.Color)), input.Properties[1].Slot);

        var output = Assert.IsType<CustomStruct>(Assert.Single(vertex.Outputs));
        Assert.Equal(StorageKind.Output, output.Kind);
        Assert.Equal(new BuiltInSlot(BuiltIn.Position), output.Properties[0].Slot);
        Assert.IsType<Vector>(output.Properties[0]);
    }

    [Fact]
    public void BasicColor_FragmentReturnUsesRenderTargetLocation()
    {
        var fragment = BasicColor().Functions[1];

        var output = Assert.IsType<Vector>(Assert.Single(fragment.Outputs));
        Assert.Equal(new LocationSlot(0), output.Slot);
    }

    [Fact]
    public void VectorConstructor_LowersToVectorExpressionWithComponentReads()
    {
        var vertex = BasicColor().Functions[0];
        var assignments = vertex.Body.OfType<Assignment>().ToList();
        var vector = Assert.IsType<VectorExpression>(assignments[0].Value);

        Assert.Equal(VectorComponentsType.Float, vector.ComponentsType);
        Assert.Equal(4, vector.Values.Count);
        var x = Assert.IsType<StorageStructPropertyReference>(vector.Values[0]);
        Assert.Equal("x", x.PropertyName);
        Assert.IsType<FloatLiteral>(vector.Values[3]);
    }

    [Fact]
    public void ForRange_LowersToLoopWithContinuingStep()
    {
        var shader = Lower(VertexWith("var total := 0\nfor i in range(4):\n    total += i"));

        var block = shader.Functions[0].Body.OfType<Block>().First(b => b.Statements.OfType<Loop>().Any());
        var loop = block.Statements.OfType<Loop>().Single();
        var condition = Assert.IsType<BinaryExpression>(loop.Condition);
        Assert.Equal(BinaryOperator.Less, condition.Operator);
        Assert.Single(loop.Continuing.Statements);
    }

    [Fact]
    public void NegativeRangeStep_UsesGreaterComparison()
    {
        var shader = Lower(VertexWith("for i in range(4, 0, -1):\n    pass"));

        var loop = shader.Functions[0].Body.OfType<Block>().SelectMany(b => b.Statements).OfType<Loop>().Single();
        Assert.Equal(BinaryOperator.Greater, Assert.IsType<BinaryExpression>(loop.Condition).Operator);
    }

    [Fact]
    public void While_LowersToTopConditionedLoop()
    {
        var shader = Lower(VertexWith("var n := 0\nwhile n < 3:\n    n += 1"));

        var loop = shader.Functions[0].Body.OfType<Loop>().Single();
        Assert.NotNull(loop.Condition);
        Assert.Empty(loop.Continuing.Statements);
    }

    [Fact]
    public void Ternary_LowersToSequenceWithTemporary()
    {
        var shader = Lower(VertexWith("var a := 1.0\nvar b := 2.0 if a > 0.5 else 3.0"));

        var declaration = shader.Functions[0].Body.OfType<VariableDeclaration>().First(d => d.Storage.HumanName == "b");
        var sequence = Assert.IsType<SequenceExpression>(declaration.Initializer);
        Assert.IsType<If>(sequence.Prelude[1]);
    }

    [Fact]
    public void IntToFloat_InsertsNumericConversion_AndFoldsLiterals()
    {
        var shader = Lower(VertexWith("var i := 2\nvar f: float = i\nvar g: float = 3"));
        var declarations = shader.Functions[0].Body.OfType<VariableDeclaration>().ToList();

        var conversion = Assert.IsType<NumericConversion>(declarations[1].Initializer);
        Assert.Equal(ScalarType.Float32, conversion.TargetType);
        Assert.IsType<FloatLiteral>(declarations[2].Initializer);
    }

    [Fact]
    public void UnsignedLiteral_AdoptsUintInComparisons()
    {
        var source = """
            class Out:
                var position: Vector4 # @builtin(position)

            # @vertex
            func vs(
                    i: uint # @builtin(vertex_index)
            ) -> Out:
                var o := Out.new()
                if i == 1:
                    o.position = Vector4(1.0, 0.0, 0.0, 1.0)
                return o
            """;
        var shader = Lower(source);

        var branch = shader.Functions[0].Body.OfType<If>().Single();
        var condition = Assert.IsType<BinaryExpression>(branch.Condition);
        Assert.Equal(new IntLiteral("1", false), condition.Right);
    }

    [Fact]
    public void GlobalConstants_AreInlined()
    {
        var shader = Lower(VertexWith("var x := LIMIT", "const LIMIT = 8"));

        var declaration = shader.Functions[0].Body.OfType<VariableDeclaration>().First();
        Assert.Equal(new IntLiteral("8", true), declaration.Initializer);
    }

    [Fact]
    public void HelperFunctions_GetIdsAfterEntryPoints_AndOnlyWhenCalled()
    {
        var shader = Lower("""
            func used(a: float) -> float:
                return a * 2.0

            func unused(a: float) -> float:
                return a

            class Out:
                var position: Vector4 # @builtin(position)

            # @vertex
            func vs() -> Out:
                var o := Out.new()
                o.position = Vector4(used(1.0), 0.0, 0.0, 1.0)
                return o
            """);

        Assert.Equal(["vs", "used"], shader.Functions.Select(f => f.HumanName));
        Assert.Equal(Stage.None, shader.Functions[1].Stage);
        Assert.Equal(1u, shader.Functions[1].Id.Value);
    }

    [Fact]
    public void UniformStruct_BecomesGlobalResourceWithDescriptorSlot()
    {
        var shader = GdShaderCompiler.ToIntermediateLanguage(File.ReadAllText(SamplePath("resources.gd"))).Shader;

        var resource = Assert.IsType<CustomStruct>(Assert.Single(shader.GlobalResources));
        Assert.Equal(StorageKind.Uniform, resource.Kind);
        Assert.Equal(new DescriptorSlot(0, 0), resource.Slot);
        Assert.Equal("MaterialData", resource.TypeName);
    }

    [Fact]
    public void ComputeBuiltIn_ResolvesInvocationId()
    {
        var shader = GdShaderCompiler.ToIntermediateLanguage(File.ReadAllText(SamplePath("wgsl_style.gd"))).Shader;

        var compute = shader.Functions.Single(f => f.Stage == Stage.Compute);
        var id = Assert.IsType<Vector>(Assert.Single(compute.Inputs));
        Assert.Equal(new BuiltInSlot(BuiltIn.GlobalInvocationId), id.Slot);
        Assert.Equal(VectorComponentsType.Int, id.ComponentsType);
        Assert.Equal(new ComputeParams(8, 8, 1), compute.ExtraParams);
    }

    [Fact]
    public void PositionInAFragmentStage_BecomesFragCoord()
    {
        var shader = Lower("""
            class In:
                var position: Vector4 # @SV_Position

            # @fragment
            # @location(0)
            func fs(input: In) -> Vector4:
                return input.position
            """);

        var input = Assert.IsType<CustomStruct>(shader.Functions[0].Inputs[0]);
        Assert.Equal(new BuiltInSlot(BuiltIn.FragCoord), input.Properties[0].Slot);
    }
}
