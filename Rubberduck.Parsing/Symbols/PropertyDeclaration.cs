﻿using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Rubberduck.Parsing.Annotations;
using Rubberduck.Parsing.Grammar;
using Rubberduck.Parsing.VBA;
using Rubberduck.VBEditor;

namespace Rubberduck.Parsing.Symbols
{
    public abstract class PropertyDeclaration : ModuleBodyElementDeclaration
    {
        protected PropertyDeclaration(
            QualifiedMemberName name,
            Declaration parent,
            Declaration parentScope,
            string asTypeName,
            VBAParser.AsTypeClauseContext asTypeContext,
            string typeHint,
            Accessibility accessibility,
            DeclarationType type,
            ParserRuleContext context,
            ParserRuleContext attributesPassContext,
            Selection selection,
            bool isArray,
            bool isUserDefined,
            IEnumerable<IAnnotation> annotations,
            Attributes attributes)
            : base(
                name,
                parent,
                parentScope,
                asTypeName,
                asTypeContext,
                typeHint,
                accessibility,
                type,
                context,
                attributesPassContext,
                selection,
                isArray,
                isUserDefined,
                annotations,
                attributes)
        { }

        public override bool IsObject =>
            base.IsObject || (Parameters.OrderBy(p => p.Selection).LastOrDefault()?.IsObject ?? false);

        /// <inheritdoc/>
        protected abstract override bool Implements(ICanBeInterfaceMember member);
    }
}
