using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Extensions;
using Telerik.JustDecompiler.Ast;
using Telerik.JustDecompiler.Ast.Expressions;
using Telerik.JustDecompiler.Ast.Statements;
using Telerik.JustDecompiler.Languages;
using Telerik.JustDecompiler.Cil;

namespace Telerik.JustDecompiler.Decompiler
{
    class AutoImplementedPropertyMatcher
    {
        private bool processed;
        private readonly PropertyDefinition propertyDef;
        private FieldDefinition propertyFieldDef;

        public AutoImplementedPropertyMatcher(PropertyDefinition property)
        {
            this.propertyDef = property;
        }

        public bool IsAutoImplemented(out FieldDefinition propertyField)
        {
            bool result = IsAutoImplemented();
            propertyField = this.propertyFieldDef;
            return result;
        }

        public bool IsAutoImplemented()
        {
            if (processed)
            {
                return propertyFieldDef != null;
            }
            processed = true;

            if (propertyDef.GetMethod == null || propertyDef.GetMethod.Parameters.Count != 0 || !propertyDef.GetMethod.HasBody ||
                propertyDef.OtherMethods.Count != 0)
            {
                return false;
            }

            if (propertyDef.SetMethod != null)
            {
                if (propertyDef.SetMethod.Parameters.Count != 1 || !propertyDef.SetMethod.HasBody)
                {
                    return false;
                }
            }
            else
            {
                // Getter only auto-implemented property
                return CheckGetter();
            }

            return CheckGetter() && CheckSetter();
        }

        private bool CheckGetter()
        {
            BlockStatement getterStatements = GetStatements(propertyDef.GetMethod.Body);

            if (getterStatements == null || getterStatements.Statements == null ||
                getterStatements.Statements.Count != 1 && getterStatements.Statements.Count != 2 ||
                getterStatements.Statements[0].CodeNodeType != CodeNodeType.ExpressionStatement)
            {
                return false;
            }

            FieldReferenceExpression fieldRefExpression;
            if (getterStatements.Statements.Count == 1)
            {
                ReturnExpression returnExpression = (getterStatements.Statements[0] as ExpressionStatement).Expression as ReturnExpression;
                if (returnExpression == null || returnExpression.Value == null ||
                    returnExpression.Value.CodeNodeType != CodeNodeType.FieldReferenceExpression)
                {
                    return false;
                }
                fieldRefExpression = returnExpression.Value as FieldReferenceExpression;
            }
            else
            {
                BinaryExpression binaryExpression = (getterStatements.Statements[0] as ExpressionStatement).Expression as BinaryExpression;
                if (binaryExpression == null || !binaryExpression.IsAssignmentExpression ||
                    binaryExpression.Left.CodeNodeType != CodeNodeType.VariableReferenceExpression ||
                    binaryExpression.Right.CodeNodeType != CodeNodeType.FieldReferenceExpression)
                {
                    return false;
                }

                if (getterStatements.Statements[1].CodeNodeType != CodeNodeType.ExpressionStatement)
                {
                    return false;
                }

                ReturnExpression returnExpression = (getterStatements.Statements[1] as ExpressionStatement).Expression as ReturnExpression;
                if (returnExpression == null || returnExpression.Value == null ||
                    returnExpression.Value.CodeNodeType != CodeNodeType.VariableReferenceExpression)
                {
                    return false;
                }

                fieldRefExpression = binaryExpression.Right as FieldReferenceExpression;
            }

            return CheckFieldReferenceExpression(fieldRefExpression);
        }

        private bool CheckSetter()
        {
            BlockStatement setterStatements = GetStatements(propertyDef.SetMethod.Body);

            if (setterStatements == null || setterStatements.Statements == null || setterStatements.Statements.Count != 2 ||
                setterStatements.Statements[0].CodeNodeType != CodeNodeType.ExpressionStatement ||
                setterStatements.Statements[1].CodeNodeType != CodeNodeType.ExpressionStatement)
            {
                return false;
            }

            ReturnExpression returnExpression = (setterStatements.Statements[1] as ExpressionStatement).Expression as ReturnExpression;
            if (returnExpression == null || returnExpression.Value != null)
            {
                return false;
            }

            BinaryExpression binaryExpression = (setterStatements.Statements[0] as ExpressionStatement).Expression as BinaryExpression;
            if (binaryExpression == null || !binaryExpression.IsAssignmentExpression ||
                binaryExpression.Left.CodeNodeType != CodeNodeType.FieldReferenceExpression ||
                binaryExpression.Right.CodeNodeType != CodeNodeType.ArgumentReferenceExpression)
            {
                return false;
            }

            return CheckFieldReferenceExpression(binaryExpression.Left as FieldReferenceExpression);
        }

        private bool CheckFieldReferenceExpression(FieldReferenceExpression fieldRefExpression)
        {
            if (fieldRefExpression.Field == null)
            {
                return false;
            }

            if (propertyFieldDef != null)
            {
                return fieldRefExpression.Field.Resolve() == this.propertyFieldDef;
            }

            FieldDefinition fieldDef = fieldRefExpression.Field.Resolve();
            if (fieldDef == null || fieldDef.DeclaringType != propertyDef.DeclaringType)
            {
                return false;
            }

            if (!fieldDef.HasCompilerGeneratedAttribute())
            {
                return false;
            }

            propertyFieldDef = fieldDef;
            return true;
        }

        private BlockStatement GetStatements(MethodBody body)
        {
            //Performance improvement
            ControlFlowGraph cfg = new ControlFlowGraphBuilder(body.Method).CreateGraph();
            if (cfg.Blocks.Length > 2)
            {
                return null;
            }

            DecompilationPipeline pipeline = new DecompilationPipeline(BaseLanguage.IntermediateRepresenationPipeline.Steps,
                new DecompilationContext(new MethodSpecificContext(body) { EnableEventAnalysis = false }, new TypeSpecificContext(body.Method.DeclaringType))) ;
            
            pipeline.Run(body);
            return pipeline.Body;
        }
    }
}
