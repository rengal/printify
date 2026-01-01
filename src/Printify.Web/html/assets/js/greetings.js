// Greeting message generator with contextual awareness

function getWelcomeMessage(name, printers, documents, workspaceCreatedAt) {
    const nameStr = name ? `, ${name}` : '';
    const hour = new Date().getHours();
    const now = new Date();

    // 25% chance for contextual greeting
    const useContextual = Math.random() < 0.25;

    if (useContextual && workspaceCreatedAt) {
        const contextualGreeting = getContextualGreeting(nameStr, printers, documents, workspaceCreatedAt, now);
        if (contextualGreeting) {
            return contextualGreeting;
        }
    }

    // 75% chance for general/time-based greeting
    return getGeneralGreeting(nameStr, hour);
}

function getContextualGreeting(nameStr, printers, documents, workspaceCreatedAt, now) {
    const workspaceAge = now - workspaceCreatedAt;
    const threeDaysMs = 3 * 24 * 60 * 60 * 1000;
    const oneMonthMs = 30 * 24 * 60 * 60 * 1000;
    const oneDayMs = 24 * 60 * 60 * 1000;
    const threeMinutesMs = 3 * 60 * 1000;

    // Flatten all documents from all printers
    const allDocs = Object.values(documents).flat();

    // Count documents in last 24 hours
    const last24hDocs = allDocs.filter(doc => {
        return doc.timestamp && (now - doc.timestamp) < oneDayMs;
    });
    const docsLast24h = last24hDocs.length;

    // Find most recent document
    const mostRecentDoc = allDocs.length > 0
        ? allDocs.reduce((latest, doc) =>
            (doc.timestamp > latest.timestamp) ? doc : latest
          )
        : null;

    const daysSinceLastDoc = mostRecentDoc
        ? (now - mostRecentDoc.timestamp) / (24 * 60 * 60 * 1000)
        : Infinity;

    // 1. Workspace created less than 3 minutes ago
    if (workspaceAge < threeMinutesMs) {
        const greetings = [
            `Welcome to your new workspace${nameStr}!`,
            `Fresh start${nameStr}!`,
            `Let's get you set up${nameStr}!`,
            `Your workspace is ready${nameStr}!`,
            `Time to configure your printers${nameStr}!`
        ];
        return greetings[Math.floor(Math.random() * greetings.length)];
    }

    // 2. No documents over 3 days and workspace created more than 3 days ago
    if (workspaceAge > threeDaysMs && daysSinceLastDoc > 3) {
        const greetings = [
            `Been a while${nameStr}!`,
            `Long time no see${nameStr}!`,
            `Welcome back after a break${nameStr}!`,
            `Ready to start printing again${nameStr}?`,
            `Printers are waiting${nameStr}!`
        ];
        return greetings[Math.floor(Math.random() * greetings.length)];
    }

    // 3. No documents over 1 month and workspace created more than 1 month ago
    if (workspaceAge > oneMonthMs && daysSinceLastDoc > 30) {
        const greetings = [
            `It's been a month${nameStr}!`,
            `Missed you${nameStr}!`,
            `Where have you been${nameStr}?`,
            `Back from vacation${nameStr}?`,
            `Time to dust off those printers${nameStr}!`
        ];
        return greetings[Math.floor(Math.random() * greetings.length)];
    }

    // 4. 1-10 documents in the last 24 hours
    if (docsLast24h >= 1 && docsLast24h <= 10) {
        const greetings = [
            `Light day today${nameStr}!`,
            `Taking it easy${nameStr}?`,
            `Steady pace${nameStr}!`,
            `Nice and calm${nameStr}!`,
            `${docsLast24h} document${docsLast24h > 1 ? 's' : ''} so far${nameStr}!`
        ];
        return greetings[Math.floor(Math.random() * greetings.length)];
    }

    // 5. 10-30 documents in the last 24 hours
    if (docsLast24h > 10 && docsLast24h <= 30) {
        const greetings = [
            `Busy day${nameStr}!`,
            `Things are moving${nameStr}!`,
            `${docsLast24h} documents and counting${nameStr}!`,
            `Productive day${nameStr}!`,
            `Keeping busy${nameStr}!`
        ];
        return greetings[Math.floor(Math.random() * greetings.length)];
    }

    // 6. More than 30 documents in the last 24 hours
    if (docsLast24h > 30) {
        const greetings = [
            `Wow, ${docsLast24h} documents${nameStr}!`,
            `You're on fire${nameStr}!`,
            `Super busy today${nameStr}!`,
            `Printers working overtime${nameStr}!`,
            `What a day${nameStr}!`,
            `That's a lot of printing${nameStr}!`
        ];
        return greetings[Math.floor(Math.random() * greetings.length)];
    }

    return null; // Fall back to general greeting
}

function getGeneralGreeting(nameStr, hour) {
    // Time-based greetings (8 variations)
    const timeBasedGreetings = [];
    if (hour >= 5 && hour < 12) {
        timeBasedGreetings.push(
            `Good morning${nameStr}!`,
            `Rise and shine${nameStr}!`,
            `Morning${nameStr}!`,
            `Top of the morning${nameStr}!`
        );
    } else if (hour >= 12 && hour < 17) {
        timeBasedGreetings.push(
            `Good afternoon${nameStr}!`,
            `Afternoon${nameStr}!`,
            `Hello${nameStr}!`,
            `Hey there${nameStr}!`
        );
    } else {
        timeBasedGreetings.push(
            `Good evening${nameStr}!`,
            `Evening${nameStr}!`,
            `Hello${nameStr}!`,
            `Hey${nameStr}!`
        );
    }

    // General greetings (12 variations)
    const generalGreetings = [
        `Welcome back${nameStr}!`,
        `Great to see you${nameStr}!`,
        `Nice to see you${nameStr}!`,
        `Hello again${nameStr}!`,
        `Welcome${nameStr}!`,
        `Hi${nameStr}!`,
        `Hey${nameStr}!`,
        `Greetings${nameStr}!`,
        `Good to have you back${nameStr}!`,
        `Ready to print${nameStr}?`,
        `Let's get to work${nameStr}!`,
        `Back again${nameStr}?`
    ];

    // Combine all greetings (20 total)
    const allGreetings = [...timeBasedGreetings, ...generalGreetings];

    // Select random greeting
    return allGreetings[Math.floor(Math.random() * allGreetings.length)];
}
