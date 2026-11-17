using Lab;

var command = args.Length > 0 ? args[0] : "help";
var rest = args.Skip(1).ToArray();

try
{
    return command switch
    {
        "doctor" => Doctor.Run(),
        "warm" => Warm.Run(),
        "lint" => Lint.Run(rest),
        "list" or "status" => StepCommands.List(),
        "apply" => StepCommands.Apply(rest),
        "reset" => StepCommands.Reset(rest),
        "goto" => StepCommands.Goto(rest),
        "tdd" => Tdd.Run(rest),
        "hook" => Hook.Run(rest),
        "ratchet" => Ratchet(rest),
        "help" or "--help" or "-h" => Help(),
        _ => Unknown(command),
    };
}
catch (LabException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}

static int Help()
{
    Console.WriteLine("""
        Usage: ./lab <command>          (Windows: lab <command>)

          doctor                 Check this machine can run the lab, and which steps it can run.
          warm                   One-off setup after doctor: first build, and the skill scripts' packages.
          lint [--json]          Build the app and count lint findings against docs/lint-baseline.md.
          lint --check           Same, but exit 1 if the count is above the baseline.
          ratchet [N]            Lower the lint baseline (by one, or to N) and commit it.

          list                   Show the steps and which are installed.
          apply N [--tools T]    Install step N's example as its own commit. T: claude,codex adapters.
          reset [--dry-run]      Throw away uncommitted changes in the lab's folders (a Before or After run).
          goto N [--solutions]   Catch up: install every step before N (and their reference solutions).

        The loop for each step: Before run, ./lab reset, ./lab apply N, read the files, After run in a new session.
        """);
    return 0;
}

static int Ratchet(string[] args)
{
    // The team lowers the lint baseline (Step 7's setup). Its own commit, so ./lab reset doesn't undo it.
    var file = Path.Combine(Repo.Root, "docs", "lint-baseline.md");
    var text = File.ReadAllText(file);
    var match = System.Text.RegularExpressions.Regex.Match(text, @"^Total:\s*(\d+)", System.Text.RegularExpressions.RegexOptions.Multiline);
    if (!match.Success)
    {
        throw new LabException("docs/lint-baseline.md has no 'Total: N' line. Install step 0 first (./lab apply 0).");
    }

    var current = int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    var target = args.Length > 0 ? int.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture) : current - 1;
    if (target >= current)
    {
        Console.WriteLine($"The baseline is {current}; a ratchet only goes down.");
        return 1;
    }

    File.WriteAllText(file, text.Remove(match.Index, match.Length).Insert(match.Index, $"Total: {target}"));
    Git.Commit($"lab: lint baseline lowered to {target}", ["docs/lint-baseline.md"]);
    Console.WriteLine($"Lint baseline lowered from {current} to {target}, committed. ./lab lint now compares against {target}.");
    return 0;
}

static int Unknown(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'. Run ./lab help.");
    return 2;
}
