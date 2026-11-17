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

          list                   Show the steps and which are installed.
          apply N [--tools T]    Install step N's example as its own commit. T: claude,codex adapters.
          reset [--dry-run]      Throw away uncommitted changes in the lab's folders (a Before or After run).
          goto N [--solutions]   Catch up: install every step before N (and their reference solutions).

        The loop for each step: Before run, ./lab reset, ./lab apply N, read the files, After run in a new session.
        """);
    return 0;
}

static int Unknown(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'. Run ./lab help.");
    return 2;
}
