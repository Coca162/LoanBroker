using static Shared.Main;

Config config = await GetConfig<Config>();

await Bot.RunAsync(config);

await Task.Delay(-1);