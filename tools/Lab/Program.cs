using Lab;

var command = args.Length > 0 ? args[0] : "help";
var rest = args.Skip(1).ToArray();

return command switch
{
    "doctor" => Doctor.Run(),
    "lint" => Lint.Run(rest),
    "help" or "--help" or "-h" => Help(),
    _ => Unknown(command),
};

static int Help()
{
    Console.WriteLine("""
        Usage: ./lab <command>          (Windows: lab <command>)

          doctor            Check this machine can run the lab, and which steps it can run.
          lint [--json]     Build the app and count lint findings against docs/lint-baseline.md.
          lint --check      Same, but exit 1 if the count is above the baseline.

        Coming in later steps: list, reset, apply N, goto N.
        """);
    return 0;
}

static int Unknown(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'. Run ./lab help.");
    return 2;
}
