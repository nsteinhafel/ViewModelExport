using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ViewModelExport.Services
{
    /// <summary>
    /// Visitor that extracts types from properties that are required to recreate this a given model class when translated into
    /// another language, like TypeScript.
    /// </summary>
    public class ClassNameVisitor : CSharpSyntaxWalker
    {
        /// <summary>Set of models for which to collect required types.</summary>
        private readonly ISet<string> _modelSet;

        /// <summary>Instantiates a new <see cref="ClassNameVisitor" /> the the set of desired model classes.</summary>
        /// <param name="modelSet"></param>
        public ClassNameVisitor(ISet<string> modelSet)
        {
            _modelSet = modelSet ?? throw new ArgumentNullException(nameof(modelSet));
        }

        /// <summary>Set of required types for this class.</summary>
        public ISet<string> RequiredTypes { get; } = new HashSet<string>();

        /// <summary>Visit a property and process it to extract the required types.</summary>
        /// <param name="node">Property declaration syntax.</param>
        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (!(node.Parent is ClassDeclarationSyntax classNode)) return;

            // Only concern ourselves with our desired models.
            if (!_modelSet.Contains(classNode.Identifier.ValueText)) return;

            // Add container base types.
            switch (node.Type)
            {
                case ArrayTypeSyntax array:
                    RequiredTypes.Add(array.ElementType.ToString());
                    return;
                case GenericNameSyntax generic:
                    AddGenerics(generic);
                    return;
            }

            // If we're not a container, add our type.
            RequiredTypes.Add(node.Type.ToString());
        }

        /// <summary>Recursively add generic types to our required types.</summary>
        /// <param name="generic">Generic type syntax.</param>
        private void AddGenerics(GenericNameSyntax generic)
        {
            foreach (var typeArgument in generic.TypeArgumentList.Arguments)
            {
                // If our sub-type is generic, keep going down the chain.
                if (typeArgument is GenericNameSyntax)
                {
                    AddGenerics(generic);
                    continue;
                }

                // Add our type.
                RequiredTypes.Add(typeArgument.ToString());
            }
        }
    }
}