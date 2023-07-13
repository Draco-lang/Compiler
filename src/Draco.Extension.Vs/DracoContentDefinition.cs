using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace Draco.Extension.Vs
{
    public sealed class BarContentDefinition
    {
        [Export]
        [Name("draco")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition BarContentTypeDefinition;

        [Export]
        [FileExtension(".draco")]
        [ContentType("draco")]
        internal static FileExtensionToContentTypeDefinition BarFileExtensionDefinition;
    }
}
