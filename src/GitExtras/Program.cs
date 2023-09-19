
namespace GitExtras;

using Spectre.Console;
using Spectre.Console.Cli;
using System;

static class Program
{
    static int Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(x =>
        {
            x.PropagateExceptions();
            x.AddCommand<FixEol>("fixeol");
        });
        return app.Run(args);
    }
}
