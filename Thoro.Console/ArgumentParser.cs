namespace Thoro.Console
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ArgumentParser
    {
        public string ApplicationName { get; set; }

        public List<Argument> Arguments { get; set; }

        public List<Option> Options { get; set; }

        public ArgumentParser(string applicationName)
        {
            this.ApplicationName = applicationName;
            this.Arguments = new List<Argument>();
            this.Options = new List<Option>();
        }

        public string Help()
        {
            string usage = string.Format("Usage: {0} {1} {2}", this.ApplicationName, string.Join(" ", this.Options), string.Join(" ", this.Arguments));

            string options = string.Join("\n", this.Options.Select(n => n.Help()));

            return usage + "\n\nOptions:\n" + options;
        }

        public Option AddOption(string name, string help = null, Argument[] arguments = null)
        {
            var opt = new Option(name);
            opt.HelpText = help;
            this.Options.Add(opt);

            if (arguments != null)
            {
                opt.Arguments.AddRange(arguments);
            }

            return opt;
        }

        public void AddArgument(Argument arg)
        {
            this.Arguments.Add(arg);
        }

        public bool Parse(string[] args)
        {
            List<Option> availableOptions = new List<Option>(this.Options);
            Queue<Argument> availableArguments = new Queue<Argument>(this.Arguments);

            Option option = null;
            Argument expected = null;

            foreach (string arg in args)
            {
                // Option
                if (arg.StartsWith("-"))
                {
                    option = this.Options.Where(n => n.Name == arg.Substring(1)).FirstOrDefault();

                    if (option == null)
                    {
                        Console.WriteLine("Option " + arg + " not available!");
                        return false;
                    }

                    option.Used = true;
                    availableOptions.Remove(option);
                    expected = option.Arguments.FirstOrDefault();
                    continue;
                }

                // Option Argument!
                if (expected != null)
                {
                    if (!expected.Parse(arg))
                    {
                        Console.WriteLine("Invalid Parameter Type for " + option.Name + " at position " + option.Arguments.IndexOf(expected));
                        return false;
                    }

                    expected = option.Arguments.SkipWhile(n => n != expected).FirstOrDefault();
                    if (expected == null)
                    {
                        option = null;
                    }

                    continue;
                }

                if (availableArguments.Count == 0)
                {
                    Console.WriteLine("Invalid Argument " + arg);
                    return false;
                }

                Argument cur = availableArguments.Dequeue();

                if (!cur.Parse(arg))
                {
                    Console.WriteLine("Invalid Parameter Type for Argument " + cur.Name);
                    return false;
                }
            }

            if (availableArguments.Count > 0)
            {
                Console.WriteLine("The arguments " + string.Join(", ", availableArguments.Select(n => n.Name)) + " missing!");
                return false;
            }

            return true;
        }
    }
}
