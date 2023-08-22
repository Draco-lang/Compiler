using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Scriban.Runtime;
using Scriban;

namespace Draco.Trace;

internal static class ScribanRenderer
{
    public static string Render(string templateName, object model, CancellationToken cancellationToken)
    {
        var template = ScribanTemplateLoader.Load(templateName);

        var context = new TemplateContext
        {
            TemplateLoader = ScribanTemplateLoader.Instance,
            MemberRenamer = MemberRenamer,
        };
        var scriptObject = new ScriptObject();
        // scriptObject.Import(ScribanHelperFunctions.Instance);
        scriptObject.Import(model, renamer: MemberRenamer);
        context.PushGlobal(scriptObject);
        context.CancellationToken = cancellationToken;

        return template.Render(context);
    }

    private static string MemberRenamer(MemberInfo memberInfo) => memberInfo.Name;
}
