BotDelay =
{
	easy = 12,
	normal = 6,
	hard = 3
}

WorldLoaded = function()
	Camera.Position = Actor1.CenterPosition
	Ants = Player.GetPlayer("Ants")
	Wasps = Player.GetPlayer("Wasps")
	Scorpions = Player.GetPlayer("Scorpions")
	Trigger.AfterDelay(DateTime.Seconds(BotDelay[Map.LobbyOption("difficulty")]), function()
	end)
end