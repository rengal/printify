namespace Printify.Application.Services;

using Mediator.Net.Contracts;

public interface IGreetingService
{
    GreetingResponse GetGreeting(GreetingContext? context);
}

public sealed record GreetingContext(
    DateTimeOffset WorkspaceCreatedAt,
    int TotalPrinters,
    long TotalDocuments,
    long DocumentsLast24h,
    DateTimeOffset? LastDocumentAt);

public sealed class GreetingResponse(string? morning, string? afternoon, string? evening, string general) : IResponse
{
    public string? Morning { get; } = morning;
    public string? Afternoon { get; } = afternoon;
    public string? Evening { get; } = evening;
    public string General { get; } = general;
}

public sealed class GreetingService : IGreetingService
{
    private static readonly Random random = new();

    // General greetings (always safe, neutral)
    private static readonly string[] GeneralGreetings =
    [
        "Welcome back!",
        "Great to see you!",
        "Nice to see you!",
        "Hello again!",
        "Welcome!",
        "Hi!",
        "Hey!",
        "Greetings!",
        "Good to have you back!",
        "Ready to print?",
        "Let's get to work!",
        "Back again?",
        "Welcome to Printify!",
        "Your virtual printing solution.",
        "Ready to manage your printers.",
        "Let's get printing.",
        "Your print workspace awaits.",
        "Your printers are standing by.",
        "Ready to print something amazing?",
        "Let's handle those print jobs.",
        "Your virtual printing journey starts here.",
        "Managing printers made simple.",
        "Your print commands, our expertise."
    ];

    // Daytime greetings
    private static readonly string[] MorningGreetings =
    [
        "Good morning!",
        "Rise and shine!",
        "Morning!",
        "Top of the morning!"
    ];

    private static readonly string[] AfternoonGreetings =
    [
        "Good afternoon!",
        "Afternoon!",
        "Hello!",
        "Hey there!"
    ];

    private static readonly string[] EveningGreetings =
    [
        "Good evening!",
        "Evening!",
        "Hello!",
        "Hey!"
    ];

    public GreetingResponse GetGreeting(GreetingContext? context)
    {
        var strategy = SelectStrategy();
        var general = GetGeneralGreeting();

        return strategy switch
        {
            GreetingStrategy.GeneralOnly => new GreetingResponse(null, null, null, general),
            GreetingStrategy.DaytimeBased => GetDaytimeBasedGreeting(general),
            GreetingStrategy.ContextBased => new GreetingResponse(null, null, null, GetContextualGreeting(context)),
            _ => new GreetingResponse(null, null, null, general)
        };
    }

    private static GreetingStrategy SelectStrategy()
    {
        // Strategy distribution:
        // General-only: 80%
        // Daytime-based: 10%
        // Context-based: 10%
        var roll = random.Next(100);

        if (roll < 80) return GreetingStrategy.GeneralOnly;
        if (roll < 90) return GreetingStrategy.DaytimeBased;
        return GreetingStrategy.ContextBased;
    }

    private static string GetGeneralGreeting()
    {
        return GeneralGreetings[random.Next(GeneralGreetings.Length)];
    }

    private static GreetingResponse GetDaytimeBasedGreeting(string general)
    {
        var morning = MorningGreetings[random.Next(MorningGreetings.Length)];
        var afternoon = AfternoonGreetings[random.Next(AfternoonGreetings.Length)];
        var evening = EveningGreetings[random.Next(EveningGreetings.Length)];

        return new GreetingResponse(morning, afternoon, evening, general);
    }

    private static string GetContextualGreeting(GreetingContext? context)
    {
        // If no context available, return safe general greeting
        if (context is null)
        {
            return GeneralGreetings[random.Next(GeneralGreetings.Length)];
        }

        var now = DateTimeOffset.UtcNow;
        var workspaceAge = now - context.WorkspaceCreatedAt;
        TimeSpan? timeSinceLastDocument = context.LastDocumentAt.HasValue
            ? now - context.LastDocumentAt.Value
            : null;

        // Priority 1: New workspace (less than 5 minutes old)
        if (workspaceAge.TotalMinutes < 5)
        {
            return GetNewWorkspaceGreeting(context.TotalPrinters);
        }

        // Priority 2: First prints (workspace exists but no documents yet)
        if (context.TotalDocuments == 0)
        {
            return GetFirstPrintsGreeting(context.TotalPrinters);
        }

        // Priority 3: Recently active (document in last hour)
        if (timeSinceLastDocument.HasValue && timeSinceLastDocument.Value.TotalHours < 1)
        {
            return GetRecentlyActiveGreeting(context.DocumentsLast24h);
        }

        // Priority 4: Welcome back (last activity 1-24 hours ago)
        if (timeSinceLastDocument.HasValue && timeSinceLastDocument.Value.TotalHours < 24)
        {
            return GetWelcomeBackGreeting(timeSinceLastDocument.Value);
        }

        // Priority 5: Long time no see (last activity 1-30 days ago)
        if (timeSinceLastDocument.HasValue && timeSinceLastDocument.Value.TotalDays < 30)
        {
            return GetLongTimeNoSeeGreeting(timeSinceLastDocument.Value);
        }

        // Priority 6: Very long time (30+ days)
        if (timeSinceLastDocument.HasValue && timeSinceLastDocument.Value.TotalDays >= 30)
        {
            return GetVeryLongTimeGreeting();
        }

        // Priority 7: Activity-based greetings (based on volume)
        return GetActivityBasedGreeting(context.DocumentsLast24h, context.TotalDocuments);
    }

    private static string GetNewWorkspaceGreeting(int totalPrinters)
    {
        var greetings = totalPrinters == 0
            ? new[]
            {
                "Welcome to your new workspace!",
                "Let's add your first printer.",
                "Your workspace is ready for printers.",
                "Time to set up your printing environment.",
                "Get started by adding a printer."
            }
            : new[]
            {
                "Workspace is ready!",
                "Your printers are configured.",
                "All set up and ready to print.",
                "Printers added and ready to go.",
                "Setup complete. Ready when you are."
            };

        return greetings[random.Next(greetings.Length)];
    }

    private static string GetFirstPrintsGreeting(int totalPrinters)
    {
        var greetings = totalPrinters == 0
            ? new[]
            {
                "Add a printer to get started.",
                "Ready for your first printer.",
                "Let's set up your first printer.",
                "Your workspace awaits its first printer.",
                "Time to configure a printer."
            }
            : new[]
            {
                "Ready for your first print job.",
                "Printers configured. Send your first document!",
                "First print awaits!",
                "Ready to start printing.",
                "Your first document is ready when you are."
            };

        return greetings[random.Next(greetings.Length)];
    }

    private static string GetRecentlyActiveGreeting(long documentsLast24h)
    {
        if (documentsLast24h == 0)
        {
            return GeneralGreetings[random.Next(GeneralGreetings.Length)];
        }

        var greetings = documentsLast24h switch
        {
            < 5 => new[]
            {
                "Keeping things steady.",
                "Printing along nicely.",
                "Quiet but productive.",
                "Steady pace today."
            },
            < 20 => new[]
            {
                "Active printing session.",
                "Documents are flowing.",
                "Productive day so far.",
                "Keeping busy with prints."
            },
            _ => new[]
            {
                "Super busy today!",
                "Printers are working hard.",
                "What a productive day!",
                "Documents flying off the press!",
                "You're on fire today!"
            }
        };

        return greetings[random.Next(greetings.Length)];
    }

    private static string GetWelcomeBackGreeting(TimeSpan timeSinceLastDocument)
    {
        var hours = (int)timeSinceLastDocument.TotalHours;

        if (hours < 6)
        {
            var soonGreetings = new[]
            {
                "Back so soon?",
                "Ready for more printing?",
                "Printers are waiting.",
                "Let's continue where we left off."
            };
            return soonGreetings[random.Next(soonGreetings.Length)];
        }

        var greetings = new[]
        {
            "Welcome back!",
            "Good to see you again.",
            "Ready to print more?",
            "Back for another session.",
            "Printers missed you."
        };
        return greetings[random.Next(greetings.Length)];
    }

    private static string GetLongTimeNoSeeGreeting(TimeSpan timeSinceLastDocument)
    {
        var days = (int)timeSinceLastDocument.TotalDays;

        if (days < 7)
        {
            var greetings = new[]
            {
                "Been a few days!",
                "Back after a short break.",
                "Ready to print again?",
                "Printers are ready for you."
            };
            return greetings[random.Next(greetings.Length)];
        }

        if (days < 14)
        {
            var weekGreetings = new[]
            {
                "It's been a week!",
                "Long time no see.",
                "Back for more printing?",
                "A week since your last print."
            };
            return weekGreetings[random.Next(weekGreetings.Length)];
        }

        var longTimeGreetings = new[]
        {
            "Been a while!",
            "Welcome back after a break.",
            "Ready to start printing again?",
            "Printers have been waiting."
        };
        return longTimeGreetings[random.Next(longTimeGreetings.Length)];
    }

    private static string GetVeryLongTimeGreeting()
    {
        var greetings = new[]
        {
            "It's been a long time!",
            "Welcome back after a long break.",
            "Ready to dust off those printers?",
            "Long time no see!",
            "Your printers are still here."
        };
        return greetings[random.Next(greetings.Length)];
    }

    private static string GetActivityBasedGreeting(long documentsLast24h, long totalDocuments)
    {
        // If no activity in 24h, use general
        if (documentsLast24h == 0)
        {
            return GeneralGreetings[random.Next(GeneralGreetings.Length)];
        }

        // Otherwise use volume-based greeting
        var greetings = documentsLast24h switch
        {
            < 5 => new[]
            {
                "Light day today.",
                "Taking it easy?",
                "Steady pace.",
                "Quiet productive day."
            },
            < 20 => new[]
            {
                "Busy day!",
                "Things are moving.",
                "Productive session.",
                "Documents flowing nicely."
            },
            _ => new[]
            {
                "Super busy today!",
                "Printers working overtime!",
                "What a day!",
                "You're on fire!",
                "Maximum productivity!"
            }
        };

        return greetings[random.Next(greetings.Length)];
    }

    private enum GreetingStrategy
    {
        GeneralOnly,
        DaytimeBased,
        ContextBased
    }
}
